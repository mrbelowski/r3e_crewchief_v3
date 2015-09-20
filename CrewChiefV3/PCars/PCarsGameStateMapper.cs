using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.GameState;

/**
 * Maps memory mapped file to a local game-agnostic representation.
 */
namespace CrewChiefV3.PCars
{
    class PCarsGameStateMapper : GameStateMapper
    {
        private static uint expectedVersion = 5;

        private List<uint> racingSurfaces = new List<uint>() { (uint)eTerrain.TERRAIN_BUMPY_DIRT_ROAD, 
            (uint)eTerrain.TERRAIN_BUMPY_ROAD1, (uint)eTerrain.TERRAIN_BUMPY_ROAD2, (uint)eTerrain.TERRAIN_BUMPY_ROAD3, 
            (uint)eTerrain.TERRAIN_COBBLES, (uint)eTerrain.TERRAIN_DRAINS, (uint)eTerrain.TERRAIN_EXIT_RUMBLE_STRIPS,
            (uint)eTerrain.TERRAIN_LOW_GRIP_ROAD, (uint)eTerrain.TERRAIN_MARBLES,(uint)eTerrain.TERRAIN_PAVEMENT,
            (uint)eTerrain.TERRAIN_ROAD, (uint)eTerrain.TERRAIN_RUMBLE_STRIPS, (uint)eTerrain.TERRAIN_SAND_ROAD};

        private float trivialDamageThreshold = 0.1f;
        private float minorDamageThreshold = 0.3f;
        private float severeDamageThreshold = 0.7f;
        private float destroyedDamageThreshold = 0.99f;

        // tyres in PCars are worn out when the wear level is > ?
        private float wornOutTyreWearLevel = 0.50f;

        private float scrubbedTyreWearPercent = 5f;
        private float minorTyreWearPercent = 30f;
        private float majorTyreWearPercent = 50f;
        private float wornOutTyreWearPercent = 90f;    
        
        private TimeSpan minimumSessionParticipationTime = TimeSpan.FromSeconds(6);

        public void versionCheck(Object memoryMappedFileStruct)
        {
            uint currentVersion = ((pCarsAPIStruct)memoryMappedFileStruct).mVersion;
            if (currentVersion != expectedVersion)
            {
                throw new GameDataReadException("Expected shared data version " + expectedVersion + " but got version " + currentVersion);
            }
        }

        public GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            GameStateData currentGameState = new GameStateData();
            pCarsAPIStruct shared = (pCarsAPIStruct)memoryMappedFileStruct;
            DateTime now = DateTime.Now;

            if (shared.mViewedParticipantIndex < 0 || shared.mNumParticipants < 1)
            {
                // Unusable data in the block
                return null;
            }
            pCarsAPIParticipantStruct viewedParticipant = shared.mParticipantData[shared.mViewedParticipantIndex];
            currentGameState.SessionData.CompletedLaps = (int)viewedParticipant.mLapsCompleted;
            currentGameState.SessionData.SectorNumber = (int)viewedParticipant.mCurrentSector;
            currentGameState.SessionData.Position = (int)viewedParticipant.mRacePosition;
            currentGameState.SessionData.IsNewSector = previousGameState == null || viewedParticipant.mCurrentSector != previousGameState.SessionData.SectorNumber;
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
            }

