using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.GameState;
using CrewChiefV3.Events;

/**
 * Maps memory mapped file to a local game-agnostic representation.
 */
namespace CrewChiefV3.RaceRoom
{
    public class R3EGameStateMapper : GameStateMapper
    {
        private TimeSpan minimumSessionParticipationTime = TimeSpan.FromSeconds(6);

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

        private float destroyedTransmissionThreshold = 0.0f;
        private float destroyedEngineThreshold = 0.0f;
        private float destroyedAeroThreshold = 0.0f;

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
           
        public GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            CrewChiefV3.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper wrapper = (CrewChiefV3.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper)memoryMappedFileStruct;
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            RaceRoomData.RaceRoomShared shared = wrapper.data;

            if (shared.Player.GameSimulationTime <= 0 ||
                shared.ControlType == (int)RaceRoomConstant.Control.Remote || shared.ControlType ==(int)RaceRoomConstant.Control.Replay)
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
                currentGameState.carClass = CarData.getCarClass(null);  // TODO: change this null to the actual class identifier
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

                        // reset the engine temp monitor stuff
                        gotBaselineEngineData = false;
                        baselineEngineDataSamples = 0;
                        baselineEngineDataOilTemp = 0;
                        baselineEngineDataWaterTemp = 0;

                        // no car class info in the block, but if we've got DTM tyres on we can use that
                        if ((int)RaceRoomConstant.TireType.DTM_Option == shared.TireType || (int)RaceRoomConstant.TireType.Prime == shared.TireType)
                        {
                            Console.WriteLine("Using DTM (TC3) car class data");
                            currentGameState.carClass = CarData.getCarClassFromEnum(CarData.CarClassEnum.TC3);
                        }
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
                }
            }

            //------------------------ Session data -----------------------
            currentGameState.SessionData.Flag = FlagEnum.UNKNOWN;
            currentGameState.SessionData.SessionTimeRemaining = shared.SessionTimeRemaining;
            currentGameState.SessionData.CompletedLaps = shared.CompletedLaps;     
            currentGameState.SessionData.LapTimeBestPlayer = shared.LapTimeBest;

            float lapTimeBestLeader = shared.LapTimeBestLeader; 
            float lapTimeBestLeaderClass = shared.LapTimeBestLeaderClass;
            float lapTimeBestPlayer = shared.LapTimeBest;
            if (lapTimeBestLeader > 0 && 
                (currentGameState.SessionData.LapTimeSessionBest <= 0 || currentGameState.SessionData.LapTimeSessionBest > lapTimeBestLeader))
            {
                currentGameState.SessionData.LapTimeSessionBest = lapTimeBestLeader;
            }
            if (lapTimeBestLeaderClass > 0 &&
                (currentGameState.SessionData.LapTimeSessionBestPlayerClass <= 0 || currentGameState.SessionData.LapTimeSessionBestPlayerClass > lapTimeBestLeaderClass))
            {
                currentGameState.SessionData.LapTimeSessionBestPlayerClass = lapTimeBestLeaderClass;
                if (lapTimeBestLeaderClass < currentGameState.SessionData.LapTimeSessionBest)
                {
                    currentGameState.SessionData.LapTimeSessionBest = lapTimeBestLeaderClass;
                }
            }
            if (lapTimeBestPlayer > 0 &&
                (currentGameState.SessionData.LapTimeBestPlayer <= 0 || currentGameState.SessionData.LapTimeBestPlayer > lapTimeBestPlayer))
            {
                currentGameState.SessionData.LapTimeBestPlayer = lapTimeBestPlayer;
                if (lapTimeBestPlayer < currentGameState.SessionData.LapTimeSessionBestPlayerClass)
                {
                    currentGameState.SessionData.LapTimeSessionBestPlayerClass = lapTimeBestPlayer;
                }
                if (lapTimeBestPlayer < currentGameState.SessionData.LapTimeSessionBest)
                {
                    currentGameState.SessionData.LapTimeSessionBest = lapTimeBestPlayer;
                }
            }

            currentGameState.SessionData.LapTimeCurrent = shared.LapTimeCurrent;
            currentGameState.SessionData.CurrentLapIsValid = currentGameState.SessionData.LapTimeCurrent != -1;
            currentGameState.SessionData.LapTimeDeltaLeader = shared.LapTimeDeltaLeader;
            currentGameState.SessionData.LapTimeDeltaLeaderClass = shared.LapTimeDeltaLeaderClass;
            currentGameState.SessionData.LapTimeDeltaSelf = shared.LapTimeDeltaSelf;
            currentGameState.SessionData.LapTimePrevious = shared.LapTimePrevious;
            currentGameState.SessionData.PreviousLapWasValid = shared.LapTimePrevious != -1;
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


            if (shared.SessionType == (int)RaceRoomConstant.Session.Race && shared.SessionPhase == (int)RaceRoomConstant.SessionPhase.Checkered &&
                previousGameState != null && previousGameState.SessionData.SessionPhase == SessionPhase.Green)
            {
                Console.WriteLine("Leader has finished race, player has done "+ shared.CompletedLaps + " laps, session time = " + shared.Player.GameSimulationTime);
                currentGameState.SessionData.LeaderHasFinishedRace = true;
            }

            // sector information
            if (previousGameState == null || currentGameState.SessionData.IsNewLap)
            {
                currentGameState.SessionData.SectorNumber = 1;
                currentGameState.SessionData.IsNewSector = true;
            } else 
            {
                currentGameState.SessionData.SectorNumber = previousGameState.SessionData.SectorNumber;
                if (previousGameState.SessionData.SectorNumber == 1 &&
                    previousGameState.SessionData.Sector1TimeDeltaSelf != currentGameState.SessionData.Sector1TimeDeltaSelf)
                {
                    currentGameState.SessionData.SectorNumber = 2;
                }
                else if (previousGameState.SessionData.SectorNumber == 2 &&
                    previousGameState.SessionData.Sector2TimeDeltaSelf != currentGameState.SessionData.Sector2TimeDeltaSelf)
                {
                    currentGameState.SessionData.SectorNumber = 3;
                }
                currentGameState.SessionData.IsNewSector = currentGameState.SessionData.SectorNumber != previousGameState.SessionData.SectorNumber;
            }
       
            // racing same car in front / behind?
            if (previousGameState != null)
            {
                if (previousGameState.SessionData.NumCars == currentGameState.SessionData.NumCars)
                {
                    currentGameState.SessionData.IsRacingSameCarInFront = previousGameState.SessionData.Position == currentGameState.SessionData.Position;
                    currentGameState.SessionData.IsRacingSameCarBehind = previousGameState.SessionData.Position == currentGameState.SessionData.Position;
                }
                else
                {
                    // someone's dropped out of the race so see if it's the car immediately in front or behind
                    if (currentGameState.SessionData.Position == 1)
                    {
                        currentGameState.SessionData.IsRacingSameCarInFront = false;
                    }
                    if (currentGameState.SessionData.Position == currentGameState.SessionData.NumCars)
                    {
                        currentGameState.SessionData.IsRacingSameCarBehind = false;
                    }
                    if (currentGameState.SessionData.Position > 1)
                    {
                        // we're not first. We don't care what position we're in here (2 or more cars could have
                        // dropped out) - we just want to know if the car immediately in front or behind as dropped out.
                        // To test this we see if the gap has changed by more than the time interval - if the car in front 
                        // has stopped completely we'd catch him in a single time interval. If the gap change is larger than
                        // a single time interval he must have disappeared. He might still have dropped out, but the next 
                        // car is very close to us - can't detect this but it shouldn't be a major issue.
                        currentGameState.SessionData.IsRacingSameCarInFront = 
                            Math.Abs(currentGameState.SessionData.TimeDeltaFront - previousGameState.SessionData.TimeDeltaFront) * 1000 < CrewChief._timeInterval.Milliseconds;
                    }
                    if (currentGameState.SessionData.Position < currentGameState.SessionData.NumCars)
                    {
                        currentGameState.SessionData.IsRacingSameCarBehind = 
                            Math.Abs(currentGameState.SessionData.TimeDeltaBehind - previousGameState.SessionData.TimeDeltaBehind) * 1000 < CrewChief._timeInterval.Milliseconds;
                    }
                }
            }

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
            currentGameState.PitData.InPitlane = shared.Player.GameSimulationTime > 60 && currentGameState.SessionData.SessionPhase == SessionPhase.Green &&
                    currentGameState.ControlData.ControlType == ControlType.AI && !currentGameState.SessionData.LeaderHasFinishedRace;
            if (currentGameState.PitData.InPitlane && shared.CompletedLaps > currentGameState.PitData.LapCountWhenLastEnteredPits &&
                !currentGameState.PitData.OnInLap && !currentGameState.PitData.OnOutLap)
            {
                currentGameState.PitData.LapCountWhenLastEnteredPits = shared.CompletedLaps;
                currentGameState.PitData.OnInLap = true;
            }
            currentGameState.PitData.OnOutLap = shared.CompletedLaps > 0 && shared.CompletedLaps == currentGameState.PitData.LapCountWhenLastEnteredPits + 1;
            if (currentGameState.PitData.OnOutLap && currentGameState.PitData.OnInLap)
            {
                currentGameState.PitData.OnInLap = false;
            }
            currentGameState.PitData.PitWindow = mapToPitWindow(shared.PitWindowStatus);
            currentGameState.PitData.IsMakingMandatoryPitStop = (currentGameState.PitData.PitWindow == PitWindow.Open || currentGameState.PitData.PitWindow == PitWindow.StopInProgress) &&
               (currentGameState.PitData.OnInLap || currentGameState.PitData.OnOutLap);
            if (previousGameState == null || previousGameState.PitData.IsAtPitExit)
            {
                currentGameState.PitData.IsAtPitExit = false;
            }
            else if (currentGameState.PitData.OnInLap && currentGameState.ControlData.ControlType == ControlType.Player && previousGameState.ControlData.ControlType == ControlType.AI)
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
            currentGameState.TyreData.FrontLeftTyreType = tyreType;
            currentGameState.TyreData.FrontLeftPressure = shared.TirePressure.FrontLeft;
            currentGameState.TyreData.FrontLeftPercentWear = getTyreWearPercentage(shared.CarDamage.TireFrontLeft);

            currentGameState.TyreData.FrontRight_CenterTemp = shared.TireTemp.FrontRight_Center;
            currentGameState.TyreData.FrontRight_LeftTemp = shared.TireTemp.FrontRight_Left;
            currentGameState.TyreData.FrontRight_RightTemp = shared.TireTemp.FrontRight_Right;
            currentGameState.TyreData.FrontRightTyreType = tyreType;
            currentGameState.TyreData.FrontRightPressure = shared.TirePressure.FrontRight;
            currentGameState.TyreData.FrontRightPercentWear = getTyreWearPercentage(shared.CarDamage.TireFrontRight);

            currentGameState.TyreData.RearLeft_CenterTemp = shared.TireTemp.RearLeft_Center;
            currentGameState.TyreData.RearLeft_LeftTemp = shared.TireTemp.RearLeft_Left;
            currentGameState.TyreData.RearLeft_RightTemp = shared.TireTemp.RearLeft_Right;
            currentGameState.TyreData.RearLeftTyreType = tyreType;
            currentGameState.TyreData.RearLeftPressure = shared.TirePressure.RearLeft;
            currentGameState.TyreData.RearLeftPercentWear = getTyreWearPercentage(shared.CarDamage.TireRearLeft);

            currentGameState.TyreData.RearRight_CenterTemp = shared.TireTemp.RearRight_Center;
            currentGameState.TyreData.RearRight_LeftTemp = shared.TireTemp.RearRight_Left;
            currentGameState.TyreData.RearRight_RightTemp = shared.TireTemp.RearRight_Right;
            currentGameState.TyreData.RearRightTyreType = tyreType;
            currentGameState.TyreData.RearRightPressure = shared.TirePressure.RearRight;
            currentGameState.TyreData.RearRightPercentWear = getTyreWearPercentage(shared.CarDamage.TireRearRight);

            currentGameState.TyreData.TyreTempStatus = CornerData.getCornerData(CarData.tyreTempThresholds[tyreType],
                (currentGameState.TyreData.FrontLeft_CenterTemp + currentGameState.TyreData.FrontLeft_LeftTemp + currentGameState.TyreData.FrontLeft_RightTemp) / 3,
                (currentGameState.TyreData.FrontRight_CenterTemp + currentGameState.TyreData.FrontRight_LeftTemp + currentGameState.TyreData.FrontRight_RightTemp) / 3,
                (currentGameState.TyreData.RearLeft_CenterTemp + currentGameState.TyreData.RearLeft_LeftTemp + currentGameState.TyreData.RearLeft_RightTemp) / 3,
                (currentGameState.TyreData.RearRight_CenterTemp + currentGameState.TyreData.RearRight_LeftTemp + currentGameState.TyreData.RearRight_RightTemp) / 3);

            // TODO: the brake temp thresholds here are for 'iron race brakes'. We don't know what kind of car the player is driving...
            List<CornerData.EnumWithThresholds> brakeTempThresholdsToUse;
            if  (tyreType == TyreType.DTM_Option || tyreType == TyreType.DTM_Prime)
            {
                brakeTempThresholdsToUse = CarData.brakeTempThresholds[BrakeType.Carbon];
            } else {
                brakeTempThresholdsToUse = CarData.brakeTempThresholds[BrakeType.Iron_Race];
            }
            currentGameState.TyreData.BrakeTempStatus = CornerData.getCornerData(brakeTempThresholdsToUse, shared.BrakeTemperatures.FrontLeft, 
                shared.BrakeTemperatures.FrontRight, shared.BrakeTemperatures.RearLeft, shared.BrakeTemperatures.RearRight);
            
            currentGameState.TyreData.LeftFrontBrakeTemp = shared.BrakeTemperatures.FrontLeft;
            currentGameState.TyreData.RightFrontBrakeTemp = shared.BrakeTemperatures.FrontRight;
            currentGameState.TyreData.LeftRearBrakeTemp = shared.BrakeTemperatures.RearLeft;
            currentGameState.TyreData.RightRearBrakeTemp = shared.BrakeTemperatures.RearRight;

            return currentGameState;
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
    }
}
