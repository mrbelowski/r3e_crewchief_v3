﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.GameState;
using CrewChiefV3.Events;

/**
 * Maps memory mapped file to a local game-agnostic representation.
 * 
 * Weather...
 * 
 * cloud brightness varies a *lot*. Perhaps it's for that section of track?
 */
namespace CrewChiefV3.PCars
{
    class PCarsGameStateMapper : GameStateMapper
    {
        private Boolean attemptPitDetection = UserSettings.GetUserSettings().getBoolean("attempt_pcars_opponent_pit_detection");
        private static String userSpecifiedSteamId = UserSettings.GetUserSettings().getString("pcars_steam_id");
        private static String playerSteamId = null;
        private static Boolean getPlayerByName = true;

        private List<CornerData.EnumWithThresholds> suspensionDamageThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> tyreWearThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> brakeDamageThresholds = new List<CornerData.EnumWithThresholds>();

        // these are set when we start a new session, from the car name / class
        private TyreType defaultTyreTypeForPlayersCar = TyreType.Unknown_Race;
        private List<CornerData.EnumWithThresholds> brakeTempThresholdsForPlayersCar = null;

        // if all 4 wheels are off the racing surface, increment the number of cut track incedents
        private Boolean incrementCutTrackCountWhenLeavingRacingSurface = false;

        private static uint expectedVersion = 5;

        private List<uint> racingSurfaces = new List<uint>() { (uint)eTerrain.TERRAIN_BUMPY_DIRT_ROAD, 
            (uint)eTerrain.TERRAIN_BUMPY_ROAD1, (uint)eTerrain.TERRAIN_BUMPY_ROAD2, (uint)eTerrain.TERRAIN_BUMPY_ROAD3, 
            (uint)eTerrain.TERRAIN_COBBLES, (uint)eTerrain.TERRAIN_DRAINS, (uint)eTerrain.TERRAIN_EXIT_RUMBLE_STRIPS,
            (uint)eTerrain.TERRAIN_LOW_GRIP_ROAD, (uint)eTerrain.TERRAIN_MARBLES,(uint)eTerrain.TERRAIN_PAVEMENT,
            (uint)eTerrain.TERRAIN_ROAD, (uint)eTerrain.TERRAIN_RUMBLE_STRIPS, (uint)eTerrain.TERRAIN_SAND_ROAD};

        private float trivialEngineDamageThreshold = 0.02f;
        private float minorEngineDamageThreshold = 0.10f;
        private float severeEngineDamageThreshold = 0.35f;
        private float destroyedEngineDamageThreshold = 0.80f;

        private float trivialSuspensionDamageThreshold = 0.02f;
        private float minorSuspensionDamageThreshold = 0.10f;
        private float severeSuspensionDamageThreshold = 0.25f;
        private float destroyedSuspensionDamageThreshold = 0.60f;

        private float trivialBrakeDamageThreshold = 0.1f;
        private float minorBrakeDamageThreshold = 0.25f;
        private float severeBrakeDamageThreshold = 0.5f;
        private float destroyedBrakeDamageThreshold = 0.90f;

        private float trivialAeroDamageThreshold = 0.1f;
        private float minorAeroDamageThreshold = 0.25f;
        private float severeAeroDamageThreshold = 0.5f;
        private float destroyedAeroDamageThreshold = 0.90f;

        // tyres in PCars are worn out when the wear level is > ?
        private float wornOutTyreWearLevel = 0.50f;

        private float scrubbedTyreWearPercent = 1f;
        private float minorTyreWearPercent = 20f;
        private float majorTyreWearPercent = 40f;
        private float wornOutTyreWearPercent = 80f;

        private TimeSpan minimumSessionParticipationTime = TimeSpan.FromSeconds(6);

        private Dictionary<String, int> previousSpeedReuseCount = new Dictionary<string, int>();

        private SpeechRecogniser speechRecogniser;
        
