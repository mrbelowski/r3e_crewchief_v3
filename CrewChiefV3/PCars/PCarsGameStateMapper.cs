using System;
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
        Dictionary<int, DateTime> limiterCheckSchedule = new Dictionary<int, DateTime>();

        private class LocationAndTime
        {
            public float x;
            public float y;
            public long ticks;
            public LocationAndTime(float x, float y, long ticks)
            {
                this.x = x;
                this.y = y;
                this.ticks = ticks;
            }
        }

        private List<CornerData.EnumWithThresholds> suspensionDamageThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> tyreWearThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> brakeDamageThresholds = new List<CornerData.EnumWithThresholds>();

        // these are set when we start a new session, from the car name / class
        private TyreType defaultTyreTypeForPlayersCar = TyreType.Unknown_Race;
        private List<CornerData.EnumWithThresholds> brakeTempThresholdsForPlayersCar = null;


        // for each opponent, this is a list of his x/y locations + the time the location was recorded (in ticks). This is used to detect pit entry, so is
        // populated only during sector 3 and cleared at sector 1
        private Dictionary<int, List<LocationAndTime>> OpponentWorldPositions = new Dictionary<int, List<LocationAndTime>>();

        // if all 4 wheels are off the racing surface, increment the number of cut track incedents
        private Boolean incrementCutTrackCountWhenLeavingRacingSurface = false;

        private static uint expectedVersion = 5;

        private List<uint> racingSurfaces = new List<uint>() { (uint)eTerrain.TERRAIN_BUMPY_DIRT_ROAD, 
            (uint)eTerrain.TERRAIN_BUMPY_ROAD1, (uint)eTerrain.TERRAIN_BUMPY_ROAD2, (uint)eTerrain.TERRAIN_BUMPY_ROAD3, 
            (uint)eTerrain.TERRAIN_COBBLES, (uint)eTerrain.TERRAIN_DRAINS, (uint)eTerrain.TERRAIN_EXIT_RUMBLE_STRIPS,
            (uint)eTerrain.TERRAIN_LOW_GRIP_ROAD, (uint)eTerrain.TERRAIN_MARBLES,(uint)eTerrain.TERRAIN_PAVEMENT,
            (uint)eTerrain.TERRAIN_ROAD, (uint)eTerrain.TERRAIN_RUMBLE_STRIPS, (uint)eTerrain.TERRAIN_SAND_ROAD};

        private float trivialDamageThreshold = 0.02f;
        private float minorDamageThreshold = 0.10f;
        private float severeDamageThreshold = 0.35f;
        private float destroyedDamageThreshold = 0.70f;

        // tyres in PCars are worn out when the wear level is > ?
        private float wornOutTyreWearLevel = 0.50f;

        private float scrubbedTyreWearPercent = 1f;
        private float minorTyreWearPercent = 20f;
        private float majorTyreWearPercent = 40f;
        private float wornOutTyreWearPercent = 80f;

        private TimeSpan minimumSessionParticipationTime = TimeSpan.FromSeconds(6);

        public PCarsGameStateMapper()
        {
            CornerData.EnumWithThresholds damageNone = new CornerData.EnumWithThresholds(DamageLevel.NONE, -10000, trivialDamageThreshold);
            CornerData.EnumWithThresholds damageTrivial = new CornerData.EnumWithThresholds(DamageLevel.TRIVIAL, trivialDamageThreshold, minorDamageThreshold);
            CornerData.EnumWithThresholds damageMinor = new CornerData.EnumWithThresholds(DamageLevel.MINOR, trivialDamageThreshold, severeDamageThreshold);
            CornerData.EnumWithThresholds damageMajor = new CornerData.EnumWithThresholds(DamageLevel.MAJOR, severeDamageThreshold, destroyedDamageThreshold);
            CornerData.EnumWithThresholds damageDestroyed = new CornerData.EnumWithThresholds(DamageLevel.DESTROYED, destroyedDamageThreshold, 10000);
            suspensionDamageThresholds.Add(damageNone);
            suspensionDamageThresholds.Add(damageTrivial);
            suspensionDamageThresholds.Add(damageMinor);
            suspensionDamageThresholds.Add(damageMajor);
            suspensionDamageThresholds.Add(damageDestroyed);

            brakeDamageThresholds.Add(damageNone);
            brakeDamageThresholds.Add(damageTrivial);
            brakeDamageThresholds.Add(damageMinor);
            brakeDamageThresholds.Add(damageMajor);
            brakeDamageThresholds.Add(damageDestroyed);

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

        public GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            pCarsAPIStruct shared = ((CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)memoryMappedFileStruct).data;
            long ticks = ((CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)memoryMappedFileStruct).ticksWhenRead;
            
            GameStateData currentGameState = new GameStateData(ticks);

            if (shared.mNumParticipants < 1 || 
                ((shared.mEventTimeRemaining == -1 || shared.mEventTimeRemaining == 0) && shared.mLapsInEvent == 0) ||
                shared.mTrackLocation == null || shared.mTrackLocation.Length == 0)
            {
                // Unusable data in the block
                return null;
            }
            
            pCarsAPIParticipantStruct viewedParticipant = shared.mParticipantData[shared.mViewedParticipantIndex];
            currentGameState.SessionData.CompletedLaps = (int)viewedParticipant.mLapsCompleted;
            currentGameState.SessionData.SectorNumber = (int)viewedParticipant.mCurrentSector;
            currentGameState.SessionData.Position = (int)viewedParticipant.mRacePosition;
            currentGameState.SessionData.IsNewSector = previousGameState == null || viewedParticipant.mCurrentSector != previousGameState.SessionData.SectorNumber;
                        
            currentGameState.PositionAndMotionData.DistanceRoundTrack = viewedParticipant.mCurrentLapDistance;
          
            
            // previous session data to check if we've started an new session
            SessionPhase lastSessionPhase = SessionPhase.Unavailable;
            SessionType lastSessionType = SessionType.Unavailable;
            float lastSessionRunningTime = 0;
            int lastSessionLapsCompleted = 0;
            String lastSessionTrack = "";
            String lastSessionTrackLayout = "";
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
                lastSessionTrack = previousGameState.SessionData.TrackName;
                lastSessionTrackLayout = previousGameState.SessionData.TrackLayout;
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
            currentGameState.SessionData.TrackName = shared.mTrackLocation;
            currentGameState.SessionData.TrackLayout = shared.mTrackVariation;
            
            int opponentSlotId = 0;

            // now check if this is a new session...
            if (currentGameState.SessionData.SessionType != SessionType.Unavailable && (lastSessionType != currentGameState.SessionData.SessionType ||
                lastSessionHasFixedTime != currentGameState.SessionData.SessionHasFixedTime || lastSessionTrack != currentGameState.SessionData.TrackName ||
                lastSessionTrackLayout != currentGameState.SessionData.TrackLayout || lastSessionLapsCompleted > currentGameState.SessionData.CompletedLaps ||
                (numberOfLapsInSession > 0 && lastSessionNumberOfLaps > 0 && lastSessionNumberOfLaps != numberOfLapsInSession) ||
                (sessionTimeRemaining > 0 && sessionTimeRemaining > lastSessionRunTime)))
            {
                Console.WriteLine("New session, trigger...");  
                if (lastSessionType != currentGameState.SessionData.SessionType) 
                {
                    Console.WriteLine("lastSessionType = " + lastSessionType + " currentGameState.SessionData.SessionType = " + currentGameState.SessionData.SessionType);
                }
                else if (lastSessionHasFixedTime != currentGameState.SessionData.SessionHasFixedTime) 
                {
                    Console.WriteLine("lastSessionHasFixedTime = " + lastSessionHasFixedTime + " currentGameState.SessionData.SessionHasFixedTime = " + currentGameState.SessionData.SessionHasFixedTime);
                }
                else if (lastSessionTrack != currentGameState.SessionData.TrackName)
                {
                    Console.WriteLine("lastSessionTrack "+ lastSessionTrack + " currentGameState.SessionData.TrackName = " +currentGameState.SessionData.TrackName);
                }
                else if (lastSessionTrackLayout != currentGameState.SessionData.TrackLayout)
                {
                    Console.WriteLine("lastSessionTrackLayout = " + lastSessionTrackLayout +" currentGameState.SessionData.TrackLayout = " + currentGameState.SessionData.TrackLayout);
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
                currentGameState.SessionData.TrackLength = shared.mTrackLength;
                currentGameState.SessionData.SessionNumberOfLaps = numberOfLapsInSession;
                currentGameState.SessionData.LeaderHasFinishedRace = false;
                currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                currentGameState.SessionData.SessionStartPosition = (int)shared.mParticipantData[shared.mViewedParticipantIndex].mRacePosition;
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
                currentGameState.SessionData.DriverRawName = shared.mParticipantData[shared.mViewedParticipantIndex].mName;
                currentGameState.PitData.IsRefuellingAllowed = true;

                foreach (pCarsAPIParticipantStruct participantStruct in shared.mParticipantData)
                {
                    if (participantStruct.mIsActive)
                    {
                        if (shared.mViewedParticipantIndex != opponentSlotId)
                        {
                            if (!currentGameState.OpponentData.ContainsKey(opponentSlotId))
                            {
                                currentGameState.OpponentData.Add(opponentSlotId, createOpponentData(participantStruct));
                                if (OpponentWorldPositions.ContainsKey(opponentSlotId))
                                {
                                    OpponentWorldPositions[opponentSlotId].Clear();
                                }
                                else
                                {
                                    OpponentWorldPositions.Add(opponentSlotId, new List<LocationAndTime>());
                                }
                            }
                        }
                        opponentSlotId++;
                    }
                }
                currentGameState.carClass = CarData.getCarClass(shared.mCarClassName);
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
                            currentGameState.SessionData.SessionStartPosition = (int)shared.mParticipantData[shared.mViewedParticipantIndex].mRacePosition;
                        }          
                        currentGameState.SessionData.TrackLength = shared.mTrackLength;
                        currentGameState.SessionData.LeaderHasFinishedRace = false;
                        currentGameState.SessionData.NumCarsAtStartOfSession = shared.mNumParticipants;

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
                        Console.WriteLine("HasMandatoryPitStop " + currentGameState.SessionData.HasMandatoryPitStop);
                        Console.WriteLine("PitWindowStart " + currentGameState.SessionData.PitWindowStart);
                        Console.WriteLine("PitWindowEnd " + currentGameState.SessionData.PitWindowEnd);
                        Console.WriteLine("NumCarsAtStartOfSession " + currentGameState.SessionData.NumCarsAtStartOfSession);
                        Console.WriteLine("SessionNumberOfLaps " + currentGameState.SessionData.SessionNumberOfLaps);
                        Console.WriteLine("SessionRunTime " + currentGameState.SessionData.SessionRunTime);
                        Console.WriteLine("SessionStartPosition " + currentGameState.SessionData.SessionStartPosition);
                        Console.WriteLine("SessionStartTime " + currentGameState.SessionData.SessionStartTime);
                        Console.WriteLine("TrackName " + currentGameState.SessionData.TrackName);
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
                    currentGameState.SessionData.TrackLength = previousGameState.SessionData.TrackLength;
                    currentGameState.SessionData.EventIndex = previousGameState.SessionData.EventIndex;
                    currentGameState.SessionData.SessionIteration = previousGameState.SessionData.SessionIteration;
                    currentGameState.SessionData.PitWindowStart = previousGameState.SessionData.PitWindowStart;
                    currentGameState.SessionData.PitWindowEnd = previousGameState.SessionData.PitWindowEnd;
                    currentGameState.SessionData.HasMandatoryPitStop = previousGameState.SessionData.HasMandatoryPitStop;
                    currentGameState.OpponentData = previousGameState.OpponentData;
                    currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                    currentGameState.SessionData.SessionTimeAtEndOfLastSector1 = previousGameState.SessionData.SessionTimeAtEndOfLastSector1;
                    currentGameState.SessionData.SessionTimeAtEndOfLastSector2 = previousGameState.SessionData.SessionTimeAtEndOfLastSector2;
                    currentGameState.SessionData.SessionTimeAtEndOfLastSector3 = previousGameState.SessionData.SessionTimeAtEndOfLastSector3;
                    currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings;
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
                    currentGameState.SessionData.SessionTimeAtEndOfLastSector3 = currentGameState.SessionData.SessionRunningTime;
                }
                else if (currentGameState.SessionData.SectorNumber == 2)
                {
                    currentGameState.SessionData.SessionTimeAtEndOfLastSector1 = currentGameState.SessionData.SessionRunningTime;
                }
                if (currentGameState.SessionData.SectorNumber == 3)
                {
                    currentGameState.SessionData.SessionTimeAtEndOfLastSector2 = currentGameState.SessionData.SessionRunningTime;
                }
            }
            currentGameState.SessionData.Flag = mapToFlagEnum(shared.mHighestFlagColour);
            currentGameState.SessionData.NumCars = shared.mNumParticipants;
            currentGameState.SessionData.CurrentLapIsValid = !shared.mLapInvalidated;                
            currentGameState.SessionData.IsNewLap = previousGameState == null || currentGameState.SessionData.CompletedLaps == previousGameState.SessionData.CompletedLaps + 1;
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.SessionData.PreviousLapWasValid = previousGameState != null && previousGameState.SessionData.CurrentLapIsValid;
            }
            else if (previousGameState != null)
            {
                currentGameState.SessionData.PreviousLapWasValid = previousGameState.SessionData.PreviousLapWasValid;
            }
            

            currentGameState.SessionData.IsRacingSameCarBehind = previousGameState != null && previousGameState.getOpponentIdBehind() == currentGameState.getOpponentIdBehind();
            currentGameState.SessionData.IsRacingSameCarInFront = previousGameState != null && previousGameState.getOpponentIdInFront() == currentGameState.getOpponentIdInFront();

            currentGameState.SessionData.LapTimePrevious = shared.mLastLapTime;
            if (previousGameState != null && previousGameState.SessionData.LapTimeBestPlayer > 0)
            {
                if (shared.mLastLapTime > 0 && shared.mLastLapTime < previousGameState.SessionData.LapTimeBestPlayer)
                {
                    currentGameState.SessionData.LapTimeBestPlayer = shared.mLastLapTime;
                }
                else
                {
                    currentGameState.SessionData.LapTimeBestPlayer = previousGameState.SessionData.LapTimeBestPlayer;
                }
            }
            else
            {
                currentGameState.SessionData.LapTimeBestPlayer = shared.mLastLapTime;
            }

            if (previousGameState == null || !currentGameState.SessionData.IsNewSector)
            {
                currentGameState.SessionData.LapTimeSessionBest = currentGameState.getBestOpponentLapTime();
                currentGameState.SessionData.LapTimeSessionBestPlayerClass = currentGameState.SessionData.LapTimeSessionBest;
            }
            currentGameState.SessionData.LapTimeCurrent = shared.mCurrentTime;
            currentGameState.SessionData.LapTimeDeltaSelf = shared.mLastLapTime - currentGameState.SessionData.LapTimeBestPlayer;
            currentGameState.SessionData.TimeDeltaBehind = shared.mSplitTimeBehind;
            currentGameState.SessionData.TimeDeltaFront = shared.mSplitTimeAhead;

            opponentSlotId = 0;
            foreach (pCarsAPIParticipantStruct participantStruct in shared.mParticipantData)
            {
                if (shared.mViewedParticipantIndex != opponentSlotId)
                {
                    if (currentGameState.OpponentData.ContainsKey(opponentSlotId))
                    {
                        if (currentGameState.OpponentData[opponentSlotId].IsActive && participantStruct.mIsActive)
                        {
                            if (previousGameState != null && previousGameState.OpponentData.Count == currentGameState.OpponentData.Count) 
                            {
                                OpponentData previousOpponentData = previousGameState.OpponentData[opponentSlotId];
                                int currentOpponentRacePosition = (int) participantStruct.mRacePosition;
                                int currentOpponentLapsCompleted = (int) participantStruct.mLapsCompleted;
                                int currentOpponentSector = (int) participantStruct.mCurrentSector;
                                if (currentOpponentSector == 0)
                                {
                                    currentOpponentSector = previousOpponentData.CurrentSectorNumber;
                                }
                                float currentOpponentLapDistance = participantStruct.mCurrentLapDistance;
                                double secondsBetweenPoints = ((double)currentGameState.Ticks - (double)previousGameState.Ticks) / (double)TimeSpan.TicksPerSecond;
                                
                                if (currentOpponentRacePosition == 1 && (currentGameState.SessionData.SessionNumberOfLaps > 0 && 
                                        currentGameState.SessionData.SessionNumberOfLaps == currentOpponentLapsCompleted) ||
                                        (currentGameState.SessionData.SessionRunTime > 0 && currentGameState.SessionData.SessionTimeRemaining < 1 &&
                                        previousOpponentData.CompletedLaps < currentOpponentLapsCompleted))
                                {
                                    currentGameState.SessionData.LeaderHasFinishedRace = true;
                                }
                                if (currentOpponentRacePosition == 1 && previousOpponentData.Position != 1)
                                {
                                    currentGameState.SessionData.HasLeadChanged = true;
                                }
                                int opponentPositionAtSector3 = previousOpponentData.Position;
                                Boolean isEnteringPits = false;
                                if (currentOpponentSector == 3 && currentGameState.SessionData.SessionRunningTime > 30)
                                {
                                    if (previousOpponentData.CurrentSectorNumber == 2)
                                    {
                                        if (limiterCheckSchedule.ContainsKey(opponentSlotId))
                                        {
                                            limiterCheckSchedule[opponentSlotId] = currentGameState.Now.AddSeconds(1);
                                        }
                                        else
                                        {
                                            limiterCheckSchedule.Add(opponentSlotId, currentGameState.Now.AddSeconds(1));
                                        }
                                        opponentPositionAtSector3 = currentOpponentRacePosition;
                                        if (OpponentWorldPositions.ContainsKey(opponentSlotId))
                                        {
                                            OpponentWorldPositions[opponentSlotId].Clear();
                                        }
                                        else
                                        {
                                            OpponentWorldPositions.Add(opponentSlotId, new List<LocationAndTime>());
                                        }
                                    }
                                    else if (!previousOpponentData.IsEnteringPits)
                                    {
                                        if (OpponentWorldPositions.ContainsKey(opponentSlotId))
                                        {
                                            OpponentWorldPositions[opponentSlotId].Add(new LocationAndTime(participantStruct.mWorldPosition[0],
                                                participantStruct.mWorldPosition[2], currentGameState.Ticks));
                                            if (limiterCheckSchedule.ContainsKey(opponentSlotId) && currentGameState.Now > limiterCheckSchedule[opponentSlotId])
                                            {
                                                isEnteringPits = IsOpponentOnPitLimiter(opponentSlotId);
                                                if (isEnteringPits)
                                                {
                                                    Console.WriteLine("opponent at position " + currentOpponentRacePosition + " is entering pits");
                                                    limiterCheckSchedule[opponentSlotId] = DateTime.MaxValue;
                                                }
                                                else
                                                {
                                                    limiterCheckSchedule[opponentSlotId] = currentGameState.Now.AddSeconds(1);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        isEnteringPits = previousOpponentData.IsEnteringPits;
                                    }
                                }                                

                                if (isEnteringPits && !previousOpponentData.IsEnteringPits)
                                {
                                    if (opponentPositionAtSector3 == 1)
                                    {
                                        Console.WriteLine("leader pitting, pos at sector 3 = " + opponentPositionAtSector3 + " current pos = " + currentOpponentRacePosition);
                                        currentGameState.PitData.LeaderIsPitting = true;
                                    }
                                    if (currentGameState.SessionData.Position > 2 && opponentPositionAtSector3 == currentGameState.SessionData.Position - 1)
                                    {
                                        Console.WriteLine("car in front pitting, pos at sector 3 = " + opponentPositionAtSector3 + " current pos = " + currentOpponentRacePosition);
                                        currentGameState.PitData.CarInFrontIsPitting = true;
                                    }
                                    if (!currentGameState.isLast() && opponentPositionAtSector3 == currentGameState.SessionData.Position + 1)
                                    {
                                        Console.WriteLine("car behind pitting, pos at sector 3 = " + opponentPositionAtSector3 + " current pos = " + currentOpponentRacePosition);
                                        currentGameState.PitData.CarBehindIsPitting = true;
                                    }
                                }
                                float secondsSinceLastUpdate = (float)new TimeSpan(currentGameState.Ticks - previousGameState.Ticks).TotalSeconds;
                                upateOpponentData(currentGameState.OpponentData[opponentSlotId], currentOpponentRacePosition, currentOpponentLapsCompleted,
                                        currentOpponentSector, isEnteringPits, currentGameState.SessionData.SessionRunningTime, secondsSinceLastUpdate, 
                                        opponentPositionAtSector3, new float[] { participantStruct.mWorldPosition[0], participantStruct.mWorldPosition[2]}, previousOpponentData.WorldPosition,
                                        previousOpponentData.Speed);
                            }
                        }                            
                        else
                        {
                            currentGameState.OpponentData[opponentSlotId].IsActive = false;
                        }
                    }
                    else
                    {
                        if (participantStruct.mIsActive)
                        {
                            Console.WriteLine("Creating opponent for slot " + opponentSlotId);
                            currentGameState.OpponentData.Add(opponentSlotId, createOpponentData(participantStruct));
                            if (OpponentWorldPositions.ContainsKey(opponentSlotId))
                            {
                                OpponentWorldPositions[opponentSlotId].Clear();
                            }
                            else
                            {
                                OpponentWorldPositions.Add(opponentSlotId, new List<LocationAndTime>());
                            }
                        }
                    }                    
                }
                opponentSlotId++;
            }

            if (currentGameState.getOpponentAtPosition(1) != null)
            {
                currentGameState.SessionData.LapTimeDeltaLeader = shared.mLastLapTime - currentGameState.getOpponentAtPosition(1).ApproximateLastLapTime;
                currentGameState.SessionData.LapTimeDeltaLeaderClass = currentGameState.SessionData.LapTimeDeltaLeader;
                // TODO: get the leading opponent in the same car class...
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
            else if (shared.mPitMode == (int)ePitMode.PIT_MODE_DRIVING_OUT_OF_PITS)
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
            currentGameState.CarDamageData.OverallAeroDamage = mapToDamageLevel(shared.mAeroDamage);
            currentGameState.CarDamageData.OverallEngineDamage = mapToDamageLevel(shared.mEngineDamage);
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

            currentGameState.TyreData.FrontRight_CenterTemp = shared.mTyreTreadTemp[1] - 273;
            currentGameState.TyreData.FrontRight_LeftTemp = shared.mTyreTreadTemp[1] - 273;
            currentGameState.TyreData.FrontRight_RightTemp = shared.mTyreTreadTemp[1] - 273;
            currentGameState.TyreData.FrontRightTyreType = defaultTyreTypeForPlayersCar;
            currentGameState.TyreData.FrontRightPressure = -1; // not in the block
            currentGameState.TyreData.FrontRightPercentWear = Math.Min(100, shared.mTyreWear[1] * 100 / wornOutTyreWearLevel);

            currentGameState.TyreData.RearLeft_CenterTemp = shared.mTyreTreadTemp[2] - 273;
            currentGameState.TyreData.RearLeft_LeftTemp = shared.mTyreTreadTemp[2] - 273;
            currentGameState.TyreData.RearLeft_RightTemp = shared.mTyreTreadTemp[2] - 273;
            currentGameState.TyreData.RearLeftTyreType = defaultTyreTypeForPlayersCar;
            currentGameState.TyreData.RearLeftPressure = -1; // not in the block
            currentGameState.TyreData.RearLeftPercentWear = Math.Min(100, shared.mTyreWear[2] * 100 / wornOutTyreWearLevel);

            currentGameState.TyreData.RearRight_CenterTemp = shared.mTyreTreadTemp[3] - 273;
            currentGameState.TyreData.RearRight_LeftTemp = shared.mTyreTreadTemp[3] - 273;
            currentGameState.TyreData.RearRight_RightTemp = shared.mTyreTreadTemp[3] - 273;
            currentGameState.TyreData.RearRightTyreType = defaultTyreTypeForPlayersCar;
            currentGameState.TyreData.RearRightPressure = -1; // not in the block
            currentGameState.TyreData.RearRightPercentWear = Math.Min(100, shared.mTyreWear[3] * 100 / wornOutTyreWearLevel);

            currentGameState.TyreData.TyreConditionStatus = CornerData.getCornerData(tyreWearThresholds, currentGameState.TyreData.FrontLeftPercentWear, 
                currentGameState.TyreData.FrontRightPercentWear, currentGameState.TyreData.RearLeftPercentWear, currentGameState.TyreData.RearRightPercentWear);

            currentGameState.TyreData.TyreTempStatus = CornerData.getCornerData(CarData.tyreTempThresholds[defaultTyreTypeForPlayersCar],
                currentGameState.TyreData.FrontLeft_CenterTemp, currentGameState.TyreData.FrontRight_CenterTemp,
                currentGameState.TyreData.RearLeft_CenterTemp, currentGameState.TyreData.RearRight_CenterTemp);

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
            if (previousGameState != null && previousGameState.SessionData.CurrentLapIsValid && !currentGameState.SessionData.CurrentLapIsValid &&
                !(shared.mSessionState == (int)eSessionState.SESSION_RACE && shared.mRaceState == (int)eRaceState.RACESTATE_NOT_STARTED))
            {
                currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings + 1;
            }

            return currentGameState;
        }

        private DamageLevel mapToDamageLevel(float damage)
        {
            if (damage >= destroyedDamageThreshold)
            {
                return DamageLevel.DESTROYED;
            } 
            else if (damage >= severeDamageThreshold)
            {
                return DamageLevel.MAJOR;
            } 
            else if (damage >= minorDamageThreshold)
            {
                return DamageLevel.MINOR;
            } 
            else if (damage >= trivialDamageThreshold)
            {
                return DamageLevel.TRIVIAL;
            } 
            else
            {
                return DamageLevel.NONE;
            }
        }

        private void upateOpponentData(OpponentData opponentData, int racePosition, int completedLaps, int sector, Boolean isEnteringPits,
            float sessionRunningTime, float secondsSinceLastUpdate, int racePositionAtSector3, float[] currentWorldPosition, float[] previousWorldPosition, float previousSpeed)
        {
            opponentData.IsEnteringPits = isEnteringPits;
            float speed;
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
                opponentData.LapIsValid = false;
                opponentData.Speed = 0;
            }
            opponentData.Speed = speed;
            opponentData.Position = racePosition;
            opponentData.WorldPosition = currentWorldPosition;
            opponentData.IsNewLap = false;
            if (opponentData.CurrentSectorNumber != sector)
            {
                opponentData.CurrentSectorNumber = sector;
                if (opponentData.CurrentSectorNumber == 1)
                {
                    if (completedLaps == opponentData.CompletedLaps + 1)
                    {
                        opponentData.ApproximateLastLapTime = sessionRunningTime - opponentData.SessionTimeAtEndOfLastSector3;
                        opponentData.SessionTimeAtEndOfLastSector3 = sessionRunningTime;
                        opponentData.LapsCompletedAtEndOfLastSector3 = completedLaps;

                        opponentData.LapIsValid = true;
                        opponentData.IsNewLap = true;
                    }
                }
                else if (opponentData.CurrentSectorNumber == 2)
                {
                    opponentData.SessionTimeAtEndOfLastSector1 = sessionRunningTime;
                    opponentData.LapsCompletedAtEndOfLastSector1 = completedLaps;
                }
                else if (opponentData.CurrentSectorNumber == 3)
                {
                    opponentData.SessionTimeAtEndOfLastSector2 = sessionRunningTime;
                    opponentData.LapsCompletedAtEndOfLastSector2 = completedLaps;
                }
            }
            opponentData.CompletedLaps = completedLaps;
            opponentData.PositionAtSector3 = racePositionAtSector3;
        }

        private OpponentData createOpponentData(pCarsAPIParticipantStruct participantStruct)
        {
            OpponentData opponentData = new OpponentData();
            opponentData.DriverRawName = participantStruct.mName.Trim();            
            opponentData.Position = (int)participantStruct.mRacePosition;
            opponentData.CompletedLaps = (int)participantStruct.mLapsCompleted;
            opponentData.CurrentSectorNumber = (int)participantStruct.mCurrentSector;
            opponentData.WorldPosition = new float[] { participantStruct.mWorldPosition[0], participantStruct.mWorldPosition[2] };
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

        private Boolean IsOpponentOnPitLimiter(int opponentId)
        {
            float limiterSpeedVariance = 2;
            float limiterMinSpeed = 7;
            float limiterMaxSpeed = 30;
            int updatesPerSecond = CrewChief._timeInterval == TimeSpan.Zero ? 10 : 1000 / CrewChief._timeInterval.Milliseconds;
            int chunksToCheck = 3;
            int chunkSize = updatesPerSecond;
            Boolean onLimiter = false;
            if (OpponentWorldPositions.ContainsKey(opponentId) && OpponentWorldPositions[opponentId].Count > chunksToCheck * chunkSize)
            {
                float initialX = 0;
                float initialY = 0;
                onLimiter = true;
                for (int i = 1; i <= chunksToCheck; i++)
                {
                    List<LocationAndTime> locationsForChunk = OpponentWorldPositions[opponentId].GetRange(OpponentWorldPositions[opponentId].Count - (i * chunkSize), chunkSize);
                    float meanXSpeed = 0;
                    float meanYSpeed = 0;
                    int count = 0;
                    for (int j = 1; j < chunkSize; j++) 
                    {
                        count++;
                        float t = (float)TimeSpan.FromTicks(locationsForChunk[j].ticks - locationsForChunk[j - 1].ticks).TotalSeconds;
                        meanXSpeed += Math.Abs(locationsForChunk[j].x - locationsForChunk[j - 1].x) / t; 
                        meanYSpeed += Math.Abs(locationsForChunk[j].y - locationsForChunk[j - 1].y) / t;
                    }
                    meanXSpeed = meanXSpeed / (chunkSize - 1);
                    meanYSpeed = meanYSpeed / (chunkSize - 1);
                    if (i == 1)
                    {
                        initialX = meanXSpeed;
                        initialY = meanYSpeed;
                    }
                    if (meanXSpeed > limiterMaxSpeed || meanYSpeed > limiterMaxSpeed || (meanXSpeed < limiterMinSpeed && meanYSpeed < limiterMinSpeed) ||
                            Math.Abs(meanXSpeed - initialX) > limiterSpeedVariance || Math.Abs(meanYSpeed - initialY) > limiterSpeedVariance)
                    {
                        onLimiter = false;
                        break;
                    }
                }
            }
            return onLimiter;
        }
    }
}
