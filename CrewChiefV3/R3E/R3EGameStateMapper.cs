using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.GameState;
using CrewChiefV3.Events;
using CrewChiefV3.RaceRoom.RaceRoomData;

/**
 * Maps memory mapped file to a local game-agnostic representation.
 */
namespace CrewChiefV3.RaceRoom
{
    public class R3EGameStateMapper : GameStateMapper
    {
        private TimeSpan minimumSessionParticipationTime = TimeSpan.FromSeconds(6);

        // for locking / spinning check - the tolerance values are built into these tyre diameter values
        private float minTyreCirumference = 0.4f * (float)Math.PI;  // 0.4m diameter
        private float maxTyreCirumference = 1.2f * (float)Math.PI;

        private List<CornerData.EnumWithThresholds> suspensionDamageThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> tyreWearThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> brakeDamageThresholds = new List<CornerData.EnumWithThresholds>();

        // tyres in R3E only go down to 0.9
        private float wornOutTyreWearLevel = 0.90f;

        private float scrubbedTyreWearPercent = 2f;
        private float minorTyreWearPercent = 10f;
        private float majorTyreWearPercent = 40f;
        private float wornOutTyreWearPercent = 80f;        

        private float trivialAeroDamageThreshold = 0.9999f;
        private float trivialEngineDamageThreshold = 0.99f;
        private float trivialTransmissionDamageThreshold = 0.99f;

        private float minorTransmissionDamageThreshold = 0.97f;
        private float minorEngineDamageThreshold = 0.98f;
        private float minorAeroDamageThreshold = 0.99f;

        private float severeTransmissionDamageThreshold = 0.4f;
        private float severeEngineDamageThreshold = 0.4f;
        private float severeAeroDamageThreshold = 0.9f;

        private float destroyedTransmissionThreshold = 0.1f;
        private float destroyedEngineThreshold = 0.1f;
        private float destroyedAeroThreshold = 0.0f;

        private List<CornerData.EnumWithThresholds> brakeTempThresholdsForPlayersCar = null;

        // Oil temps are typically 1 or 2 units (I'm assuming celcius) higher than water temps. Typical temps while racing tend to be
        // mid - high 50s, with some in-traffic running this creeps up to the mid 60s. To get it into the 
        // 70s you have to really try. Any higher requires you to sit by the side of the road bouncing off the
        // rev limiter. Doing this I've been able to get to 110 without blowing up (I got bored). With temps in the
        // 80s, by the end of a single lap at racing speed they're back into the 60s.
        //
        // I think the cool down effect of the radiator is the underlying issue here - it's far too strong. 
        // The oil temp does lag behind the water temp, which is correct, but I think it should lag 
        // more (i.e. it should take longer for the oil to cool) and the oil should heat up more relative to the water. 
        // 
        // I'd expect to be seeing water temperatures in the 80s for 'normal' running, with this getting well into the 
        // 90s or 100s in traffic. The oil temps should be 100+, maybe hitting 125 or more when the water's also hot.
        // 
        // To work around this I take a 'baseline' temp for oil and water - this is the average temperature between 3
        // and 5 minutes of the session. I then look at differences between this baseline and the current temperature, allowing
        // a configurable 'max above baseline' for each. Assuming the base line temps are sensible (say, 85 for water 105 for oil), 
        // then anthing over 95 for water and 120 for oil is 'bad' - the numbers in the config reflect this

        private Boolean gotBaselineEngineData = false;
        private int baselineEngineDataSamples = 0;
        // record the average temperature between minutes 2 and 4
        private double baselineEngineDataStartSeconds = 120;
        private double baselineEngineDataFinishSeconds = 240;
        private float baselineEngineDataOilTemp = 0;
        private float baselineEngineDataWaterTemp = 0;
        private float targetEngineWaterTemp = 88;
        private float targetEngineOilTemp = 105;

        private SpeechRecogniser speechRecogniser;

        private Dictionary<int, PendingRacePositionChange> PendingRacePositionChanges = new Dictionary<int, PendingRacePositionChange>();
        private TimeSpan PositionChangeLag = TimeSpan.FromMilliseconds(500);
        class PendingRacePositionChange
        {
            public int newPosition;
            public DateTime positionChangeTime;
            public PendingRacePositionChange(int newPosition, DateTime positionChangeTime)
            {
                this.newPosition = newPosition;
                this.positionChangeTime = positionChangeTime;
            }
        }