        public PCarsGameStateMapper()
        {
            CornerData.EnumWithThresholds suspensionDamageNone = new CornerData.EnumWithThresholds(DamageLevel.NONE, -10000, trivialSuspensionDamageThreshold);
            CornerData.EnumWithThresholds suspensionDamageTrivial = new CornerData.EnumWithThresholds(DamageLevel.TRIVIAL, trivialSuspensionDamageThreshold, minorSuspensionDamageThreshold);
            CornerData.EnumWithThresholds suspensionDamageMinor = new CornerData.EnumWithThresholds(DamageLevel.MINOR, trivialSuspensionDamageThreshold, severeSuspensionDamageThreshold);
            CornerData.EnumWithThresholds suspensionDamageMajor = new CornerData.EnumWithThresholds(DamageLevel.MAJOR, severeSuspensionDamageThreshold, destroyedSuspensionDamageThreshold);
            CornerData.EnumWithThresholds suspensionDamageDestroyed = new CornerData.EnumWithThresholds(DamageLevel.DESTROYED, destroyedSuspensionDamageThreshold, 10000);
            suspensionDamageThresholds.Add(suspensionDamageNone);
            suspensionDamageThresholds.Add(suspensionDamageTrivial);
            suspensionDamageThresholds.Add(suspensionDamageMinor);
            suspensionDamageThresholds.Add(suspensionDamageMajor);
            suspensionDamageThresholds.Add(suspensionDamageDestroyed);

            CornerData.EnumWithThresholds brakeDamageNone = new CornerData.EnumWithThresholds(DamageLevel.NONE, -10000, trivialBrakeDamageThreshold);
            CornerData.EnumWithThresholds brakeDamageTrivial = new CornerData.EnumWithThresholds(DamageLevel.TRIVIAL, trivialBrakeDamageThreshold, minorBrakeDamageThreshold);
            CornerData.EnumWithThresholds brakeDamageMinor = new CornerData.EnumWithThresholds(DamageLevel.MINOR, trivialBrakeDamageThreshold, severeBrakeDamageThreshold);
            CornerData.EnumWithThresholds brakeDamageMajor = new CornerData.EnumWithThresholds(DamageLevel.MAJOR, severeBrakeDamageThreshold, destroyedBrakeDamageThreshold);
            CornerData.EnumWithThresholds brakeDamageDestroyed = new CornerData.EnumWithThresholds(DamageLevel.DESTROYED, destroyedBrakeDamageThreshold, 10000);
            brakeDamageThresholds.Add(brakeDamageNone);
            brakeDamageThresholds.Add(brakeDamageTrivial);
            brakeDamageThresholds.Add(brakeDamageMinor);
            brakeDamageThresholds.Add(brakeDamageMajor);
            brakeDamageThresholds.Add(brakeDamageDestroyed);

            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000, scrubbedTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, scrubbedTyreWearPercent, minorTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, minorTyreWearPercent, majorTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, majorTyreWearPercent, wornOutTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, wornOutTyreWearPercent, 10000));
        }

        public void versionCheck(Object memoryMappedFileStruct)
        {
            pCarsAPIStruct shared = ((CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)memoryMappedFileStruct).data;
            uint currentVersion = shared.mVersion;
            if (currentVersion != expectedVersion)
            {
                throw new GameDataReadException("Expected shared data version " + expectedVersion + " but got version " + currentVersion);
            }
        }

        public void setSpeechRecogniser(SpeechRecogniser speechRecogniser)
        {
            this.speechRecogniser = speechRecogniser;
        }

        public static pCarsAPIParticipantStruct getParticipantDataForName(pCarsAPIParticipantStruct[] pCarsAPIParticipantStructArray, String name, int index)
        {
            if (name != null && name.Length > 0)
            {
                foreach (pCarsAPIParticipantStruct data in pCarsAPIParticipantStructArray)
                {
                    if (data.mName == name)
                    {
                        return data;
                    }
                }
            }
            return pCarsAPIParticipantStructArray[index];
        }
        
        public static Tuple<int, pCarsAPIParticipantStruct> getPlayerDataStruct(pCarsAPIParticipantStruct[] pCarsAPIParticipantStructArray, int viewedParticipantIndex)
        {
            if (!getPlayerByName)
            {
                return new Tuple<int, pCarsAPIParticipantStruct>(viewedParticipantIndex, pCarsAPIParticipantStructArray[viewedParticipantIndex]);
            }
            else if (playerSteamId == null)
            {
                if (userSpecifiedSteamId == null || userSpecifiedSteamId.Length == 0)
                {
                    // get the player ID from the first viewed participant the app 'sees' and use this for the remainder of the app's run time
                    pCarsAPIParticipantStruct likelyParticipantData = pCarsAPIParticipantStructArray[viewedParticipantIndex];
                    playerSteamId = likelyParticipantData.mName;
                    Console.WriteLine("Using player steam ID " + playerSteamId + " from the first reported viewed participant");
                    if (playerSteamId == null || playerSteamId.Length == 0) 
                    {
                        getPlayerByName = false;
                        Console.WriteLine("Player steam ID is not valid, falling back to viewedParticipantIndex");
                    }
                    return new Tuple<int, pCarsAPIParticipantStruct>(viewedParticipantIndex, likelyParticipantData);
                }
                else
                {
                    // use the steamID provided
                    playerSteamId = userSpecifiedSteamId;
                }
            }
            for (int i=0; i<pCarsAPIParticipantStructArray.Length; i++) 
            {
                if (pCarsAPIParticipantStructArray[i].mName == playerSteamId)
                {
                    return new Tuple<int, pCarsAPIParticipantStruct> (i,pCarsAPIParticipantStructArray[i]);
                }
            }
            // no match in the block, so use the viewParticipantIndex
            getPlayerByName = false;
            return new Tuple<int, pCarsAPIParticipantStruct>(viewedParticipantIndex, pCarsAPIParticipantStructArray[viewedParticipantIndex]);            
        }

        public GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            pCarsAPIStruct shared = ((CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)memoryMappedFileStruct).data;
            long ticks = ((CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)memoryMappedFileStruct).ticksWhenRead;

            // game state is 3 for paused, 5 for replay. No idea what 4 is...
            if (shared.mGameState == (uint)eGameState.GAME_FRONT_END ||
                (shared.mGameState == (uint)eGameState.GAME_INGAME_PAUSED && !System.Diagnostics.Debugger.IsAttached) ||
                (shared.mGameState == (uint)eGameState.GAME_INGAME_PAUSED) || 
                shared.mGameState == (uint)eGameState.GAME_VIEWING_REPLAY || shared.mGameState == (uint)eGameState.GAME_EXITED)
            {
                // don't ignore the paused game updates if we're in debug mode
                return previousGameState;
            }
            
            GameStateData currentGameState = new GameStateData(ticks);
            if (shared.mNumParticipants < 1 || shared.mTrackLocation == null || shared.mTrackLocation.Length == 0 ||
                shared.mCarName == null || shared.mCarName == "" || shared.mTrackLength <= 0)
            {
                // Unusable data in the block
                // TODO: is this check sufficient?
                return null;
            }            
            Tuple<int, pCarsAPIParticipantStruct> playerData = getPlayerDataStruct(shared.mParticipantData, shared.mViewedParticipantIndex);
            if (getPlayerByName && playerSteamId != shared.mParticipantData[shared.mViewedParticipantIndex].mName)
            {
                return null;
            }
            
            int playerDataIndex = playerData.Item1;
            pCarsAPIParticipantStruct viewedParticipant = playerData.Item2;
            NameValidator.validateName(viewedParticipant.mName);
            currentGameState.SessionData.CompletedLaps = (int)viewedParticipant.mLapsCompleted;
            currentGameState.SessionData.SectorNumber = (int)viewedParticipant.mCurrentSector;
            currentGameState.SessionData.Position = (int)viewedParticipant.mRacePosition;
            currentGameState.SessionData.UnFilteredPosition = (int)viewedParticipant.mRacePosition;
            currentGameState.SessionData.IsNewSector = previousGameState == null || viewedParticipant.mCurrentSector != previousGameState.SessionData.SectorNumber;
            currentGameState.PositionAndMotionData.DistanceRoundTrack = viewedParticipant.mCurrentLapDistance;
          
            
            // previous session data to check if we've started an new session
            SessionPhase lastSessionPhase = SessionPhase.Unavailable;
            SessionType lastSessionType = SessionType.Unavailable;
            float lastSessionRunningTime = 0;
            int lastSessionLapsCompleted = 0;
            TrackDefinition lastSessionTrack = null;
            Boolean lastSessionHasFixedTime = false;
            int lastSessionNumberOfLaps = 0;
            float lastSessionRunTime = 0;
            float lastSessionTimeRemaining = 0;
            CarData.CarClass carClass = CarData.getDefaultCarClass();
            if (previousGameState != null)
            {
                lastSessionPhase = previousGameState.SessionData.SessionPhase;
                lastSessionType = previousGameState.SessionData.SessionType;
                lastSessionRunningTime = previousGameState.SessionData.SessionRunningTime;
                lastSessionHasFixedTime = previousGameState.SessionData.SessionHasFixedTime;
                lastSessionTrack = previousGameState.SessionData.TrackDefinition;
                lastSessionLapsCompleted = previousGameState.SessionData.CompletedLaps;
                lastSessionNumberOfLaps = previousGameState.SessionData.SessionNumberOfLaps;
                lastSessionRunTime = previousGameState.SessionData.SessionRunTime;
                lastSessionTimeRemaining = previousGameState.SessionData.SessionTimeRemaining;
                if (previousGameState.carClass != null)
                {
                    carClass = previousGameState.carClass;
                }
            }

            currentGameState.carClass = carClass;

            // current session data
            currentGameState.SessionData.SessionType = mapToSessionType(shared);
            Boolean leaderHasFinished = previousGameState != null && previousGameState.SessionData.LeaderHasFinishedRace;
            currentGameState.SessionData.LeaderHasFinishedRace = leaderHasFinished;
            currentGameState.SessionData.IsDisqualified = shared.mRaceState == (int)eRaceState.RACESTATE_DISQUALIFIED;
            currentGameState.SessionData.SessionPhase = mapToSessionPhase(currentGameState.SessionData.SessionType,
                shared.mSessionState, shared.mRaceState, shared.mNumParticipants, leaderHasFinished, lastSessionPhase, lastSessionTimeRemaining, lastSessionRunTime);
            float sessionTimeRemaining = -1;
            int numberOfLapsInSession = (int)shared.mLapsInEvent;
            if (shared.mEventTimeRemaining > 0)
            {
                currentGameState.SessionData.SessionHasFixedTime = true;
                sessionTimeRemaining = shared.mEventTimeRemaining;
            }
            currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.mTrackLocation + ":" + shared.mTrackVariation, shared.mTrackLength, GameEnum.PCARS_64BIT);
            // Console.WriteLine(lastSessionPhase + ", " + currentGameState.SessionData.SessionPhase + "; " + lastSessionType + ", " + currentGameState.SessionData.SessionType);
            // now check if this is a new session...
            Boolean raceRestarted = currentGameState.SessionData.SessionType == SessionType.Race &&
                lastSessionPhase == SessionPhase.Green && currentGameState.SessionData.SessionPhase == SessionPhase.Countdown;
            if (raceRestarted || 
                (currentGameState.SessionData.SessionType != SessionType.Unavailable && (lastSessionType != currentGameState.SessionData.SessionType ||
                lastSessionHasFixedTime != currentGameState.SessionData.SessionHasFixedTime || 
                lastSessionTrack != currentGameState.SessionData.TrackDefinition || lastSessionLapsCompleted > currentGameState.SessionData.CompletedLaps ||
                (numberOfLapsInSession > 0 && lastSessionNumberOfLaps > 0 && lastSessionNumberOfLaps != numberOfLapsInSession) ||
                (sessionTimeRemaining > 0 && sessionTimeRemaining > lastSessionRunTime))))
            {
                Console.WriteLine("New session, trigger...");
                if (raceRestarted)
                {
                    Console.WriteLine("Race restarted (green -> countdown)");
                }
                if (lastSessionType != currentGameState.SessionData.SessionType) 
                {
                    Console.WriteLine("lastSessionType = " + lastSessionType + " currentGameState.SessionData.SessionType = " + currentGameState.SessionData.SessionType);
                }
                else if (lastSessionHasFixedTime != currentGameState.SessionData.SessionHasFixedTime) 
                {
                    Console.WriteLine("lastSessionHasFixedTime = " + lastSessionHasFixedTime + " currentGameState.SessionData.SessionHasFixedTime = " + currentGameState.SessionData.SessionHasFixedTime);
                }
                else if (lastSessionTrack != currentGameState.SessionData.TrackDefinition)
                {
                    String lastTrackName = lastSessionTrack == null ? "unknown" : lastSessionTrack.name;
                    String currentTrackName = currentGameState.SessionData.TrackDefinition == null ? "unknown" : currentGameState.SessionData.TrackDefinition.name;
                    Console.WriteLine("lastSessionTrack = " + lastTrackName + " currentGameState.SessionData.Track = " + currentTrackName);
                } 
                else if (lastSessionLapsCompleted > currentGameState.SessionData.CompletedLaps)
                {
                    Console.WriteLine("lastSessionLapsCompleted = " + lastSessionLapsCompleted + " currentGameState.SessionData.CompletedLaps = " + currentGameState.SessionData.CompletedLaps);
                }
                else if (lastSessionNumberOfLaps != numberOfLapsInSession)
                {
                    Console.WriteLine("lastSessionNumberOfLaps = " + lastSessionNumberOfLaps + " numberOfLapsInSession = "+ numberOfLapsInSession);
                }
                else if (sessionTimeRemaining > 0 && sessionTimeRemaining > lastSessionRunTime)
                {
                    Console.WriteLine("sessionTimeRemaining = " + sessionTimeRemaining + " lastSessionRunTime = " + lastSessionRunTime);
                }
                currentGameState.SessionData.IsNewSession = true;
                currentGameState.SessionData.SessionNumberOfLaps = numberOfLapsInSession;
                currentGameState.SessionData.LeaderHasFinishedRace = false;
                currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                currentGameState.SessionData.SessionStartPosition = (int)viewedParticipant.mRacePosition;
                if (currentGameState.SessionData.SessionHasFixedTime)
                {
                    currentGameState.SessionData.SessionRunTime = sessionTimeRemaining;
                    currentGameState.SessionData.SessionTimeRemaining = sessionTimeRemaining;
                    if (currentGameState.SessionData.SessionRunTime == 0)
                    {
                        Console.WriteLine("Setting session run time to 0");
                    }
                    Console.WriteLine("Time in this new session = " + sessionTimeRemaining);
                }
                currentGameState.SessionData.DriverRawName = viewedParticipant.mName;
                currentGameState.PitData.IsRefuellingAllowed = true;

                for (int i=0; i < shared.mParticipantData.Length; i++)
                {
                    pCarsAPIParticipantStruct participantStruct = shared.mParticipantData[i];
                    if (i != playerDataIndex && participantStruct.mIsActive && participantStruct.mName != null && participantStruct.mName.Length > 0)
                    {
                        if (!currentGameState.OpponentData.ContainsKey(participantStruct.mName))
                        {
                            currentGameState.OpponentData.Add(participantStruct.mName, createOpponentData(participantStruct, false));
                        }
                    }
                }
                currentGameState.carClass = CarData.getCarClassForPCarsClassName(shared.mCarClassName);
                brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(carClass, shared.mCarName);
                // no tyre data in the block so get the default tyre types for this car
                defaultTyreTypeForPlayersCar = CarData.getDefaultTyreType(carClass, shared.mCarName);
            }
            else
            {
                Boolean justGoneGreen = false;
                if (lastSessionPhase != currentGameState.SessionData.SessionPhase)
                {
                    if (currentGameState.SessionData.SessionPhase == SessionPhase.Green)
                    {
                        justGoneGreen = true;
                        // just gone green, so get the session data   
                        if (currentGameState.SessionData.SessionType == SessionType.Race)
                        {
                            if (currentGameState.SessionData.SessionHasFixedTime)
                            {
                                currentGameState.SessionData.SessionRunTime = sessionTimeRemaining;
                                currentGameState.SessionData.SessionTimeRemaining = sessionTimeRemaining;
                                if (currentGameState.SessionData.SessionRunTime == 0)
                                {
                                    Console.WriteLine("Setting session run time to 0");
                                }
                            }
                            currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                            currentGameState.SessionData.SessionNumberOfLaps = numberOfLapsInSession;
                            currentGameState.SessionData.SessionStartPosition = (int)viewedParticipant.mRacePosition;
                        }          
                        currentGameState.SessionData.LeaderHasFinishedRace = false;
                        currentGameState.SessionData.NumCarsAtStartOfSession = shared.mNumParticipants;
                        currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.mTrackLocation + ":" + shared.mTrackVariation, shared.mTrackLength, GameEnum.PCARS_64BIT);

                        if (previousGameState != null)
                        {
                            currentGameState.OpponentData = previousGameState.OpponentData;
                            currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                            if (currentGameState.SessionData.SessionType != SessionType.Race)
                            {
                                currentGameState.SessionData.SessionStartTime = previousGameState.SessionData.SessionStartTime;
                                currentGameState.SessionData.SessionRunTime = previousGameState.SessionData.SessionRunTime;
                                currentGameState.SessionData.SessionTimeRemaining = previousGameState.SessionData.SessionTimeRemaining;
                                currentGameState.SessionData.SessionNumberOfLaps = previousGameState.SessionData.SessionNumberOfLaps;
                            }
                        }

                        Console.WriteLine("Just gone green, session details...");
                        Console.WriteLine("SessionType " + currentGameState.SessionData.SessionType);
                        Console.WriteLine("SessionPhase " + currentGameState.SessionData.SessionPhase);
                        if (previousGameState != null)
                        {
                            Console.WriteLine("previous SessionPhase " + previousGameState.SessionData.SessionPhase);
                        }
                        Console.WriteLine("EventIndex " + currentGameState.SessionData.EventIndex);
                        Console.WriteLine("SessionIteration " + currentGameState.SessionData.SessionIteration);
                        Console.WriteLine("HasMandatoryPitStop " + currentGameState.PitData.HasMandatoryPitStop);
                        Console.WriteLine("PitWindowStart " + currentGameState.PitData.PitWindowStart);
                        Console.WriteLine("PitWindowEnd " + currentGameState.PitData.PitWindowEnd);
                        Console.WriteLine("NumCarsAtStartOfSession " + currentGameState.SessionData.NumCarsAtStartOfSession);
                        Console.WriteLine("SessionNumberOfLaps " + currentGameState.SessionData.SessionNumberOfLaps);
                        Console.WriteLine("SessionRunTime " + currentGameState.SessionData.SessionRunTime);
                        Console.WriteLine("SessionStartPosition " + currentGameState.SessionData.SessionStartPosition);
                        Console.WriteLine("SessionStartTime " + currentGameState.SessionData.SessionStartTime);
                        String trackName = currentGameState.SessionData.TrackDefinition == null ? "unknown" : currentGameState.SessionData.TrackDefinition.name;
                        Console.WriteLine("TrackName " + trackName);
                    }
                }
                // copy persistent data from the previous game state
                //
                if (!justGoneGreen && previousGameState != null)
                {
                    currentGameState.SessionData.SessionStartTime = previousGameState.SessionData.SessionStartTime;
                    currentGameState.SessionData.SessionRunTime = previousGameState.SessionData.SessionRunTime;
                    currentGameState.SessionData.SessionNumberOfLaps = previousGameState.SessionData.SessionNumberOfLaps;
                    currentGameState.SessionData.SessionStartPosition = previousGameState.SessionData.SessionStartPosition;
                    currentGameState.SessionData.NumCarsAtStartOfSession = previousGameState.SessionData.NumCarsAtStartOfSession;
                    currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                    currentGameState.SessionData.EventIndex = previousGameState.SessionData.EventIndex;
                    currentGameState.SessionData.SessionIteration = previousGameState.SessionData.SessionIteration;
                    currentGameState.OpponentData = previousGameState.OpponentData;
                    currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                    currentGameState.PitData.PitWindowStart = previousGameState.PitData.PitWindowStart;
                    currentGameState.PitData.PitWindowEnd = previousGameState.PitData.PitWindowEnd;
                    currentGameState.PitData.HasMandatoryPitStop = previousGameState.PitData.HasMandatoryPitStop;
                    currentGameState.PitData.HasMandatoryDriverChange = previousGameState.PitData.HasMandatoryDriverChange;
                    currentGameState.PitData.HasMandatoryTyreChange = previousGameState.PitData.HasMandatoryTyreChange;
                    currentGameState.PitData.MandatoryTyreChangeRequiredTyreType = previousGameState.PitData.MandatoryTyreChangeRequiredTyreType;
                    currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                    currentGameState.PitData.MaxPermittedDistanceOnCurrentTyre = previousGameState.PitData.MaxPermittedDistanceOnCurrentTyre;
                    currentGameState.PitData.MinPermittedDistanceOnCurrentTyre = previousGameState.PitData.MinPermittedDistanceOnCurrentTyre;
                    currentGameState.SessionData.SessionTimesAtEndOfSectors = previousGameState.SessionData.SessionTimesAtEndOfSectors;
                    currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings;
                    currentGameState.SessionData.formattedPlayerLapTimes = previousGameState.SessionData.formattedPlayerLapTimes;
                    currentGameState.SessionData.PlayerLapTimeSessionBest = previousGameState.SessionData.PlayerLapTimeSessionBest;
                    currentGameState.SessionData.OpponentsLapTimeSessionBestOverall = previousGameState.SessionData.OpponentsLapTimeSessionBestOverall;
                    currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = previousGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass;
                    currentGameState.SessionData.OverallSessionBestLapTime = previousGameState.SessionData.OverallSessionBestLapTime;
                    currentGameState.SessionData.PlayerClassSessionBestLapTime = previousGameState.SessionData.PlayerClassSessionBestLapTime;
                    currentGameState.SessionData.GameTimeAtLastPositionFrontChange = previousGameState.SessionData.GameTimeAtLastPositionFrontChange;
                    currentGameState.SessionData.GameTimeAtLastPositionBehindChange = previousGameState.SessionData.GameTimeAtLastPositionBehindChange;
                    currentGameState.SessionData.LastSector1Time = previousGameState.SessionData.LastSector1Time;
                    currentGameState.SessionData.LastSector2Time = previousGameState.SessionData.LastSector2Time;
                    currentGameState.SessionData.LastSector3Time = previousGameState.SessionData.LastSector3Time;
                    currentGameState.SessionData.BestSector1Time = previousGameState.SessionData.BestSector1Time;
                    currentGameState.SessionData.BestSector2Time = previousGameState.SessionData.BestSector2Time;
                    currentGameState.SessionData.BestSector3Time = previousGameState.SessionData.BestSector3Time;
                    currentGameState.SessionData.BestLapSector1Time = previousGameState.SessionData.BestLapSector1Time;
                    currentGameState.SessionData.BestLapSector2Time = previousGameState.SessionData.BestLapSector2Time;
                    currentGameState.SessionData.BestLapSector3Time = previousGameState.SessionData.BestLapSector3Time;
                    currentGameState.Conditions = previousGameState.Conditions;
                }                
            }            
            
            //------------------- Variable session data ---------------------------
            if (currentGameState.SessionData.SessionHasFixedTime)
            {
                currentGameState.SessionData.SessionRunningTime = currentGameState.SessionData.SessionRunTime - shared.mEventTimeRemaining;
                currentGameState.SessionData.SessionTimeRemaining = shared.mEventTimeRemaining;
            }
            else
            {
                currentGameState.SessionData.SessionRunningTime = (float)(currentGameState.Now - currentGameState.SessionData.SessionStartTime).TotalSeconds;
            }
            if (currentGameState.SessionData.IsNewSector)
            {
                if (currentGameState.SessionData.SectorNumber == 1)
                {
                    currentGameState.SessionData.LapTimePreviousEstimateForInvalidLap = currentGameState.SessionData.SessionRunningTime - currentGameState.SessionData.SessionTimesAtEndOfSectors[3];
                    currentGameState.SessionData.SessionTimesAtEndOfSectors[3] = currentGameState.SessionData.SessionRunningTime;
                    currentGameState.SessionData.LastSector3Time = shared.mCurrentSector3Time;
                    if (currentGameState.SessionData.LastSector3Time > 0 && 
                        (currentGameState.SessionData.BestSector3Time == -1 || currentGameState.SessionData.LastSector3Time < currentGameState.SessionData.BestSector3Time))
                    {
                        currentGameState.SessionData.BestSector3Time = currentGameState.SessionData.LastSector3Time;
                    }
                    if (shared.mLastLapTime > 0 &&
                        (currentGameState.SessionData.PlayerLapTimeSessionBest == -1 || shared.mLastLapTime <= currentGameState.SessionData.PlayerLapTimeSessionBest))
                    {
                        currentGameState.SessionData.BestLapSector1Time = currentGameState.SessionData.LastSector1Time;
                        currentGameState.SessionData.BestLapSector2Time = currentGameState.SessionData.LastSector2Time;
                        currentGameState.SessionData.BestLapSector3Time = currentGameState.SessionData.LastSector3Time;
                    }
                }
                else if (currentGameState.SessionData.SectorNumber == 2)
                {
                    currentGameState.SessionData.SessionTimesAtEndOfSectors[1] = currentGameState.SessionData.SessionRunningTime;
                    currentGameState.SessionData.LastSector1Time = shared.mCurrentSector1Time;
                    if (currentGameState.SessionData.LastSector1Time > 0 &&
                        (currentGameState.SessionData.BestSector1Time == -1 || currentGameState.SessionData.LastSector1Time < currentGameState.SessionData.BestSector1Time))
                    {
                        currentGameState.SessionData.BestSector1Time = currentGameState.SessionData.LastSector1Time;
                    }
                }
                if (currentGameState.SessionData.SectorNumber == 3)
                {
                    currentGameState.SessionData.SessionTimesAtEndOfSectors[2] = currentGameState.SessionData.SessionRunningTime;
                    currentGameState.SessionData.LastSector2Time = shared.mCurrentSector2Time;
                    if (currentGameState.SessionData.LastSector2Time > 0 &&
                        (currentGameState.SessionData.BestSector2Time == -1 || currentGameState.SessionData.LastSector2Time < currentGameState.SessionData.BestSector2Time))
                    {
                        currentGameState.SessionData.BestSector2Time = currentGameState.SessionData.LastSector2Time;
                    }
                }
            }

            currentGameState.SessionData.Flag = mapToFlagEnum(shared.mHighestFlagColour);
            currentGameState.SessionData.NumCars = shared.mNumParticipants;
            currentGameState.SessionData.CurrentLapIsValid = !shared.mLapInvalidated;                
            currentGameState.SessionData.IsNewLap = previousGameState == null || currentGameState.SessionData.CompletedLaps == previousGameState.SessionData.CompletedLaps + 1 ||
                ((shared.mSessionState == (int)eSessionState.SESSION_PRACTICE || shared.mSessionState == (int)eSessionState.SESSION_QUALIFY || 
                shared.mSessionState == (int)eSessionState.SESSION_TEST || shared.mSessionState == (int)eSessionState.SESSION_TIME_ATTACK) 
                && previousGameState.SessionData.LapTimeCurrent == -1 && shared.mCurrentTime > 0);

            currentGameState.SessionData.IsRacingSameCarBehind = previousGameState != null && previousGameState.getOpponentKeyBehind() == currentGameState.getOpponentKeyBehind();
            currentGameState.SessionData.IsRacingSameCarInFront = previousGameState != null && previousGameState.getOpponentKeyInFront() == currentGameState.getOpponentKeyInFront();
            if (!currentGameState.SessionData.IsRacingSameCarInFront)
            {
                currentGameState.SessionData.GameTimeAtLastPositionFrontChange = currentGameState.SessionData.SessionRunningTime;
            }
            if (!currentGameState.SessionData.IsRacingSameCarBehind)
            {
                currentGameState.SessionData.GameTimeAtLastPositionBehindChange = currentGameState.SessionData.SessionRunningTime;
            }

            currentGameState.SessionData.LapTimeCurrent = shared.mCurrentTime;
            currentGameState.SessionData.TimeDeltaBehind = shared.mSplitTimeBehind;
            currentGameState.SessionData.TimeDeltaFront = shared.mSplitTimeAhead;

            for (int i = 0; i < shared.mParticipantData.Length; i++)
            {
                if (i != playerDataIndex)
                {
                    pCarsAPIParticipantStruct participantStruct = shared.mParticipantData[i];
                    if (participantStruct.mName != null && currentGameState.OpponentData.ContainsKey(participantStruct.mName))
                    {
                        OpponentData currentOpponentData = currentGameState.OpponentData[participantStruct.mName];
                        if (currentOpponentData.IsActive && participantStruct.mIsActive)
                        {
                            if (previousGameState != null)
                            {
                                int previousOpponentSectorNumber = 1;
                                int previousOpponentCompletedLaps = 0;
                                int previousOpponentPosition = 0;
                                Boolean previousOpponentIsEnteringPits = false;
                                Boolean previousOpponentIsExitingPits = false;

                                float[] previousOpponentWorldPosition = new float[] { 0, 0, 0 };
                                float previousOpponentSpeed = 0;

                                OpponentData previousOpponentData = null;
                                if (previousGameState.OpponentData.ContainsKey(participantStruct.mName))
                                {
                                    previousOpponentData = previousGameState.OpponentData[participantStruct.mName];
                                    previousOpponentSectorNumber = previousOpponentData.CurrentSectorNumber;
                                    previousOpponentCompletedLaps = previousOpponentData.CompletedLaps;
                                    previousOpponentPosition = previousOpponentData.Position;
                                    previousOpponentIsEnteringPits = previousOpponentData.isEnteringPits();
                                    previousOpponentIsExitingPits = previousOpponentData.isExitingPits();
                                    previousOpponentWorldPosition = previousOpponentData.WorldPosition;
                                    previousOpponentSpeed = previousOpponentData.Speed;
                                }

                                int currentOpponentRacePosition = (int)participantStruct.mRacePosition;
                                int currentOpponentLapsCompleted = (int)participantStruct.mLapsCompleted;
                                int currentOpponentSector = (int)participantStruct.mCurrentSector;
                                if (currentOpponentSector == 0)
                                {
                                    currentOpponentSector = previousOpponentSectorNumber;
                                }
                                float currentOpponentLapDistance = participantStruct.mCurrentLapDistance;

                                if (currentOpponentRacePosition == 1 && (currentGameState.SessionData.SessionNumberOfLaps > 0 &&
                                        currentGameState.SessionData.SessionNumberOfLaps == currentOpponentLapsCompleted) ||
                                        (currentGameState.SessionData.SessionRunTime > 0 && currentGameState.SessionData.SessionTimeRemaining < 1 &&
                                        previousOpponentCompletedLaps < currentOpponentLapsCompleted))
                                {
                                    currentGameState.SessionData.LeaderHasFinishedRace = true;
                                }
                                if (currentOpponentRacePosition == 1 && previousOpponentPosition > 1)
                                {
                                    currentGameState.SessionData.HasLeadChanged = true;
                                }
                                int opponentPositionAtSector3 = previousOpponentPosition;
                                Boolean isEnteringPits = false;
                                Boolean isLeavingPits = false;
                                if (attemptPitDetection)
                                {
                                    if (previousOpponentData != null && currentGameState.SessionData.SessionRunningTime > 30) 
                                    {
                                        if (currentOpponentSector == 3)
                                        {
                                            if (previousOpponentSectorNumber == 2)
                                            {
                                                opponentPositionAtSector3 = currentOpponentRacePosition;
                                            }
                                            else if (!previousOpponentIsEnteringPits)
                                            {
                                                isEnteringPits = currentGameState.SessionData.TrackDefinition != null &&
                                                    currentGameState.SessionData.TrackDefinition.isAtPitEntry(participantStruct.mWorldPosition[0], participantStruct.mWorldPosition[2]);
                                            }
                                            else
                                            {
                                                isEnteringPits = previousOpponentIsEnteringPits;
                                            }
                                        }
                                        else if (currentOpponentSector == 1 && !previousOpponentIsExitingPits)
                                        {
                                            isLeavingPits = currentGameState.SessionData.TrackDefinition != null &&
                                                    currentGameState.SessionData.TrackDefinition.isAtPitExit(participantStruct.mWorldPosition[0], participantStruct.mWorldPosition[2]);
                                        }
                                    }
                                    if (isEnteringPits && !previousOpponentIsEnteringPits)
                                    {
                                        if (opponentPositionAtSector3 == 1)
                                        {
                                            Console.WriteLine("leader pitting, pos at sector 3 = " + opponentPositionAtSector3 + " current pos = " + currentOpponentRacePosition);
                                            currentGameState.PitData.LeaderIsPitting = true;
                                            currentGameState.PitData.OpponentForLeaderPitting = currentOpponentData;
                                        }
                                        if (currentGameState.SessionData.Position > 2 && opponentPositionAtSector3 == currentGameState.SessionData.Position - 1)
                                        {
                                            Console.WriteLine("car in front pitting, pos at sector 3 = " + opponentPositionAtSector3 + " current pos = " + currentOpponentRacePosition);
                                            currentGameState.PitData.CarInFrontIsPitting = true;
                                            currentGameState.PitData.OpponentForCarAheadPitting = currentOpponentData;
                                        }
                                        if (!currentGameState.isLast() && opponentPositionAtSector3 == currentGameState.SessionData.Position + 1)
                                        {
                                            Console.WriteLine("car behind pitting, pos at sector 3 = " + opponentPositionAtSector3 + " current pos = " + currentOpponentRacePosition);
                                            currentGameState.PitData.CarBehindIsPitting = true;
                                            currentGameState.PitData.OpponentForCarBehindPitting = currentOpponentData;
                                        }
                                    }
                                }
                                float secondsSinceLastUpdate = (float)new TimeSpan(currentGameState.Ticks - previousGameState.Ticks).TotalSeconds;
                                upateOpponentData(currentOpponentData, currentOpponentRacePosition, currentOpponentLapsCompleted,
                                        currentOpponentSector, isEnteringPits || isLeavingPits, currentGameState.SessionData.SessionRunningTime, secondsSinceLastUpdate,
                                        new float[] { participantStruct.mWorldPosition[0], participantStruct.mWorldPosition[2] }, previousOpponentWorldPosition,
                                        previousOpponentSpeed, shared.mWorldFastestLapTime, participantStruct.mCurrentLapDistance, shared.mRainDensity == 1, 
                                        shared.mAmbientTemperature, shared.mTrackTemperature);
                                if (currentOpponentData.IsNewLap && currentOpponentData.CurrentBestLapTime > 0)
                                {
                                    // the car class is always Unknown for PCars - it's not in the opponent data
                                    if (currentGameState.SessionData.OpponentsLapTimeSessionBestOverall == -1 ||
                                        currentOpponentData.CurrentBestLapTime < currentGameState.SessionData.OpponentsLapTimeSessionBestOverall)
                                    {
                                        currentGameState.SessionData.OpponentsLapTimeSessionBestOverall = currentOpponentData.CurrentBestLapTime;
                                        currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = currentOpponentData.CurrentBestLapTime;
                                        if (currentGameState.SessionData.OverallSessionBestLapTime == -1 ||
                                            currentGameState.SessionData.OverallSessionBestLapTime > currentOpponentData.CurrentBestLapTime)
                                        {
                                            currentGameState.SessionData.OverallSessionBestLapTime = currentOpponentData.CurrentBestLapTime;
                                            currentGameState.SessionData.PlayerClassSessionBestLapTime = currentOpponentData.CurrentBestLapTime;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            currentOpponentData.IsActive = false;
                        }
                    }
                    else
                    {
                        if (participantStruct.mIsActive && participantStruct.mName != null && participantStruct.mName.Length > 0)
                        {
                            Console.WriteLine("Creating opponent for name " + participantStruct.mName);
                            currentGameState.OpponentData.Add(participantStruct.mName, createOpponentData(participantStruct, true));
                        }
                    }
                }
            }

            currentGameState.SessionData.LapTimePrevious = shared.mLastLapTime;
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.SessionData.PreviousLapWasValid = previousGameState != null && previousGameState.SessionData.CurrentLapIsValid;
                currentGameState.SessionData.formattedPlayerLapTimes.Add(TimeSpan.FromSeconds(shared.mLastLapTime).ToString(@"mm\:ss\.fff"));
            }
            else if (previousGameState != null)
            {
                currentGameState.SessionData.PreviousLapWasValid = previousGameState.SessionData.PreviousLapWasValid;
            }

            if (currentGameState.SessionData.IsNewLap && currentGameState.SessionData.PreviousLapWasValid &&
                currentGameState.SessionData.LapTimePrevious > 0)
            {
                if ((currentGameState.SessionData.PlayerLapTimeSessionBest == -1 ||
                     currentGameState.SessionData.LapTimePrevious < currentGameState.SessionData.PlayerLapTimeSessionBest))
                {
                    currentGameState.SessionData.PlayerLapTimeSessionBest = currentGameState.SessionData.LapTimePrevious;
                    if (currentGameState.SessionData.OverallSessionBestLapTime == -1 ||
                        currentGameState.SessionData.OverallSessionBestLapTime > currentGameState.SessionData.PlayerLapTimeSessionBest)
                    {
                        currentGameState.SessionData.OverallSessionBestLapTime = currentGameState.SessionData.PlayerLapTimeSessionBest;
                    }
                    if (currentGameState.SessionData.PlayerClassSessionBestLapTime == -1 ||
                        currentGameState.SessionData.PlayerClassSessionBestLapTime > currentGameState.SessionData.PlayerLapTimeSessionBest)
                    {
                        currentGameState.SessionData.PlayerClassSessionBestLapTime = currentGameState.SessionData.PlayerLapTimeSessionBest;
                    }
                }
            }

            currentGameState.PitData.InPitlane = shared.mPitMode == (int)ePitMode.PIT_MODE_DRIVING_INTO_PITS ||
                shared.mPitMode == (int)ePitMode.PIT_MODE_IN_PIT ||
                shared.mPitMode == (int)ePitMode.PIT_MODE_DRIVING_OUT_OF_PITS ||
                shared.mPitMode == (int)ePitMode.PIT_MODE_IN_GARAGE;
            currentGameState.PitData.IsAtPitExit = previousGameState != null && previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane;
            if (currentGameState.PitData.IsAtPitExit)
            {
                int lapCount = currentGameState.SessionData.CompletedLaps - 1;
                if (lapCount < 0)
                {
                    lapCount = 0;
                }
                currentGameState.PitData.LapCountWhenLastEnteredPits = lapCount;
            }
            else if (previousGameState != null)
            {
                currentGameState.PitData.LapCountWhenLastEnteredPits = previousGameState.PitData.LapCountWhenLastEnteredPits;
            }

            if (shared.mPitMode == (int)ePitMode.PIT_MODE_DRIVING_INTO_PITS)
            {
                currentGameState.PitData.OnInLap = true; 
                currentGameState.PitData.OnOutLap = false;
            }
            else if (shared.mPitMode == (int)ePitMode.PIT_MODE_DRIVING_OUT_OF_PITS || shared.mPitMode == (int)ePitMode.PIT_MODE_IN_GARAGE)
            {
                currentGameState.PitData.OnInLap = false;
                currentGameState.PitData.OnOutLap = true;
            }
            else if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.PitData.OnInLap = false;
                currentGameState.PitData.OnOutLap = false;
            }
            else if (previousGameState != null)
            {
                currentGameState.PitData.OnInLap = previousGameState.PitData.OnInLap;
                currentGameState.PitData.OnOutLap = previousGameState.PitData.OnOutLap;
            }
            currentGameState.PitData.HasRequestedPitStop = shared.mPitSchedule == (int)ePitSchedule.PIT_SCHEDULE_STANDARD;
            if (previousGameState != null && previousGameState.PitData.HasRequestedPitStop)
            {
                Console.WriteLine("Has requested pitstop");
            }
            currentGameState.CarDamageData.DamageEnabled = true;    // no way to tell if it's disabled from the shared memory
            currentGameState.CarDamageData.OverallAeroDamage = mapToAeroDamageLevel(shared.mAeroDamage);
            currentGameState.CarDamageData.OverallEngineDamage = mapToEngineDamageLevel(shared.mEngineDamage);
            currentGameState.CarDamageData.OverallTransmissionDamage = DamageLevel.NONE;
            currentGameState.CarDamageData.SuspensionDamageStatus = CornerData.getCornerData(suspensionDamageThresholds,
                shared.mSuspensionDamage[0], shared.mSuspensionDamage[1], shared.mSuspensionDamage[2], shared.mSuspensionDamage[3]);
            currentGameState.CarDamageData.BrakeDamageStatus = CornerData.getCornerData(brakeDamageThresholds,
                shared.mBrakeDamage[0], shared.mBrakeDamage[1], shared.mBrakeDamage[2], shared.mBrakeDamage[3]);

            currentGameState.EngineData.EngineOilPressure = shared.mOilPressureKPa; // todo: units conversion
            currentGameState.EngineData.EngineOilTemp = shared.mOilTempCelsius;
            currentGameState.EngineData.EngineWaterTemp = shared.mWaterTempCelsius;
            currentGameState.EngineData.EngineRpm = shared.mRPM;
            currentGameState.EngineData.MaxEngineRpm = shared.mMaxRPM;
            currentGameState.EngineData.MinutesIntoSessionBeforeMonitoring = 2;

            currentGameState.FuelData.FuelCapacity = shared.mFuelCapacity;
            currentGameState.FuelData.FuelLeft = currentGameState.FuelData.FuelCapacity * shared.mFuelLevel;
            currentGameState.FuelData.FuelPressure = shared.mFuelPressureKPa;
            currentGameState.FuelData.FuelUseActive = true;         // no way to tell if it's disabled

            currentGameState.PenaltiesData.HasDriveThrough = shared.mPitSchedule == (int)ePitSchedule.PIT_SCHEDULE_DRIVE_THROUGH;
            currentGameState.PenaltiesData.HasStopAndGo = shared.mPitSchedule == (int)ePitSchedule.PIT_SCHEDULE_STOP_GO;

            currentGameState.PositionAndMotionData.CarSpeed = shared.mSpeed;
            

            //------------------------ Tyre data -----------------------          
            currentGameState.TyreData.HasMatchedTyreTypes = true;
            currentGameState.TyreData.TireWearActive = true;

            currentGameState.TyreData.LeftFrontAttached = (shared.mTyreFlags[0] & 1) == 1;
            currentGameState.TyreData.RightFrontAttached = (shared.mTyreFlags[1] & 1) == 1;
            currentGameState.TyreData.LeftRearAttached = (shared.mTyreFlags[2] & 1) == 1;
            currentGameState.TyreData.RightRearAttached = (shared.mTyreFlags[3] & 1) == 1;

            currentGameState.TyreData.FrontLeft_CenterTemp = shared.mTyreTreadTemp[0] - 273;
            currentGameState.TyreData.FrontLeft_LeftTemp = shared.mTyreTreadTemp[0] - 273;
            currentGameState.TyreData.FrontLeft_RightTemp = shared.mTyreTreadTemp[0] - 273;
            currentGameState.TyreData.FrontLeftTyreType = defaultTyreTypeForPlayersCar;
            currentGameState.TyreData.FrontLeftPressure = -1; // not in the block
            currentGameState.TyreData.FrontLeftPercentWear = Math.Min(100, shared.mTyreWear[0] * 100 / wornOutTyreWearLevel);
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap = currentGameState.TyreData.FrontLeft_CenterTemp;
            }
            else if (previousGameState == null || currentGameState.TyreData.FrontLeft_CenterTemp > previousGameState.TyreData.PeakFrontLeftTemperatureForLap)
            {
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap = currentGameState.TyreData.FrontLeft_CenterTemp;
            }

            currentGameState.TyreData.FrontRight_CenterTemp = shared.mTyreTreadTemp[1] - 273;
            currentGameState.TyreData.FrontRight_LeftTemp = shared.mTyreTreadTemp[1] - 273;
            currentGameState.TyreData.FrontRight_RightTemp = shared.mTyreTreadTemp[1] - 273;
            currentGameState.TyreData.FrontRightTyreType = defaultTyreTypeForPlayersCar;
            currentGameState.TyreData.FrontRightPressure = -1; // not in the block
            currentGameState.TyreData.FrontRightPercentWear = Math.Min(100, shared.mTyreWear[1] * 100 / wornOutTyreWearLevel);
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakFrontRightTemperatureForLap = currentGameState.TyreData.FrontRight_CenterTemp;
            }
            else if (previousGameState == null || currentGameState.TyreData.FrontRight_CenterTemp > previousGameState.TyreData.PeakFrontRightTemperatureForLap)
            {
                currentGameState.TyreData.PeakFrontRightTemperatureForLap = currentGameState.TyreData.FrontRight_CenterTemp;
            }

            currentGameState.TyreData.RearLeft_CenterTemp = shared.mTyreTreadTemp[2] - 273;
            currentGameState.TyreData.RearLeft_LeftTemp = shared.mTyreTreadTemp[2] - 273;
            currentGameState.TyreData.RearLeft_RightTemp = shared.mTyreTreadTemp[2] - 273;
            currentGameState.TyreData.RearLeftTyreType = defaultTyreTypeForPlayersCar;
            currentGameState.TyreData.RearLeftPressure = -1; // not in the block
            currentGameState.TyreData.RearLeftPercentWear = Math.Min(100, shared.mTyreWear[2] * 100 / wornOutTyreWearLevel);
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakRearLeftTemperatureForLap = currentGameState.TyreData.RearLeft_CenterTemp;
            }
            else if (previousGameState == null || currentGameState.TyreData.RearLeft_CenterTemp > previousGameState.TyreData.PeakRearLeftTemperatureForLap)
            {
                currentGameState.TyreData.PeakRearLeftTemperatureForLap = currentGameState.TyreData.RearLeft_CenterTemp;
            }

            currentGameState.TyreData.RearRight_CenterTemp = shared.mTyreTreadTemp[3] - 273;
            currentGameState.TyreData.RearRight_LeftTemp = shared.mTyreTreadTemp[3] - 273;
            currentGameState.TyreData.RearRight_RightTemp = shared.mTyreTreadTemp[3] - 273;
            currentGameState.TyreData.RearRightTyreType = defaultTyreTypeForPlayersCar;
            currentGameState.TyreData.RearRightPressure = -1; // not in the block
            currentGameState.TyreData.RearRightPercentWear = Math.Min(100, shared.mTyreWear[3] * 100 / wornOutTyreWearLevel);
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakRearRightTemperatureForLap = currentGameState.TyreData.RearRight_CenterTemp;
            }
            else if (previousGameState == null || currentGameState.TyreData.RearRight_CenterTemp > previousGameState.TyreData.PeakRearRightTemperatureForLap)
            {
                currentGameState.TyreData.PeakRearRightTemperatureForLap = currentGameState.TyreData.RearRight_CenterTemp;
            }

            currentGameState.TyreData.TyreConditionStatus = CornerData.getCornerData(tyreWearThresholds, currentGameState.TyreData.FrontLeftPercentWear, 
                currentGameState.TyreData.FrontRightPercentWear, currentGameState.TyreData.RearLeftPercentWear, currentGameState.TyreData.RearRightPercentWear);

            currentGameState.TyreData.TyreTempStatus = CornerData.getCornerData(CarData.tyreTempThresholds[defaultTyreTypeForPlayersCar],
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap, currentGameState.TyreData.PeakFrontRightTemperatureForLap,
                currentGameState.TyreData.PeakRearLeftTemperatureForLap, currentGameState.TyreData.PeakRearRightTemperatureForLap);

            currentGameState.TyreData.BrakeTempStatus = CornerData.getCornerData(brakeTempThresholdsForPlayersCar, 
                shared.mBrakeTempCelsius[0], shared.mBrakeTempCelsius[1], shared.mBrakeTempCelsius[2], shared.mBrakeTempCelsius[3]);
            currentGameState.TyreData.LeftFrontBrakeTemp = shared.mBrakeTempCelsius[0];
            currentGameState.TyreData.RightFrontBrakeTemp = shared.mBrakeTempCelsius[1];
            currentGameState.TyreData.LeftRearBrakeTemp = shared.mBrakeTempCelsius[2];
            currentGameState.TyreData.RightRearBrakeTemp = shared.mBrakeTempCelsius[0];

            // improvised cut track warnings...
            if (incrementCutTrackCountWhenLeavingRacingSurface)
            {
                currentGameState.PenaltiesData.IsOffRacingSurface = !racingSurfaces.Contains(shared.mTerrain[0]) &&
               !racingSurfaces.Contains(shared.mTerrain[1]) && !racingSurfaces.Contains(shared.mTerrain[2]) &&
               !racingSurfaces.Contains(shared.mTerrain[3]);
                if (previousGameState != null && previousGameState.PenaltiesData.IsOffRacingSurface && currentGameState.PenaltiesData.IsOffRacingSurface)
                {
                    currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings + 1;
                }
            }
            if (!currentGameState.PitData.OnOutLap && previousGameState != null && previousGameState.SessionData.CurrentLapIsValid && !currentGameState.SessionData.CurrentLapIsValid &&
                !(shared.mSessionState == (int)eSessionState.SESSION_RACE && shared.mRaceState == (int)eRaceState.RACESTATE_NOT_STARTED))
            {
                currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings + 1;
            }
            // Tyre slip speed seems to peak at about 30 with big lock or wheelspin (in Sauber Merc). It's noisy as hell and is frequently bouncing around
            // in single figures, with the noise varying between cars.
            // tyreRPS is much cleaner but we don't know the diameter of the tyre so can't compare it (accurately) to the car's speed
            if (shared.mSpeed > 7)
            {
                float minRotatingSpeed = 2 * (float)Math.PI * shared.mSpeed / currentGameState.carClass.maxTyreCircumference;
                // I think the tyreRPS is actually radians per second...
                currentGameState.TyreData.LeftFrontIsLocked = Math.Abs(shared.mTyreRPS[0]) < minRotatingSpeed;
                currentGameState.TyreData.RightFrontIsLocked = Math.Abs(shared.mTyreRPS[1]) < minRotatingSpeed;
                currentGameState.TyreData.LeftRearIsLocked = Math.Abs(shared.mTyreRPS[2]) < minRotatingSpeed;
                currentGameState.TyreData.RightRearIsLocked = Math.Abs(shared.mTyreRPS[3]) < minRotatingSpeed;

                float maxRotatingSpeed = 2 * (float)Math.PI * shared.mSpeed / currentGameState.carClass.minTyreCircumference;
                currentGameState.TyreData.LeftFrontIsSpinning = Math.Abs(shared.mTyreRPS[0]) > maxRotatingSpeed;
                currentGameState.TyreData.RightFrontIsSpinning = Math.Abs(shared.mTyreRPS[1]) > maxRotatingSpeed;
                currentGameState.TyreData.LeftRearIsSpinning = Math.Abs(shared.mTyreRPS[2]) > maxRotatingSpeed;
                currentGameState.TyreData.RightRearIsSpinning = Math.Abs(shared.mTyreRPS[3]) > maxRotatingSpeed;
            }

            if (currentGameState.Conditions.timeOfMostRecentSample.Add(ConditionsMonitor.ConditionsSampleFrequency) < currentGameState.Now)
            {
                currentGameState.Conditions.addSample(currentGameState.Now, currentGameState.SessionData.CompletedLaps, currentGameState.SessionData.SectorNumber,
                    shared.mAmbientTemperature, shared.mTrackTemperature, shared.mRainDensity, shared.mWindSpeed, shared.mWindDirectionX, shared.mWindDirectionY, shared.mCloudBrightness);
            }
            return currentGameState;
        }

        private DamageLevel mapToAeroDamageLevel(float aeroDamage)
        {
            if (aeroDamage >= destroyedAeroDamageThreshold)
            {
                return DamageLevel.DESTROYED;
            }
            else if (aeroDamage >= severeAeroDamageThreshold)
            {
                return DamageLevel.MAJOR;
            }
            else if (aeroDamage >= minorAeroDamageThreshold)
            {
                return DamageLevel.MINOR;
            }
            else if (aeroDamage >= trivialAeroDamageThreshold)
            {
                return DamageLevel.TRIVIAL;
            } 
            else
            {
                return DamageLevel.NONE;
            }
        }
        private DamageLevel mapToEngineDamageLevel(float engineDamage)
        {
            if (engineDamage >= destroyedEngineDamageThreshold)
            {
                return DamageLevel.DESTROYED;
            }
            else if (engineDamage >= severeEngineDamageThreshold)
            {
                return DamageLevel.MAJOR;
            }
            else if (engineDamage >= minorEngineDamageThreshold)
            {
                return DamageLevel.MINOR;
            }
            else if (engineDamage >= trivialEngineDamageThreshold)
            {
                return DamageLevel.TRIVIAL;
            }
            else
            {
                return DamageLevel.NONE;
            }
        }

        private void upateOpponentData(OpponentData opponentData, int racePosition, int completedLaps, int sector, Boolean isInPits,
            float sessionRunningTime, float secondsSinceLastUpdate, float[] currentWorldPosition, float[] previousWorldPosition,
            float previousSpeed, float worldRecordLapTime, float distanceRoundTrack, Boolean isRaining, float trackTemp, float airTemp)
        {
            opponentData.DistanceRoundTrack = distanceRoundTrack;
            float speed;
            Boolean validSpeed = true;
            if ((currentWorldPosition[0] == 0 && currentWorldPosition[1] == 0) || (previousWorldPosition[0] == 0 && previousWorldPosition[1] == 0))
            {
                speed = previousSpeed;
            }
            else 
            {
                speed = (float)Math.Sqrt(Math.Pow(currentWorldPosition[0] - previousWorldPosition[0], 2) + Math.Pow(currentWorldPosition[1] - previousWorldPosition[1], 2)) / secondsSinceLastUpdate;
            }
            if (speed > 500)
            {
                // faster than 500m/s (1000+mph) suggests the player has quit to the pit. Might need to reassess this as the data are quite noisy
                validSpeed = false;
                opponentData.Speed = 0;
            }
            else if (speed == 0 && previousSpeed > 5)
            {
                if (previousSpeedReuseCount.ContainsKey(opponentData.DriverRawName))
                {
                    if (previousSpeedReuseCount[opponentData.DriverRawName] > 5)
                    {
                        // we've reused 5 previous values, reset
                        previousSpeedReuseCount[opponentData.DriverRawName] = 0;
                    }
                    else
                    {
                        speed = previousSpeed;
                        previousSpeedReuseCount[opponentData.DriverRawName] = previousSpeedReuseCount[opponentData.DriverRawName] + 1;
                    }
                }
                else
                {
                    previousSpeedReuseCount.Add(opponentData.DriverRawName, 1);
                    speed = previousSpeed;
                }
            }
            opponentData.Speed = speed;
            opponentData.Position = racePosition;
            opponentData.UnFilteredPosition = racePosition;
            opponentData.WorldPosition = currentWorldPosition;
            opponentData.IsNewLap = false;
            if (opponentData.CurrentSectorNumber != sector)
            {
                if (opponentData.CurrentSectorNumber == 3 && sector == 1)
                {
                    // use -1 for provided lap time and let the AddSectorData method calculate it from the game time
                    if (opponentData.OpponentLapData.Count > 0)
                    {
                        opponentData.CompleteLapWithEstimatedLapTime(racePosition, sessionRunningTime, worldRecordLapTime, validSpeed,
                            isRaining, trackTemp, airTemp);
                    }
                    opponentData.StartNewLap(completedLaps + 1, racePosition, isInPits, sessionRunningTime, isRaining, trackTemp, airTemp);
                    opponentData.IsNewLap = true;
                }
                else if (opponentData.CurrentSectorNumber == 1 && sector == 2 || opponentData.CurrentSectorNumber == 2 && sector == 3)
                {
                    opponentData.AddSectorData(racePosition, -1, sessionRunningTime, validSpeed, isRaining, trackTemp, airTemp);
                }
                opponentData.CurrentSectorNumber = sector;
            }
            if (sector == 3 && isInPits) 
            {
                opponentData.setInLap();
            }
            opponentData.CompletedLaps = completedLaps;
        }

        private OpponentData createOpponentData(pCarsAPIParticipantStruct participantStruct, Boolean loadDriverName)
        {            
            OpponentData opponentData = new OpponentData();
            opponentData.DriverRawName = participantStruct.mName.Trim();
            if (loadDriverName && CrewChief.enableDriverNames)
            {
                speechRecogniser.addNewOpponentName(opponentData.DriverRawName);
            }
            opponentData.Position = (int)participantStruct.mRacePosition;
            opponentData.UnFilteredPosition = opponentData.Position;
            opponentData.CompletedLaps = (int)participantStruct.mLapsCompleted;
            opponentData.CurrentSectorNumber = (int)participantStruct.mCurrentSector;
            opponentData.WorldPosition = new float[] { participantStruct.mWorldPosition[0], participantStruct.mWorldPosition[2] };
            opponentData.DistanceRoundTrack = participantStruct.mCurrentLapDistance;
            return opponentData;
        }
        
        /*
         * Race state changes - start race, skip practice to end of session, then into race:
         * 
         * pre race practice initial - sessionState = SESSION_TEST, raceState = not started 
         * pre race practice after pit exit - sessionState = SESSION_TEST, raceState = racing
         * skip to end - sessionState = SESSION_TEST, raceState = not started 
         * load race - sessionState = NO_SESSION, raceState = not started 
         * grid walk - sessionState = SESSION_RACE, raceState = racing
         * 
         * TODO: other session types. The "SESSION_TEST" above is actually the warmup. Presumably
         * an event with prac -> qual -> warmup -> race would use SESSION_PRACTICE
         * */
        public SessionType mapToSessionType(Object memoryMappedFileStruct)
        {
            pCarsAPIStruct shared = (pCarsAPIStruct)memoryMappedFileStruct;
            uint sessionState = shared.mSessionState;
            if (sessionState == (uint)eSessionState.SESSION_RACE || sessionState == (uint)eSessionState.SESSION_FORMATIONLAP)
            {
                return SessionType.Race;
            }
            else if (sessionState == (uint)eSessionState.SESSION_PRACTICE || sessionState == (uint)eSessionState.SESSION_TEST)
            {
                return SessionType.Practice;
            } 
            else if (sessionState == (uint)eSessionState.SESSION_QUALIFY)
            {
                return SessionType.Qualify;
            }
            else if (sessionState == (uint)eSessionState.SESSION_TIME_ATTACK)
            {
                return SessionType.HotLap;
            }
            else
            {
                return SessionType.Unavailable;
            }
        }

        // TODO: this has been hacked around so much as to have become entirely bollocks. Re-write it
        /*
         * When a practice session ends (while driving around) the mSessionState goes from 2 (racing) to
         * 1 (race not started) to 0 (invalid) over a few seconds. It never goes to any of the finished
         * states
         */
        private SessionPhase mapToSessionPhase(SessionType sessionType, uint sessionState, uint raceState, int numParticipants,
            Boolean leaderHasFinishedRace, SessionPhase previousSessionPhase, float sessionTimeRemaining, float sessionRunTime)
        {
            if (numParticipants < 1)
            {
                return SessionPhase.Unavailable;
            }
            if (sessionType == SessionType.Race)
            {
                if (raceState == (uint)eRaceState.RACESTATE_NOT_STARTED)
                {
                    if (sessionState == (uint)eSessionState.SESSION_FORMATIONLAP)
                    {
                        return SessionPhase.Formation;
                    }
                    else
                    {
                        return SessionPhase.Countdown;
                    }
                }
                else if (raceState == (uint)eRaceState.RACESTATE_RACING)
                {
                    if (leaderHasFinishedRace)
                    {
                        return SessionPhase.Checkered;
                    }
                    else
                    {
                        return SessionPhase.Green;
                    }
                }
                else if (raceState == (uint)eRaceState.RACESTATE_FINISHED ||
                    raceState == (uint)eRaceState.RACESTATE_DNF ||
                    raceState == (uint)eRaceState.RACESTATE_DISQUALIFIED ||
                    raceState == (uint)eRaceState.RACESTATE_RETIRED ||
                    raceState == (uint)eRaceState.RACESTATE_INVALID ||
                    raceState == (uint)eRaceState.RACESTATE_MAX)
                {
                    return SessionPhase.Finished;
                }
            }
            else if (sessionType == SessionType.Practice || sessionType == SessionType.Qualify)
            {
                // yeah yeah....
                if (sessionRunTime > 0 && sessionTimeRemaining <= 1)
                {
                    return SessionPhase.Finished;
                } 
                else if (raceState == (uint)eRaceState.RACESTATE_RACING) 
                {
                    return SessionPhase.Green;
                }
                else if (raceState == (uint)eRaceState.RACESTATE_NOT_STARTED)
                {
                    // for these sessions, we'll be in 'countdown' for all of the first lap :(
                    return SessionPhase.Countdown;
                }
            }
            return SessionPhase.Unavailable;
        }

        private TyreCondition getTyreCondition(float percentWear)
        {
            if (percentWear <= -1)
            {
                return TyreCondition.UNKNOWN;
            }
            if (percentWear >= wornOutTyreWearPercent)
            {
                return TyreCondition.WORN_OUT;
            }
            else if (percentWear >= majorTyreWearPercent)
            {
                return TyreCondition.MAJOR_WEAR;
            }
            if (percentWear >= minorTyreWearPercent)
            {
                return TyreCondition.MINOR_WEAR;
            }
            if (percentWear >= scrubbedTyreWearPercent)
            {
                return TyreCondition.SCRUBBED;
            }
            else
            {
                return TyreCondition.NEW;
            }
        }

        private FlagEnum mapToFlagEnum(uint highestFlagColour)
        {
            if (highestFlagColour == (uint) eFlagColors.FLAG_COLOUR_CHEQUERED)
            {
                return FlagEnum.CHEQUERED;
            }
            else if (highestFlagColour == (uint) eFlagColors.FLAG_COLOUR_BLACK) 
            {
                return FlagEnum.BLACK;
            }
            else if (highestFlagColour == (uint)eFlagColors.FLAG_COLOUR_DOUBLE_YELLOW) 
            {
                return FlagEnum.DOUBLE_YELLOW;
            }
            else if (highestFlagColour == (uint)eFlagColors.FLAG_COLOUR_YELLOW) 
            {
                return FlagEnum.YELLOW;
            }
            else if (highestFlagColour == (uint)eFlagColors.FLAG_COLOUR_WHITE) 
            {
                return FlagEnum.WHITE;
            }
            else if (highestFlagColour == (uint)eFlagColors.FLAG_COLOUR_BLUE) 
            {
                return FlagEnum.BLUE;
            }
            else if (highestFlagColour == (uint)eFlagColors.FLAG_COLOUR_GREEN) 
            {
                return FlagEnum.GREEN;
            }
            return FlagEnum.UNKNOWN;
        }

        private float getMean(List<float> data)
        {
            if (data.Count == 0)
            {
                return 0;
            }
            float mean = 0;
            int count = 0;
            foreach (float d in data)
            {
                count++;
                mean += d;
            }
            return mean / count;
        }
    }
}