            // current session data
            currentGameState.SessionData.SessionType = mapToSessionType(shared);
            Boolean leaderHasFinished = previousGameState != null && previousGameState.SessionData.LeaderHasFinishedRace;
            currentGameState.SessionData.SessionPhase = mapToSessionPhase(currentGameState.SessionData.SessionType, 
                shared.mSessionState, shared.mRaceState, shared.mNumParticipants, leaderHasFinished);
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
            if (lastSessionType != currentGameState.SessionData.SessionType ||
                lastSessionHasFixedTime != currentGameState.SessionData.SessionHasFixedTime || lastSessionTrack != currentGameState.SessionData.TrackName ||
                lastSessionTrackLayout != currentGameState.SessionData.TrackLayout || lastSessionLapsCompleted > currentGameState.SessionData.CompletedLaps ||
                (numberOfLapsInSession > 0 && lastSessionNumberOfLaps > 0 && lastSessionNumberOfLaps != numberOfLapsInSession) ||
                (sessionTimeRemaining > 0 && currentGameState.SessionData.SessionRunTime > 0 &&
                    (sessionTimeRemaining > currentGameState.SessionData.SessionRunTime)))
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
                currentGameState.SessionData.IsNewSession = true;
                currentGameState.SessionData.TrackLength = shared.mTrackLength;
                currentGameState.SessionData.SessionNumberOfLaps = numberOfLapsInSession;
                if (currentGameState.SessionData.SessionHasFixedTime)
                {
                    currentGameState.SessionData.SessionRunTime = sessionTimeRemaining;
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
                            }
                        }
                        opponentSlotId++;
                    }
                }
            }
            else
            {
                if (lastSessionPhase != currentGameState.SessionData.SessionPhase)
                {
                    if (currentGameState.SessionData.SessionPhase == SessionPhase.Green)
                    {
                        // just gone green, so get the session data                        
                        if (currentGameState.SessionData.SessionHasFixedTime)
                        {
                            currentGameState.SessionData.SessionRunTime = sessionTimeRemaining;
                        }
                        currentGameState.SessionData.SessionNumberOfLaps = numberOfLapsInSession;
                        currentGameState.SessionData.SessionStartPosition = (int)shared.mParticipantData[shared.mViewedParticipantIndex].mRacePosition;
                        currentGameState.SessionData.NumCarsAtStartOfSession = shared.mNumParticipants;
                        currentGameState.SessionData.TrackLength = shared.mTrackLength;
                        if (previousGameState != null)
                        {
                            currentGameState.OpponentData = previousGameState.OpponentData;
                            currentGameState.SessionData.SessionStartTime = previousGameState.SessionData.SessionStartTime;
                            currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                        }

                        Console.WriteLine("Just gone green, session details...");
                        Console.WriteLine("SessionType " + currentGameState.SessionData.SessionType);
                        Console.WriteLine("SessionPhase " + currentGameState.SessionData.SessionPhase);
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
                else if (previousGameState != null)
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
                    currentGameState.SessionData.LeaderHasFinishedRace = previousGameState.SessionData.LeaderHasFinishedRace;
                    currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                }                
            }            
            
            //------------------- Variable session data ---------------------------
            if (currentGameState.SessionData.SessionHasFixedTime)
            {
                currentGameState.SessionData.SessionRunningTime = currentGameState.SessionData.SessionRunTime - shared.mEventTimeRemaining;
            }
            else
            {
                currentGameState.SessionData.SessionRunningTime = (float)(now - currentGameState.SessionData.SessionStartTime).TotalSeconds;
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
            currentGameState.SessionData.LapTimeDeltaLeader = shared.mLastLapTime - shared.mSessionFastestLapTime; // is this appropriate?
            currentGameState.SessionData.TimeDeltaBehind = shared.mSplitTimeBehind;
            currentGameState.SessionData.TimeDeltaFront = shared.mSplitTimeAhead;
            // is this right??
            if (!currentGameState.SessionData.LeaderHasFinishedRace)
            {
                if (shared.mHighestFlagColour == (int)eFlagColors.FLAG_COLOUR_CHEQUERED)
                {
                    currentGameState.SessionData.LeaderHasFinishedRace = true;
                }
            }

            opponentSlotId = 0;
            foreach (pCarsAPIParticipantStruct participantStruct in shared.mParticipantData)
            {
                if (shared.mViewedParticipantIndex != opponentSlotId)
                {
                    if (currentGameState.OpponentData.ContainsKey(opponentSlotId))
                    {
                        if (currentGameState.OpponentData[opponentSlotId].IsActive && participantStruct.mIsActive)
                        {
                            upateOpponentData(currentGameState.OpponentData[opponentSlotId], participantStruct.mRacePosition, participantStruct.mLapsCompleted,
                                    participantStruct.mCurrentSector, false, currentGameState.SessionData.SessionRunningTime, participantStruct.mCurrentLapDistance, 
                                    shared.mTrackLength, now);
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
                            currentGameState.OpponentData.Add(opponentSlotId, createOpponentData(participantStruct));
                        }
                    }                    
                }
                opponentSlotId++;
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
                
            currentGameState.CarDamageData.DamageEnabled = true;    // no way to tell if it's disabled from the shared memory
            currentGameState.CarDamageData.OverallAeroDamage = mapToDamageLevel(shared.mAeroDamage);
            currentGameState.CarDamageData.OverallEngineDamage = mapToDamageLevel(shared.mEngineDamage);
            currentGameState.CarDamageData.OverallTransmissionDamage = DamageLevel.UNKNOWN;
            currentGameState.CarDamageData.LeftFrontSuspensionDamage = mapToDamageLevel(shared.mSuspensionDamage[0]);
            currentGameState.CarDamageData.RightFrontSuspensionDamage = mapToDamageLevel(shared.mSuspensionDamage[1]);
            currentGameState.CarDamageData.LeftRearSuspensionDamage = mapToDamageLevel(shared.mSuspensionDamage[2]);
            currentGameState.CarDamageData.RightRearSuspensionDamage = mapToDamageLevel(shared.mSuspensionDamage[3]);

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
            TyreType tyreType = TyreType.Unknown;

            currentGameState.TyreData.FrontLeft_CenterTemp = shared.mTyreTreadTemp[0] - 273;
            currentGameState.TyreData.FrontLeft_LeftTemp = shared.mTyreTreadTemp[0] - 273;
            currentGameState.TyreData.FrontLeft_RightTemp = shared.mTyreTreadTemp[0] - 273;
            currentGameState.TyreData.FrontLeftTyreType = tyreType;
            currentGameState.TyreData.FrontLeftPressure = -1; // not in the block
            currentGameState.TyreData.FrontLeftPercentWear = Math.Min(100, shared.mTyreWear[0] * 100 / wornOutTyreWearLevel);
            currentGameState.TyreData.FrontLeftCondition = getTyreCondition(currentGameState.TyreData.FrontLeftPercentWear);

            currentGameState.TyreData.FrontRight_CenterTemp = shared.mTyreTreadTemp[1] - 273;
            currentGameState.TyreData.FrontRight_LeftTemp = shared.mTyreTreadTemp[1] - 273;
            currentGameState.TyreData.FrontRight_RightTemp = shared.mTyreTreadTemp[1] - 273;
            currentGameState.TyreData.FrontRightTyreType = tyreType;
            currentGameState.TyreData.FrontRightPressure = -1; // not in the block
            currentGameState.TyreData.FrontRightPercentWear = Math.Min(100, shared.mTyreWear[1] * 100 / wornOutTyreWearLevel);
            currentGameState.TyreData.FrontRightCondition = getTyreCondition(currentGameState.TyreData.FrontRightPercentWear);

            currentGameState.TyreData.RearLeft_CenterTemp = shared.mTyreTreadTemp[2] - 273;
            currentGameState.TyreData.RearLeft_LeftTemp = shared.mTyreTreadTemp[2] - 273;
            currentGameState.TyreData.RearLeft_RightTemp = shared.mTyreTreadTemp[2] - 273;
            currentGameState.TyreData.RearLeftTyreType = tyreType;
            currentGameState.TyreData.RearLeftPressure = -1; // not in the block
            currentGameState.TyreData.RearLeftPercentWear = Math.Min(100, shared.mTyreWear[2] * 100 / wornOutTyreWearLevel);
            currentGameState.TyreData.RearLeftCondition = getTyreCondition(currentGameState.TyreData.RearLeftPercentWear);

            currentGameState.TyreData.RearRight_CenterTemp = shared.mTyreTreadTemp[3] - 273;
            currentGameState.TyreData.RearRight_LeftTemp = shared.mTyreTreadTemp[3] - 273;
            currentGameState.TyreData.RearRight_RightTemp = shared.mTyreTreadTemp[3] - 273;
            currentGameState.TyreData.RearRightTyreType = tyreType;
            currentGameState.TyreData.RearRightPressure = -1; // not in the block
            currentGameState.TyreData.RearRightPercentWear = Math.Min(100, shared.mTyreWear[3] * 100 / wornOutTyreWearLevel);
            currentGameState.TyreData.RearRightCondition = getTyreCondition(currentGameState.TyreData.RearRightPercentWear);

            // improvised cut track warnings...
            currentGameState.PenaltiesData.isOffRacingSurface = !racingSurfaces.Contains(shared.mTerrain[0]) &&
                !racingSurfaces.Contains(shared.mTerrain[1]) && !racingSurfaces.Contains(shared.mTerrain[2]) &&
                !racingSurfaces.Contains(shared.mTerrain[3]);
            if (previousGameState != null && previousGameState.PenaltiesData.isOffRacingSurface && currentGameState.PenaltiesData.isOffRacingSurface) 
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

        private void upateOpponentData(OpponentData opponentData, uint position, uint completedLaps,
            uint sector, Boolean isPitting, float sessionRunningTime, float distanceRoundTrack, float trackLength, DateTime now)
        {
            opponentData.IsPitting = isPitting;
            if (distanceRoundTrack > opponentData.DistanceRoundTrack)
            {
                opponentData.approximateSpeed = (distanceRoundTrack - opponentData.DistanceRoundTrack) / ((float)(now - opponentData.lastUpdateTime).TotalMilliseconds / 1000);
                if (opponentData.approximateSpeed > 150)
                {
                    // going > 150m/s (about 320mph) suggests this guy's quit to the pits
                    opponentData.LapIsValid = false;
                }
            }
            else if (distanceRoundTrack == opponentData.DistanceRoundTrack)
            {
                opponentData.approximateSpeed = 0;
            }
            else if (distanceRoundTrack < opponentData.DistanceRoundTrack)
            {
                // if we've gone 'backwards' more than 100 metres, check this is because we've crossed the start line
                // We don't want to invalidate a lap because they went backwards
                if (opponentData.DistanceRoundTrack < trackLength - 100 &&
                    opponentData.DistanceRoundTrack - distanceRoundTrack > 100)
                {
                    opponentData.LapIsValid = false;
                }
            }

            opponentData.Position = (int)position;
            opponentData.DistanceRoundTrack = distanceRoundTrack;
            opponentData.lastUpdateTime = now;
            if (opponentData.CurrentSectorNumber != sector)
            {
                opponentData.CurrentSectorNumber = (int)sector;
                if (opponentData.CurrentSectorNumber == 1)
                {
                    if (completedLaps == opponentData.CompletedLaps + 1)
                    {
                        Console.WriteLine("Opponent " + opponentData.DriverRawName + " has just completed a lap - valid = " + opponentData.LapIsValid +
                            " SessionTimeAtEndOfLastSector3 = " + opponentData.SessionTimeAtEndOfLastSector3);
                        if (opponentData.SessionTimeAtEndOfLastSector3 > 0 && opponentData.LapIsValid)
                        {
                            float errorCorrection = 0;
                            if (opponentData.DistanceRoundTrack > 0 && opponentData.DistanceRoundTrack < 200 && opponentData.approximateSpeed > 0)
                            {
                                errorCorrection = opponentData.DistanceRoundTrack / opponentData.approximateSpeed;
                            }
                            opponentData.approximateLastLapTime = sessionRunningTime - opponentData.SessionTimeAtEndOfLastSector3 - errorCorrection;
                        }
                        opponentData.SessionTimeAtEndOfLastSector3 = sessionRunningTime;
                        opponentData.LapsCompletedAtEndOfLastSector3 = (int)completedLaps;
                    }
                    opponentData.LapIsValid = true;
                }
                else if (opponentData.CurrentSectorNumber == 2)
                {
                    opponentData.SessionTimeAtEndOfLastSector1 = sessionRunningTime;
                    opponentData.LapsCompletedAtEndOfLastSector1 = (int)completedLaps;
                }
                else if (opponentData.CurrentSectorNumber == 3)
                {
                    opponentData.SessionTimeAtEndOfLastSector2 = sessionRunningTime;
                    opponentData.LapsCompletedAtEndOfLastSector2 = (int)completedLaps;
                }
            }
            opponentData.CompletedLaps = (int)completedLaps;
        }

        private OpponentData createOpponentData(pCarsAPIParticipantStruct participantStruct)
        {
            OpponentData opponentData = new OpponentData();
            opponentData.DistanceRoundTrack = participantStruct.mCurrentLapDistance;
            opponentData.DriverRawName = participantStruct.mName.Trim();            
            opponentData.Position = (int)participantStruct.mRacePosition;
            opponentData.CompletedLaps = (int)participantStruct.mLapsCompleted;
            opponentData.CurrentSectorNumber = (int)participantStruct.mCurrentSector;
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
        private SessionPhase mapToSessionPhase(SessionType sessionType, uint sessionState, uint raceState, int numParticipants,
            Boolean leaderHasFinishedRace)
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
                if (raceState == (uint)eRaceState.RACESTATE_NOT_STARTED)
                {
                    return SessionPhase.Garage;
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
    }
}