        public R3EGameStateMapper()
        {
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000, scrubbedTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, scrubbedTyreWearPercent, minorTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, minorTyreWearPercent, majorTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, majorTyreWearPercent, wornOutTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, wornOutTyreWearPercent, 10000));
        }

        public void versionCheck(Object memoryMappedFileStruct)
        {
            // no version number in r3e shared data so this is a no-op
        }

        public void setSpeechRecogniser(SpeechRecogniser speechRecogniser)
        {
            this.speechRecogniser = speechRecogniser;
        }

        public GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            CrewChiefV3.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper wrapper = (CrewChiefV3.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper)memoryMappedFileStruct;
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            RaceRoomData.RaceRoomShared shared = wrapper.data;

            if (shared.Player.GameSimulationTime <= 0 || shared.slot_id < 0 ||
                shared.ControlType == (int)RaceRoomConstant.Control.Remote || shared.ControlType == (int)RaceRoomConstant.Control.Replay)
            {
                return null;
            }

            Boolean isCarRunning = CheckIsCarRunning(shared);

            SessionPhase lastSessionPhase = SessionPhase.Unavailable;
            CarData.CarClass carClass = CarData.getDefaultCarClass();
            float lastSessionRunningTime = 0;
            if (previousGameState != null)
            {
                lastSessionPhase = previousGameState.SessionData.SessionPhase;
                lastSessionRunningTime = previousGameState.SessionData.SessionRunningTime;
                if (previousGameState.carClass == null)
                {
                    carClass = previousGameState.carClass;
                }
            }

            currentGameState.carClass = carClass;
            currentGameState.SessionData.SessionType = mapToSessionType(shared);
            currentGameState.SessionData.SessionRunningTime = (float)shared.Player.GameSimulationTime;
            currentGameState.ControlData.ControlType = mapToControlType(shared.ControlType); // TODO: the rest of the control data
            currentGameState.SessionData.NumCarsAtStartOfSession = shared.NumCars;
            int previousLapsCompleted = previousGameState == null ? 0 : previousGameState.SessionData.CompletedLaps;
            currentGameState.SessionData.SessionPhase = mapToSessionPhase(lastSessionPhase, currentGameState.SessionData.SessionType, lastSessionRunningTime,
                currentGameState.SessionData.SessionRunningTime, shared.SessionPhase, currentGameState.ControlData.ControlType,
                previousLapsCompleted, shared.CompletedLaps, isCarRunning);

            if ((lastSessionPhase != currentGameState.SessionData.SessionPhase && (lastSessionPhase == SessionPhase.Unavailable || lastSessionPhase == SessionPhase.Finished)) ||
                ((lastSessionPhase == SessionPhase.Checkered || lastSessionPhase == SessionPhase.Finished || lastSessionPhase == SessionPhase.Green) && 
                    currentGameState.SessionData.SessionPhase == SessionPhase.Countdown) ||
                lastSessionRunningTime > currentGameState.SessionData.SessionRunningTime)
            {
                currentGameState.SessionData.IsNewSession = true;
                Console.WriteLine("New session, trigger data:");
                Console.WriteLine("lastSessionPhase = " + lastSessionPhase);
                Console.WriteLine("lastSessionRunningTime = " + lastSessionRunningTime);
                Console.WriteLine("currentSessionPhase = " + currentGameState.SessionData.SessionPhase);
                Console.WriteLine("rawSessionPhase = " + shared.SessionPhase);
                Console.WriteLine("currentSessionRunningTime = " + currentGameState.SessionData.SessionRunningTime);

                currentGameState.SessionData.EventIndex = shared.EventIndex;
                currentGameState.SessionData.SessionIteration = shared.SessionIteration;
                currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                currentGameState.OpponentData.Clear();
                currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(null, shared.track_info.length, GameEnum.RACE_ROOM);
                for (int i = 0; i < shared.all_drivers_data.Length; i++)
                {
                    DriverData participantStruct = shared.all_drivers_data[i];
                    String driverName = getNameFromBytes(participantStruct.driver_info.nameByteArray);
                    if (participantStruct.driver_info.slot_id == shared.slot_id)
                    {
                        currentGameState.SessionData.IsNewSector = previousGameState == null || participantStruct.track_sector != previousGameState.SessionData.SectorNumber;
                        currentGameState.SessionData.SectorNumber = participantStruct.track_sector;
                        currentGameState.SessionData.DriverRawName = driverName;
                        currentGameState.PitData.InPitlane = participantStruct.in_pitlane == 1;
                        currentGameState.PositionAndMotionData.DistanceRoundTrack = participantStruct.lap_distance;
                        currentGameState.carClass = CarData.getCarClassForRaceRoomId(participantStruct.driver_info.class_id);
                        Console.WriteLine("Player is using car class " + currentGameState.carClass.carClassEnum + " (class ID " + participantStruct.driver_info.class_id + ")");

                        brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass, null);
                    }
                    else if (driverName != null && driverName.Length > 0)
                    {
                        if (!currentGameState.OpponentData.ContainsKey(participantStruct.driver_info.slot_id))
                        {
                            currentGameState.OpponentData.Add(participantStruct.driver_info.slot_id, createOpponentData(participantStruct, driverName, false));
                        }
                    }
                }
            }
            else
            {
                Boolean justGoneGreen = false;
                if (lastSessionPhase != currentGameState.SessionData.SessionPhase)
                {
                    Console.WriteLine("New session phase, was " + lastSessionPhase + " now " + currentGameState.SessionData.SessionPhase);
                    if (currentGameState.SessionData.SessionPhase == SessionPhase.Green)
                    {
                        justGoneGreen = true;
                        // just gone green, so get the session data
                        if (shared.NumberOfLaps > 0)
                        {
                            currentGameState.SessionData.SessionNumberOfLaps = shared.NumberOfLaps;
                            currentGameState.SessionData.SessionHasFixedTime = false;
                        }
                        if (shared.SessionTimeRemaining > 0)
                        {
                            currentGameState.SessionData.SessionRunTime = shared.SessionTimeRemaining;
                            currentGameState.SessionData.SessionHasFixedTime = true;
                        }
                        currentGameState.SessionData.SessionStartPosition = shared.Position;
                        currentGameState.SessionData.NumCarsAtStartOfSession = shared.NumCars;
                        currentGameState.SessionData.PitWindowStart = shared.PitWindowStart;
                        currentGameState.SessionData.PitWindowEnd = shared.PitWindowEnd;
                        currentGameState.SessionData.HasMandatoryPitStop = currentGameState.SessionData.PitWindowStart > 0 && currentGameState.SessionData.PitWindowEnd > 0; Console.WriteLine("Just gone green, session details...");
                        currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                        if (previousGameState != null)
                        {
                            currentGameState.OpponentData = previousGameState.OpponentData;
                            currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                            currentGameState.carClass = previousGameState.carClass;
                            currentGameState.SessionData.DriverRawName = previousGameState.SessionData.DriverRawName;
                        }

                        // reset the engine temp monitor stuff
                        gotBaselineEngineData = false;
                        baselineEngineDataSamples = 0;
                        baselineEngineDataOilTemp = 0;
                        baselineEngineDataWaterTemp = 0;

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
                        String trackName = currentGameState.SessionData.TrackDefinition == null ? "unknown" : currentGameState.SessionData.TrackDefinition.name;
                        Console.WriteLine("TrackName " + trackName);                        
                    }
                }
                if (!justGoneGreen && previousGameState != null)
                {
                    currentGameState.SessionData.SessionStartTime = previousGameState.SessionData.SessionStartTime;
                    currentGameState.SessionData.SessionRunTime = previousGameState.SessionData.SessionRunTime;
                    currentGameState.SessionData.SessionNumberOfLaps = previousGameState.SessionData.SessionNumberOfLaps;
                    currentGameState.SessionData.SessionStartPosition = previousGameState.SessionData.SessionStartPosition;
                    currentGameState.SessionData.NumCarsAtStartOfSession = previousGameState.SessionData.NumCarsAtStartOfSession;
                    currentGameState.SessionData.EventIndex = previousGameState.SessionData.EventIndex;
                    currentGameState.SessionData.SessionIteration = previousGameState.SessionData.SessionIteration;
                    currentGameState.SessionData.PitWindowStart = previousGameState.SessionData.PitWindowStart;
                    currentGameState.SessionData.PitWindowEnd = previousGameState.SessionData.PitWindowEnd;
                    currentGameState.SessionData.HasMandatoryPitStop = previousGameState.SessionData.HasMandatoryPitStop;
                    currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                    currentGameState.SessionData.playerLapTimes = previousGameState.SessionData.playerLapTimes;
                    currentGameState.SessionData.LapTimeSessionBestPlayerClass = previousGameState.SessionData.LapTimeSessionBestPlayerClass;
                    currentGameState.SessionData.LapTimeSessionBest = previousGameState.SessionData.LapTimeSessionBest;
                    currentGameState.carClass = previousGameState.carClass;
                    currentGameState.SessionData.DriverRawName = previousGameState.SessionData.DriverRawName;
                    currentGameState.SessionData.SessionTimesAtEndOfSectors = previousGameState.SessionData.SessionTimesAtEndOfSectors;
                    currentGameState.SessionData.LapTimePreviousEstimateForInvalidLap = previousGameState.SessionData.LapTimePreviousEstimateForInvalidLap;

                }
            }

            //------------------------ Session data -----------------------
            currentGameState.SessionData.Flag = FlagEnum.UNKNOWN;
            currentGameState.SessionData.SessionTimeRemaining = shared.SessionTimeRemaining;
            currentGameState.SessionData.CompletedLaps = shared.CompletedLaps;     
            currentGameState.SessionData.LapTimeBestPlayer = shared.LapTimeBest;
            
            currentGameState.SessionData.LapTimeCurrent = shared.LapTimeCurrent;
            currentGameState.SessionData.CurrentLapIsValid = currentGameState.SessionData.LapTimeCurrent != -1;
            currentGameState.SessionData.LapTimeDeltaLeader = shared.LapTimeDeltaLeader;
            currentGameState.SessionData.LapTimeDeltaLeaderClass = shared.LapTimeDeltaLeaderClass;
            currentGameState.SessionData.LapTimeDeltaSelf = shared.LapTimeDeltaSelf;
            currentGameState.SessionData.LapTimePrevious = shared.LapTimePrevious;
            currentGameState.SessionData.PreviousLapWasValid = shared.LapTimePrevious > 0;
            currentGameState.SessionData.Sector1TimeDeltaSelf = shared.SectorTimeDeltaSelf.Sector1;
            currentGameState.SessionData.Sector2TimeDeltaSelf = shared.SectorTimeDeltaSelf.Sector2;
            currentGameState.SessionData.Sector3TimeDeltaSelf = shared.SectorTimeDeltaSelf.Sector3;
            currentGameState.SessionData.NumCars = shared.NumCars;
            currentGameState.SessionData.Position = shared.Position;
            currentGameState.SessionData.TimeDeltaBehind = shared.TimeDeltaBehind;
            currentGameState.SessionData.TimeDeltaFront = shared.TimeDeltaFront;

            currentGameState.SessionData.IsNewLap = previousGameState != null && previousGameState.SessionData.IsNewLap == false &&
                (shared.CompletedLaps == previousGameState.SessionData.CompletedLaps + 1 ||
                ((lastSessionPhase == SessionPhase.Countdown || lastSessionPhase == SessionPhase.Formation || lastSessionPhase == SessionPhase.Garage)
                && currentGameState.SessionData.SessionPhase == SessionPhase.Green));
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.SessionData.playerLapTimes.Add(shared.LapTimePrevious);
            }
            if (previousGameState != null && !currentGameState.SessionData.IsNewSession)
            {
                currentGameState.OpponentData = previousGameState.OpponentData;
                currentGameState.SessionData.SectorNumber = previousGameState.SessionData.SectorNumber;
            }

            foreach (DriverData participantStruct in shared.all_drivers_data)
            {
                if (participantStruct.driver_info.slot_id == shared.slot_id)
                {
                    currentGameState.SessionData.IsNewSector = participantStruct.track_sector != 0 && currentGameState.SessionData.SectorNumber != participantStruct.track_sector;
                    if (currentGameState.SessionData.IsNewSector)
                    {
                        // TODO: finish this - bear in mind that the sector times the game provides are cumulative
                        float sectorTime = -1;
                        if (participantStruct.track_sector == 1)
                        {
                            sectorTime = participantStruct.sector_time_current_self.Sector3;
                            currentGameState.SessionData.LapTimePreviousEstimateForInvalidLap = currentGameState.SessionData.SessionRunningTime - currentGameState.SessionData.SessionTimesAtEndOfSectors[3];
                            currentGameState.SessionData.SessionTimesAtEndOfSectors[3] = currentGameState.SessionData.SessionRunningTime;
                        }
                        else if (participantStruct.track_sector == 2)
                        {
                            sectorTime = participantStruct.sector_time_current_self.Sector1;
                            currentGameState.SessionData.SessionTimesAtEndOfSectors[1] = currentGameState.SessionData.SessionRunningTime;
                        }
                        else if (participantStruct.track_sector == 3)
                        {
                            sectorTime = participantStruct.sector_time_current_self.Sector2;
                            currentGameState.SessionData.SessionTimesAtEndOfSectors[2] = currentGameState.SessionData.SessionRunningTime;
                        }

                    }
                    currentGameState.SessionData.SectorNumber = participantStruct.track_sector;
                    currentGameState.PitData.InPitlane = participantStruct.in_pitlane == 1;
                    currentGameState.PositionAndMotionData.DistanceRoundTrack = participantStruct.lap_distance;
                    if (participantStruct.track_sector == 3 && currentGameState.PitData.InPitlane)
                    {
                        currentGameState.PitData.OnInLap = true;
                        currentGameState.PitData.OnOutLap = false;
                    }
                    else if (participantStruct.track_sector == 1 && currentGameState.PitData.InPitlane)
                    {
                        currentGameState.PitData.OnInLap = false;
                        currentGameState.PitData.OnOutLap = true;
                    }
                    else if (currentGameState.SessionData.IsNewLap)
                    {
                        currentGameState.PitData.OnInLap = false;
                        currentGameState.PitData.OnOutLap = false;
                    }
                    break;
                }
            }

            foreach (DriverData participantStruct in shared.all_drivers_data)
            {
                if (participantStruct.driver_info.slot_id != -1)
                {
                    if (currentGameState.OpponentData.ContainsKey(participantStruct.driver_info.slot_id))
                    {
                        OpponentData currentOpponentData = currentGameState.OpponentData[participantStruct.driver_info.slot_id];
                        if (previousGameState != null)
                        {
                            int previousOpponentSectorNumber = 1;
                            int previousOpponentCompletedLaps = 0;
                            int previousOpponentPosition = 0;
                            Boolean previousOpponentIsEnteringPits = false;
                            Boolean previousOpponentIsExitingPits = false;

                            float[] previousOpponentWorldPosition = new float[] { 0, 0, 0 };
                            float previousOpponentSpeed = 0;
                            float sectorTime = -1;
                            if (participantStruct.track_sector == 1)
                            {
                                sectorTime = participantStruct.sector_time_current_self.Sector3;
                            }
                            else if (participantStruct.track_sector == 2)
                            {
                                sectorTime = participantStruct.sector_time_current_self.Sector1;
                            }
                            else if (participantStruct.track_sector == 3)
                            {
                                sectorTime = participantStruct.sector_time_current_self.Sector2;
                            }

                            OpponentData previousOpponentData = null;
                            Boolean newOpponentLap = false;
                            if (previousGameState.OpponentData.ContainsKey(participantStruct.driver_info.slot_id))
                            {
                                previousOpponentData = previousGameState.OpponentData[participantStruct.driver_info.slot_id];
                                previousOpponentSectorNumber = previousOpponentData.CurrentSectorNumber;
                                previousOpponentCompletedLaps = previousOpponentData.CompletedLaps;
                                previousOpponentPosition = previousOpponentData.Position;
                                previousOpponentIsEnteringPits = previousOpponentData.isEnteringPits();
                                previousOpponentIsExitingPits = previousOpponentData.isExitingPits();
                                previousOpponentWorldPosition = previousOpponentData.WorldPosition;
                                previousOpponentSpeed = previousOpponentData.Speed;
                                newOpponentLap = previousOpponentData.CurrentSectorNumber == 3 && participantStruct.track_sector == 1;
                            }

                            int currentOpponentRacePosition = getRacePosition(participantStruct.driver_info.slot_id, previousOpponentPosition, participantStruct.place, currentGameState.Now);
                            int currentOpponentLapsCompleted = participantStruct.completed_laps;
                            int currentOpponentSector = participantStruct.track_sector;
                            if (currentOpponentSector == 0)
                            {
                                currentOpponentSector = previousOpponentSectorNumber;
                            }
                            float currentOpponentLapDistance = participantStruct.lap_distance;
                            
                            Boolean finishedAllottedRaceLaps = currentGameState.SessionData.SessionNumberOfLaps > 0 && currentGameState.SessionData.SessionNumberOfLaps == currentOpponentLapsCompleted;
                            Boolean finishedAllottedRaceTime = false;
                            if (currentGameState.carClass.carClassEnum == CarData.CarClassEnum.DTM_2015 && currentGameState.SessionData.SessionType == SessionType.Race) 
                            {
                                if (currentGameState.SessionData.SessionRunTime > 0 && currentGameState.SessionData.SessionTimeRemaining <= 0 &&
                                    previousOpponentCompletedLaps < currentOpponentLapsCompleted)
                                {
                                    if (!currentOpponentData.HasStartedExtraLap) 
                                    {
                                        currentOpponentData.HasStartedExtraLap = true;
                                    }
                                    else
                                    {
                                        finishedAllottedRaceTime = true;
                                    }
                                }
                            }
                            else if (currentGameState.SessionData.SessionRunTime > 0 && currentGameState.SessionData.SessionTimeRemaining <= 0 && 
                                previousOpponentCompletedLaps < currentOpponentLapsCompleted)
                            {
                                finishedAllottedRaceTime = true;
                            }
                            
                            if (currentOpponentRacePosition == 1 && (finishedAllottedRaceTime || finishedAllottedRaceLaps))
                            {
                                currentGameState.SessionData.LeaderHasFinishedRace = true;
                            }
                            if (currentOpponentRacePosition == 1 && previousOpponentPosition > 1)
                            {
                                Console.WriteLine("lead change, new leader = " + getNameFromBytes(participantStruct.driver_info.nameByteArray) + ", race time = "+ shared.Player.GameSimulationTime);
                                currentGameState.SessionData.HasLeadChanged = true;
                            }
                            int opponentPositionAtSector3 = previousOpponentPosition;
                            Boolean isEnteringPits = participantStruct.in_pitlane == 1 && currentOpponentSector == 3;
                            Boolean isLeavingPits = participantStruct.in_pitlane == 1 && currentOpponentSector == 1;                            
                            
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
                            float secondsSinceLastUpdate = (float)new TimeSpan(currentGameState.Ticks - previousGameState.Ticks).TotalSeconds;
                            upateOpponentData(currentOpponentData, getNameFromBytes(participantStruct.driver_info.nameByteArray), currentOpponentRacePosition, currentOpponentLapsCompleted,
                                    currentOpponentSector, sectorTime, participantStruct.lap_time_current_self, 
                                    isEnteringPits || isLeavingPits, participantStruct.current_lap_valid == 1,
                                    currentGameState.SessionData.SessionRunningTime, secondsSinceLastUpdate,
                                    new float[] { participantStruct.position.X, participantStruct.position.Z }, previousOpponentWorldPosition,
                                    participantStruct.lap_distance, shared.Player.GameSimulationTime > 60);
                            
                            if (newOpponentLap)
                            {
                                if (currentOpponentData.CurrentBestLapTime > 0)
                                {
                                    if (currentGameState.SessionData.LapTimeSessionBest == -1 ||
                                        currentOpponentData.CurrentBestLapTime < currentGameState.SessionData.LapTimeSessionBest)
                                    {
                                        currentGameState.SessionData.LapTimeSessionBest = currentOpponentData.CurrentBestLapTime;
                                    }
                                    if (currentOpponentData.CarClass.carClassEnum == currentGameState.carClass.carClassEnum)
                                    {
                                        if (currentGameState.SessionData.LapTimeSessionBestPlayerClass == -1 ||
                                            currentOpponentData.CurrentBestLapTime < currentGameState.SessionData.LapTimeSessionBestPlayerClass)
                                        {
                                            currentGameState.SessionData.LapTimeSessionBestPlayerClass = currentOpponentData.CurrentBestLapTime;
                                        }
                                    }
                                }
                            }
                            // TODO: fix this properly - hack to work around issue with lagging position updates
                            if (currentGameState.SessionData.SessionType == SessionType.Race && 
                                (!isEnteringPits || isLeavingPits) && currentGameState.PositionAndMotionData.DistanceRoundTrack != 0 &&
                                currentOpponentData.Position + 1 < shared.Position && 
                                isBehindWithinDistance(shared.track_info.length, 10, 100, currentGameState.PositionAndMotionData.DistanceRoundTrack, participantStruct.lap_distance)) {
                                    currentGameState.SessionData.Flag = FlagEnum.BLUE;
                            }
                        }
                    }
                    else
                    {
                        String driverName = getNameFromBytes(participantStruct.driver_info.nameByteArray);
                        if (driverName != null && driverName.Length > 0)
                        {
                            Console.WriteLine("Creating opponent for name " + driverName);
                            currentGameState.OpponentData.Add(participantStruct.driver_info.slot_id, createOpponentData(participantStruct, driverName, 
                                shared.Player.GameSimulationTime > 60));
                        }
                    }
                }
            }

            if (currentGameState.SessionData.IsNewLap && currentGameState.SessionData.PreviousLapWasValid)
            {
                if ((currentGameState.SessionData.LapTimeSessionBest == -1 ||
                     currentGameState.SessionData.LapTimePrevious < currentGameState.SessionData.LapTimeSessionBest))
                {
                    currentGameState.SessionData.LapTimeSessionBest = currentGameState.SessionData.LapTimePrevious;
                }
                if ((currentGameState.SessionData.LapTimeSessionBestPlayerClass == -1 ||
                     currentGameState.SessionData.LapTimePrevious < currentGameState.SessionData.LapTimeSessionBestPlayerClass))
                {
                    currentGameState.SessionData.LapTimeSessionBestPlayerClass = currentGameState.SessionData.LapTimePrevious;
                }
            }

            // TODO: lap time previous for invalid laps, sector time stuff.

            if (shared.SessionType == (int)RaceRoomConstant.Session.Race && shared.SessionPhase == (int)RaceRoomConstant.SessionPhase.Checkered &&
                previousGameState != null && previousGameState.SessionData.SessionPhase == SessionPhase.Green)
            {
                Console.WriteLine("Leader has finished race, player has done "+ shared.CompletedLaps + " laps, session time = " + shared.Player.GameSimulationTime);
                currentGameState.SessionData.LeaderHasFinishedRace = true;
            }

            currentGameState.SessionData.IsRacingSameCarBehind = previousGameState != null && previousGameState.getOpponentKeyBehind() == currentGameState.getOpponentKeyBehind();
            currentGameState.SessionData.IsRacingSameCarInFront = previousGameState != null && previousGameState.getOpponentKeyInFront() == currentGameState.getOpponentKeyInFront();
                        
            //------------------------ Car damage data -----------------------
            currentGameState.CarDamageData.DamageEnabled = shared.CarDamage.Aerodynamics != -1 &&
                shared.CarDamage.Transmission != -1 && shared.CarDamage.Engine != -1;
            if (currentGameState.CarDamageData.DamageEnabled)
            {
                if (shared.CarDamage.Aerodynamics < destroyedAeroThreshold)
                {
                    currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.DESTROYED;
                }
                else if (shared.CarDamage.Aerodynamics < severeAeroDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.MAJOR;
                }
                else if (shared.CarDamage.Aerodynamics < minorAeroDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.MINOR;
                }
                else if (shared.CarDamage.Aerodynamics < trivialAeroDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.TRIVIAL;
                }
                else
                {
                    currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.NONE;
                }
                if (shared.CarDamage.Engine < destroyedEngineThreshold)
                {
                    currentGameState.CarDamageData.OverallEngineDamage = DamageLevel.DESTROYED;
                }
                else if (shared.CarDamage.Engine < severeEngineDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallEngineDamage = DamageLevel.MAJOR;
                }
                else if (shared.CarDamage.Engine < minorEngineDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallEngineDamage = DamageLevel.MINOR;
                }
                else if (shared.CarDamage.Engine < trivialEngineDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallEngineDamage = DamageLevel.TRIVIAL;
                }
                else
                {
                    currentGameState.CarDamageData.OverallEngineDamage = DamageLevel.NONE;
                }
                if (shared.CarDamage.Transmission < destroyedTransmissionThreshold)
                {
                    currentGameState.CarDamageData.OverallTransmissionDamage = DamageLevel.DESTROYED;
                }
                else if (shared.CarDamage.Transmission < severeTransmissionDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallTransmissionDamage = DamageLevel.MAJOR;
                }
                else if (shared.CarDamage.Transmission < minorTransmissionDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallTransmissionDamage = DamageLevel.MINOR;
                }
                else if (shared.CarDamage.Transmission < trivialTransmissionDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallTransmissionDamage = DamageLevel.TRIVIAL;
                }
                else
                {
                    currentGameState.CarDamageData.OverallTransmissionDamage = DamageLevel.NONE;
                }
            }
            
            //------------------------ Engine data -----------------------            
            currentGameState.EngineData.EngineOilPressure = shared.EngineOilPressure;
            currentGameState.EngineData.EngineRpm = Utilities.RpsToRpm(shared.EngineRps);
            currentGameState.EngineData.MaxEngineRpm = Utilities.RpsToRpm(shared.MaxEngineRps);
            currentGameState.EngineData.MinutesIntoSessionBeforeMonitoring = 5;

            if (!gotBaselineEngineData)
            {
                if (currentGameState.SessionData.SessionRunningTime > baselineEngineDataStartSeconds && 
                    currentGameState.SessionData.SessionRunningTime < baselineEngineDataFinishSeconds && isCarRunning)
                {
                    baselineEngineDataSamples++;
                    baselineEngineDataWaterTemp += shared.EngineWaterTemp;
                    baselineEngineDataOilTemp += shared.EngineOilTemp;
                }
                else if (currentGameState.SessionData.SessionRunningTime >= baselineEngineDataFinishSeconds && baselineEngineDataSamples > 0)
                {
                    gotBaselineEngineData = true;
                    baselineEngineDataOilTemp = baselineEngineDataOilTemp / baselineEngineDataSamples;
                    baselineEngineDataWaterTemp = baselineEngineDataWaterTemp / baselineEngineDataSamples;
                    Console.WriteLine("Got baseline engine temps, water = " + baselineEngineDataWaterTemp + ", oil = " + baselineEngineDataOilTemp);
                }
            }
            else
            {
                currentGameState.EngineData.EngineOilTemp = shared.EngineOilTemp * targetEngineOilTemp / baselineEngineDataOilTemp;
                currentGameState.EngineData.EngineWaterTemp = shared.EngineWaterTemp * targetEngineWaterTemp / baselineEngineDataWaterTemp;
            }


            //------------------------ Fuel data -----------------------
            currentGameState.FuelData.FuelUseActive = shared.FuelUseActive == 1;
            currentGameState.FuelData.FuelPressure = shared.FuelPressure;
            currentGameState.FuelData.FuelCapacity = shared.FuelCapacity;
            currentGameState.FuelData.FuelLeft = shared.FuelLeft;


            //------------------------ Penalties data -----------------------
            currentGameState.PenaltiesData.CutTrackWarnings = shared.CutTrackWarnings;
            currentGameState.PenaltiesData.HasDriveThrough = shared.Penalties.DriveThrough > 0;
            currentGameState.PenaltiesData.HasSlowDown = shared.Penalties.SlowDown > 0;
            currentGameState.PenaltiesData.HasPitStop = shared.Penalties.PitStop > 0;
            currentGameState.PenaltiesData.HasStopAndGo = shared.Penalties.StopAndGo > 0;
            currentGameState.PenaltiesData.HasTimeDeduction = shared.Penalties.TimeDeduction > 0; ;
            currentGameState.PenaltiesData.NumPenalties = shared.NumPenalties;


            //------------------------ Pit stop data -----------------------            
            currentGameState.PitData.PitWindow = mapToPitWindow(shared.PitWindowStatus);
            currentGameState.PitData.IsMakingMandatoryPitStop = (currentGameState.PitData.PitWindow == PitWindow.Open || currentGameState.PitData.PitWindow == PitWindow.StopInProgress) &&
               (currentGameState.PitData.OnInLap || currentGameState.PitData.OnOutLap);
            if (previousGameState == null || previousGameState.PitData.IsAtPitExit)
            {
                currentGameState.PitData.IsAtPitExit = false;
            }
            else if (currentGameState.PitData.OnOutLap && currentGameState.ControlData.ControlType == ControlType.Player && previousGameState.ControlData.ControlType == ControlType.AI)
            {
                currentGameState.PitData.IsAtPitExit = true;
            }

            //------------------------ Car position / motion data -----------------------
            currentGameState.PositionAndMotionData.CarSpeed = shared.CarSpeed;


            //------------------------ Transmission data -----------------------
            currentGameState.TransmissionData.Gear = shared.Gear;


            //------------------------ Tyre data -----------------------
            // no way to have unmatched tyre types in R3E
            currentGameState.TyreData.HasMatchedTyreTypes = true;
            currentGameState.TyreData.TireWearActive = shared.TireWearActive == 1;
            TyreType tyreType = mapToTyreType(shared.TireType);

            currentGameState.TyreData.FrontLeft_CenterTemp = shared.TireTemp.FrontLeft_Center;
            currentGameState.TyreData.FrontLeft_LeftTemp = shared.TireTemp.FrontLeft_Left;
            currentGameState.TyreData.FrontLeft_RightTemp = shared.TireTemp.FrontLeft_Right;
            float frontLeftTemp = (currentGameState.TyreData.FrontLeft_CenterTemp + currentGameState.TyreData.FrontLeft_LeftTemp + currentGameState.TyreData.FrontLeft_RightTemp) / 3;
            currentGameState.TyreData.FrontLeftTyreType = tyreType;
            currentGameState.TyreData.FrontLeftPressure = shared.TirePressure.FrontLeft;
            currentGameState.TyreData.FrontLeftPercentWear = getTyreWearPercentage(shared.CarDamage.TireFrontLeft);
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap = frontLeftTemp;
            }
            else if (previousGameState == null || frontLeftTemp > previousGameState.TyreData.PeakFrontLeftTemperatureForLap)
            {
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap = frontLeftTemp;
            }

            currentGameState.TyreData.FrontRight_CenterTemp = shared.TireTemp.FrontRight_Center;
            currentGameState.TyreData.FrontRight_LeftTemp = shared.TireTemp.FrontRight_Left;
            currentGameState.TyreData.FrontRight_RightTemp = shared.TireTemp.FrontRight_Right;
            float frontRightTemp = (currentGameState.TyreData.FrontRight_CenterTemp + currentGameState.TyreData.FrontRight_LeftTemp + currentGameState.TyreData.FrontRight_RightTemp) / 3;
            currentGameState.TyreData.FrontRightTyreType = tyreType;
            currentGameState.TyreData.FrontRightPressure = shared.TirePressure.FrontRight;
            currentGameState.TyreData.FrontRightPercentWear = getTyreWearPercentage(shared.CarDamage.TireFrontRight);
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakFrontRightTemperatureForLap = frontRightTemp;
            }
            else if (previousGameState == null || frontRightTemp > previousGameState.TyreData.PeakFrontRightTemperatureForLap)
            {
                currentGameState.TyreData.PeakFrontRightTemperatureForLap = frontRightTemp;
            }

            currentGameState.TyreData.RearLeft_CenterTemp = shared.TireTemp.RearLeft_Center;
            currentGameState.TyreData.RearLeft_LeftTemp = shared.TireTemp.RearLeft_Left;
            currentGameState.TyreData.RearLeft_RightTemp = shared.TireTemp.RearLeft_Right;
            float rearLeftTemp = (currentGameState.TyreData.RearLeft_CenterTemp + currentGameState.TyreData.RearLeft_LeftTemp + currentGameState.TyreData.RearLeft_RightTemp) / 3;
            currentGameState.TyreData.RearLeftTyreType = tyreType;
            currentGameState.TyreData.RearLeftPressure = shared.TirePressure.RearLeft;
            currentGameState.TyreData.RearLeftPercentWear = getTyreWearPercentage(shared.CarDamage.TireRearLeft);
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakRearLeftTemperatureForLap = rearLeftTemp;
            }
            else if (previousGameState == null || rearLeftTemp > previousGameState.TyreData.PeakRearLeftTemperatureForLap)
            {
                currentGameState.TyreData.PeakRearLeftTemperatureForLap = rearLeftTemp;
            }

            currentGameState.TyreData.RearRight_CenterTemp = shared.TireTemp.RearRight_Center;
            currentGameState.TyreData.RearRight_LeftTemp = shared.TireTemp.RearRight_Left;
            currentGameState.TyreData.RearRight_RightTemp = shared.TireTemp.RearRight_Right;
            float rearRightTemp = (currentGameState.TyreData.RearRight_CenterTemp + currentGameState.TyreData.RearRight_LeftTemp + currentGameState.TyreData.RearRight_RightTemp) / 3;
            currentGameState.TyreData.RearRightTyreType = tyreType;
            currentGameState.TyreData.RearRightPressure = shared.TirePressure.RearRight;
            currentGameState.TyreData.RearRightPercentWear = getTyreWearPercentage(shared.CarDamage.TireRearRight);
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakRearRightTemperatureForLap = rearRightTemp;
            }
            else if (previousGameState == null || rearRightTemp > previousGameState.TyreData.PeakRearRightTemperatureForLap)
            {
                currentGameState.TyreData.PeakRearRightTemperatureForLap = rearRightTemp;
            }

            currentGameState.TyreData.TyreConditionStatus = CornerData.getCornerData(tyreWearThresholds, currentGameState.TyreData.FrontLeftPercentWear,
                currentGameState.TyreData.FrontRightPercentWear, currentGameState.TyreData.RearLeftPercentWear, currentGameState.TyreData.RearRightPercentWear);

            currentGameState.TyreData.TyreTempStatus = CornerData.getCornerData(CarData.tyreTempThresholds[tyreType],
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap, currentGameState.TyreData.PeakFrontRightTemperatureForLap,
                currentGameState.TyreData.PeakRearLeftTemperatureForLap, currentGameState.TyreData.PeakRearRightTemperatureForLap);

            currentGameState.TyreData.BrakeTempStatus = CornerData.getCornerData(brakeTempThresholdsForPlayersCar, shared.BrakeTemperatures.FrontLeft, 
                shared.BrakeTemperatures.FrontRight, shared.BrakeTemperatures.RearLeft, shared.BrakeTemperatures.RearRight);
            
            currentGameState.TyreData.LeftFrontBrakeTemp = shared.BrakeTemperatures.FrontLeft;
            currentGameState.TyreData.RightFrontBrakeTemp = shared.BrakeTemperatures.FrontRight;
            currentGameState.TyreData.LeftRearBrakeTemp = shared.BrakeTemperatures.RearLeft;
            currentGameState.TyreData.RightRearBrakeTemp = shared.BrakeTemperatures.RearRight;

            // some simple locking / spinning checks
            if (shared.CarSpeed > 7)
            {
                float minRotatingSpeed = 2 * (float)Math.PI * shared.CarSpeed / maxTyreCirumference;
                // I think the tyreRPS is actually radians per second...
                currentGameState.TyreData.LeftFrontIsLocked = Math.Abs(shared.wheel_speed.front_left) < minRotatingSpeed;
                currentGameState.TyreData.RightFrontIsLocked = Math.Abs(shared.wheel_speed.front_right) < minRotatingSpeed;
                currentGameState.TyreData.LeftRearIsLocked = Math.Abs(shared.wheel_speed.rear_left) < minRotatingSpeed;
                currentGameState.TyreData.RightRearIsLocked = Math.Abs(shared.wheel_speed.rear_right) < minRotatingSpeed;

                float maxRotatingSpeed = 2 * (float)Math.PI * shared.CarSpeed / minTyreCirumference;
                currentGameState.TyreData.LeftFrontIsSpinning = Math.Abs(shared.wheel_speed.front_left) > maxRotatingSpeed;
                currentGameState.TyreData.RightFrontIsSpinning = Math.Abs(shared.wheel_speed.front_right) > maxRotatingSpeed;
                currentGameState.TyreData.LeftRearIsSpinning = Math.Abs(shared.wheel_speed.rear_left) > maxRotatingSpeed;
                currentGameState.TyreData.RightRearIsSpinning = Math.Abs(shared.wheel_speed.rear_right) > maxRotatingSpeed;
            }
            return currentGameState;
        }

        private int getRacePosition(int slot_id, int oldPosition, int newPosition, DateTime now)
        {
            if (oldPosition == newPosition) 
            {
                // clear any pending position change
                if (PendingRacePositionChanges.ContainsKey(slot_id))
                {
                    PendingRacePositionChanges.Remove(slot_id);
                }
                return oldPosition;
            }
            else if (PendingRacePositionChanges.ContainsKey(slot_id))
            {
                PendingRacePositionChange pendingRacePositionChange = PendingRacePositionChanges[slot_id];
                if (newPosition == pendingRacePositionChange.newPosition)
                {
                    // R3E is still reporting this driver is in the same race position, see if it's been long enough...
                    if (now > pendingRacePositionChange.positionChangeTime + PositionChangeLag)
                    {
                        int positionToReturn = newPosition;
                        PendingRacePositionChanges.Remove(slot_id);
                        return positionToReturn;
                    }
                    else
                    {
                        return oldPosition;
                    }
                }
                else
                {
                    // the new position is not consistent with the pending position change, bit of an edge case here
                    pendingRacePositionChange.newPosition = newPosition;
                    pendingRacePositionChange.positionChangeTime = now;
                    return oldPosition;
                }
            }
            else
            {
                PendingRacePositionChanges.Add(slot_id, new PendingRacePositionChange(newPosition, now));
                return oldPosition;
            }
        }

        private TyreType mapToTyreType(int r3eTyreType)
        {
            if ((int)RaceRoomConstant.TireType.DTM_Option == r3eTyreType)
            {
                return TyreType.DTM_Option;
            } 
            else if ((int)RaceRoomConstant.TireType.Prime == r3eTyreType)
            {
                return TyreType.DTM_Prime;
            }
            else
            {
                return TyreType.Unknown_Race;
            }
        }

        private PitWindow mapToPitWindow(int r3ePitWindow)
        {
            if ((int)RaceRoomConstant.PitWindow.Closed == r3ePitWindow)
            {
                return PitWindow.Closed;
            }
            if ((int)RaceRoomConstant.PitWindow.Completed == r3ePitWindow)
            {
                return PitWindow.Completed;
            }
            else if ((int)RaceRoomConstant.PitWindow.Disabled == r3ePitWindow)
            {
                return PitWindow.Disabled;
            }
            else if ((int)RaceRoomConstant.PitWindow.Open == r3ePitWindow)
            {
                return PitWindow.Open;
            }
            else if ((int)RaceRoomConstant.PitWindow.StopInProgress == r3ePitWindow)
            {
                return PitWindow.StopInProgress;
            }
            else
            {
                return PitWindow.Unavailable;
            }
        }

        /**
         * Gets the current session phase. If the transition is valid this is returned, otherwise the
         * previous phase is returned
         */
        private SessionPhase mapToSessionPhase(SessionPhase lastSessionPhase, SessionType currentSessionType, float lastSessionRunningTime, float thisSessionRunningTime, 
            int r3eSessionPhase, ControlType controlType, int previousLapsCompleted, int currentLapsCompleted, Boolean isCarRunning)
        {

            /* prac and qual sessions go chequered after the allotted time. They never go 'finished'. If we complete a lap
             * during this period we can detect the session end and trigger the finish message. Otherwise we just can't detect
             * this period end - hence the 'isCarRunning' hack...
            */
            if ((int)RaceRoomConstant.SessionPhase.Checkered == r3eSessionPhase)
            {
                if (lastSessionPhase == SessionPhase.Green)
                {
                    // only allow a transition to checkered if the last state was green
                    Console.WriteLine("checkered - completed " + currentLapsCompleted + " laps, session running time = " + thisSessionRunningTime);
                    return SessionPhase.Checkered;
                }
                else if (SessionPhase.Checkered == lastSessionPhase)
                {
                    if (previousLapsCompleted != currentLapsCompleted || controlType == ControlType.AI ||
                        ((currentSessionType == SessionType.Qualify || currentSessionType == SessionType.Practice) && !isCarRunning))
                    {
                        Console.WriteLine("finished - completed " + currentLapsCompleted + " laps (was " + previousLapsCompleted + "), session running time = " +
                            thisSessionRunningTime + " control type = " + controlType);
                        return SessionPhase.Finished;
                    }
                }
            }
            else if ((int)RaceRoomConstant.SessionPhase.Countdown == r3eSessionPhase)
            {
                // don't allow a transition to Countdown if the game time has increased
                if (lastSessionRunningTime < thisSessionRunningTime)
                {
                    return SessionPhase.Countdown;
                }
            }
            else if ((int)RaceRoomConstant.SessionPhase.Formation == r3eSessionPhase)
            {
                return SessionPhase.Formation;
            }
            else if ((int)RaceRoomConstant.SessionPhase.Garage == r3eSessionPhase)
            {
                return SessionPhase.Garage;
            }
            else if ((int)RaceRoomConstant.SessionPhase.Green == r3eSessionPhase)
            {
                if (controlType == ControlType.AI && thisSessionRunningTime < 30)
                {
                    return SessionPhase.Formation;
                }
                else
                {
                    return SessionPhase.Green;
                }
            }
            else if ((int)RaceRoomConstant.SessionPhase.Gridwalk == r3eSessionPhase)
            {
                return SessionPhase.Gridwalk;
            }
            else if ((int)RaceRoomConstant.SessionPhase.Terminated == r3eSessionPhase)
            {
                return SessionPhase.Finished;
            }
            return lastSessionPhase;
        }

        public SessionType mapToSessionType(Object memoryMappedFileStruct)
        {
            RaceRoomData.RaceRoomShared shared = (RaceRoomData.RaceRoomShared)memoryMappedFileStruct;
            int r3eSessionType = shared.SessionType;
            int numCars = shared.NumCars;
            if ((int)RaceRoomConstant.Session.Practice == r3eSessionType)
            {
                return SessionType.Practice;
            }
            else if ((int)RaceRoomConstant.Session.Qualify == r3eSessionType && (numCars == 1 || numCars == 2))
            {
                // hotlap sessions are not explicity declared in R3E - have to check if it's qual and there are 1 or 2 cars
                return SessionType.HotLap;
            }
            else if ((int)RaceRoomConstant.Session.Qualify == r3eSessionType)
            {
                return SessionType.Qualify;
            }
            else if ((int)RaceRoomConstant.Session.Race == r3eSessionType)
            {
                return SessionType.Race;
            }
            else
            {
                return SessionType.Unavailable;
            }
        }

        private ControlType mapToControlType(int r3eControlType)
        {
            if ((int)RaceRoomConstant.Control.AI == r3eControlType)
            {
                return ControlType.AI;
            }
            else if ((int)RaceRoomConstant.Control.Player == r3eControlType)
            {
                return ControlType.Player;
            }
            else if ((int)RaceRoomConstant.Control.Remote == r3eControlType)
            {
                return ControlType.Remote;
            }
            else if ((int)RaceRoomConstant.Control.Replay == r3eControlType)
            {
                return ControlType.Replay;
            }
            else
            {
                return ControlType.Unavailable;
            }
        }

        private float getTyreWearPercentage(float wearLevel)
        {
            if (wearLevel == -1)
            {
                return -1;
            }
            // tyres in R3E only go down to 0.9
            return (float) (((1 - Math.Max(wornOutTyreWearLevel, wearLevel)) / (1 - wornOutTyreWearLevel)) * 100);
        }

        private Boolean CheckIsCarRunning(RaceRoomData.RaceRoomShared shared)
        {
            return shared.EngineRps > 0.001 || shared.Gear > 0 || shared.CarSpeed > 0.001;
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

        private void upateOpponentData(OpponentData opponentData, String driverName, int racePosition, int completedLaps, int sector, float sectorTime, 
            float currentLapTime, Boolean isInPits, Boolean lapIsValid, float sessionRunningTime, float secondsSinceLastUpdate, float[] currentWorldPosition, 
            float[] previousWorldPosition, float distanceRoundTrack, Boolean updateDriverName)
        {
            if (updateDriverName && driverName != opponentData.DriverRawName)
            {
                Console.WriteLine("driver swap - " + opponentData.DriverRawName + " has now been replaced with " + driverName);
                if (CrewChief.enableDriverNames) 
                {
                    speechRecogniser.addNewOpponentName(driverName);
                }
                opponentData.DriverRawName = driverName;                
            }
            opponentData.DistanceRoundTrack = distanceRoundTrack;
            float speed;
            Boolean validSpeed = true;
            speed = (float)Math.Sqrt(Math.Pow(currentWorldPosition[0] - previousWorldPosition[0], 2) + Math.Pow(currentWorldPosition[1] - previousWorldPosition[1], 2)) / secondsSinceLastUpdate;
            if (speed > 500)
            {
                // faster than 500m/s (1000+mph) suggests the player has quit to the pit. Might need to reassess this as the data are quite noisy
                validSpeed = false;
                opponentData.Speed = 0;
            }
            opponentData.Speed = speed;
            opponentData.Position = racePosition;
            opponentData.WorldPosition = currentWorldPosition;
            opponentData.IsNewLap = false;            
            if (opponentData.CurrentSectorNumber != sector)
            {
                if (opponentData.CurrentSectorNumber == 3 && sector == 1)
                {
                    if (opponentData.OpponentLapData.Count > 0)
                    {
                        opponentData.CompleteLapWithProvidedLapTime(racePosition, sessionRunningTime, opponentData.CurrentLapTime,
                            lapIsValid && validSpeed, false, 20, 20);
                    }
                    opponentData.StartNewLap(completedLaps + 1, racePosition, isInPits, sessionRunningTime, false, 20, 20);
                    opponentData.IsNewLap = true;
                }
                else if (opponentData.CurrentSectorNumber == 1 && sector == 2 || opponentData.CurrentSectorNumber == 2 && sector == 3)
                {
                    opponentData.AddSectorData(racePosition, sectorTime, sessionRunningTime, lapIsValid && validSpeed, false, 20, 20);
                }
                opponentData.CurrentSectorNumber = sector;
            }
            opponentData.CompletedLaps = completedLaps;
            opponentData.CurrentLapTime = currentLapTime;
            if (sector == 3 && isInPits)
            {
                opponentData.setInLap();
            }
        }

        private OpponentData createOpponentData(DriverData participantStruct, String driverName, Boolean loadDriverName)
        {
            if (loadDriverName && CrewChief.enableDriverNames)
            {
                speechRecogniser.addNewOpponentName(driverName);
            } 
            OpponentData opponentData = new OpponentData();
            opponentData.DriverRawName = driverName;
            opponentData.Position = participantStruct.place;
            opponentData.CompletedLaps = participantStruct.completed_laps;
            opponentData.CurrentSectorNumber = participantStruct.track_sector;
            opponentData.WorldPosition = new float[] { participantStruct.position.X, participantStruct.position.Z };
            opponentData.DistanceRoundTrack = participantStruct.lap_distance;
            opponentData.CarClass = CarData.getCarClassForRaceRoomId(participantStruct.driver_info.class_id);
            Console.WriteLine("Driver " + driverName + " is using car class " +
                opponentData.CarClass.carClassEnum + " (class ID " + participantStruct.driver_info.class_id + ")");

            return opponentData;
        }

        public Boolean isBehindWithinDistance(float trackLength, float minDistance, float maxDistance, float playerTrackDistance, float opponentTrackDistance)
        {
            float difference = playerTrackDistance - opponentTrackDistance;
            if (difference > 0)
            {
                return difference < maxDistance && difference > minDistance;
            }
            else
            {
                difference = (playerTrackDistance + trackLength) - opponentTrackDistance;
                return difference < maxDistance && difference > minDistance;
            }
        }

        public static String getNameFromBytes(byte[] name)
        {
            return Encoding.UTF8.GetString(name).TrimEnd('\0').Trim();
        } 
    }
}
