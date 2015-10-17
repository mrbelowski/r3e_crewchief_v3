using CrewChiefV3.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/**
 * Holds all the data collected from the memory mapped file for the current tick
 */
namespace CrewChiefV3.GameState
{
    public enum SessionType
    {
        Unavailable, Practice, Qualify, Race, HotLap
    }
    public enum SessionPhase
    {
        Unavailable, Garage, Gridwalk, Formation, Countdown, Green, Checkered, Finished
    }
    public enum ControlType
    {
        Unavailable, Player, AI, Remote, Replay
    }
    public enum PitWindow
    {
        Unavailable,  Disabled, Closed, Open, StopInProgress, Completed
    }
    public enum TyreType
    {
        // separate enum for compound & weather, and prime / option?
        Hard, Medium, Soft, Wet, Intermediate, DTM_Prime, DTM_Option, Road, Bias_Ply, Unknown_Race
    }

    public enum BrakeType
    {
        // pretty coarse grained here.
        Iron_Road, Iron_Race, Ceramic, Carbon
    }

    public enum TyreCondition
    {
        UNKNOWN, NEW, SCRUBBED, MINOR_WEAR, MAJOR_WEAR, WORN_OUT
    }
    public enum TyreTemp
    {
        UNKNOWN, COLD, WARM, HOT, COOKING
    }
    public enum BrakeTemp
    {
        UNKNOWN, COLD, WARM, HOT, COOKING
    }
    public enum DamageLevel
    {
        UNKNOWN = 0, NONE = 1, TRIVIAL = 2, MINOR = 3, MAJOR = 4, DESTROYED = 5
    }
    public enum FlagEnum
    {
        // note that chequered isn't used at the moment
        GREEN, YELLOW, DOUBLE_YELLOW, BLUE, WHITE, BLACK, CHEQUERED, UNKNOWN
    }


    public class TransmissionData
    {
        // -2 = no data
        // -1 = reverse,
        //  0 = neutral
        //  1 = first gear
        // (... up to 7th)
        public int Gear = -2;
    }

    public class EngineData
    {
        // Engine speed
        public Single EngineRpm = 0;

        // Maximum engine speed
        public Single MaxEngineRpm = 0;

        // Unit: Celcius
        public Single EngineWaterTemp = 0;

        // Unit: Celcius
        public Single EngineOilTemp = 0;

        // Unit: ?
        public Single EngineOilPressure = 0;

        public int MinutesIntoSessionBeforeMonitoring = 0;
        
    }

    public class FuelData
    {
        // Unit: ?
        public Single FuelPressure = 0;

        // Current amount of fuel in the tank(s)
        // Unit: Liters (l)
        public Single FuelLeft = 0;

        // Maximum capacity of fuel tank(s)
        // Unit: Liters (l)
        public Single FuelCapacity = 0;

        public Boolean FuelUseActive = false;
    }

    public class CarDamageData
    {
        public Boolean DamageEnabled = false;

        public DamageLevel OverallTransmissionDamage = DamageLevel.UNKNOWN;

        public DamageLevel OverallEngineDamage = DamageLevel.UNKNOWN;

        public DamageLevel OverallAeroDamage = DamageLevel.UNKNOWN;

        public CornerData SuspensionDamageStatus = new CornerData();

        public CornerData BrakeDamageStatus = new CornerData();
    }

    public class SessionData    
    {
        public TrackDefinition TrackDefinition = null;

        public Boolean IsDisqualified = false;

        public FlagEnum Flag = FlagEnum.GREEN;

        public Boolean IsNewSession = false;

        public Boolean SessionHasFixedTime = false;

        public SessionType SessionType = SessionType.Unavailable;

        public DateTime SessionStartTime;

        // in minutes, 0 if this session is a fixed number of laps rather than a fixed time.
        public float SessionRunTime = 0;

        public int SessionNumberOfLaps = 0;

        public int SessionStartPosition = 0;

        public int NumCarsAtStartOfSession = 0;
        
        // race number in ongoing championship (zero indexed)
        public int EventIndex = 0;

        // zero indexed - you multi iteration sessions like DTM qual
        public int SessionIteration = 0;

        // TODO: will this always be an Integer?
        public int PitWindowStart = 0;

        // The minute/lap into which you can/should pit
        // Unit: Minutes in time based sessions, otherwise lap
        public int PitWindowEnd = 0;

        public Boolean HasMandatoryPitStop = false;

        // as soon as the player leaves the racing surface this is set to false
        public Boolean CurrentLapIsValid = true;

        public Boolean PreviousLapWasValid = true;

        public SessionPhase SessionPhase = SessionPhase.Unavailable;

        public Boolean IsNewLap = false;

        // How many laps the player has completed. If this value is 6, the player is on his 7th lap.
        public int CompletedLaps = 0;

        // Unit: Seconds (-1.0 = none)
        public Single LapTimeBestPlayer = -1;

        // Unit: Seconds (-1.0 = none)
        public Single LapTimePrevious = -1;

        public Single LapTimePreviousEstimateForInvalidLap = -1;

        // Unit: Seconds (-1.0 = none)
        public Single LapTimeCurrent = -1;

        public Boolean LeaderHasFinishedRace = false;

        // Current position (1 = first place)
        public int Position = 0;

        // Number of cars (including the player) in the race
        public int NumCars = 0;

        public Single SessionRunningTime = 0;

        // ...
        public Single SessionTimeRemaining = 0;

        // ...
        public Single LapTimeSessionBest = 0;

        public Single LapTimeSessionBestPlayerClass = 0;

        // ...
        public Single LapTimeDeltaSelf = 0;

        // ...
        public Single LapTimeDeltaLeader = 0;

        // ...
        public Single LapTimeDeltaLeaderClass = 0;

        // ...
        public Single TimeDeltaFront = 0;

        // ...
        public Single TimeDeltaBehind = 0;

        // 0 means we don't know what sector we're in. This is 1-indexed
        public int SectorNumber = 0;

        public Boolean IsNewSector = false;

        public float Sector1TimeDeltaSelf = 0;

        public float Sector2TimeDeltaSelf = 0;

        public float Sector3TimeDeltaSelf = 0;

        // these are used for quick n dirty checks to see if we're racing the same opponent in front / behind,
        // without iterating over the Opponents list. Or for cases (like R3E) where we don't have an opponents list
        public Boolean IsRacingSameCarInFront = true;

        public Boolean IsRacingSameCarBehind = true;

        public Boolean HasLeadChanged = false;

        public Dictionary<int, float> SessionTimesAtEndOfSectors = new Dictionary<int, float>();

        public String DriverRawName;

        public SessionData()
        {
            SessionTimesAtEndOfSectors.Add(1, -1); 
            SessionTimesAtEndOfSectors.Add(2, -1); 
            SessionTimesAtEndOfSectors.Add(3, -1);
        }
    }

    public class PositionAndMotionData
    {
        // Unit: Meter per second (m/s).
        public Single CarSpeed = 0;

        // distance (m) from the start line (around the track)
        public Single DistanceRoundTrack = 0;

        // other stuff like X/Y/Z co-ordinates, acceleration, orientation, ...
    }

    public class OpponentData
    {
        // set this to false if this opponent drops out of the race (i.e. leaves a server)
        public Boolean IsActive = true;

        public Boolean LapIsValid = false;

        // the name read directly from the game data - might be a 'handle' with all kinds of random crap in it
        public String DriverRawName = null;

        public int Position = 0;

        // used to work out where this opponent was before he started his pit entry
        public int PositionAtSector3 = 0;
        
        public int CompletedLaps = 0;

        public int CurrentSectorNumber = 0;

        public Boolean IsEnteringPits = false;

        public Boolean IsLeavingPits = false;

        public Dictionary<int, float> SessionTimesAtEndOfSectors = new Dictionary<int, float>();
               
        public float ApproximateLastLapTime = 0;

        public float Speed = 0;

        public float[] WorldPosition;

        public Boolean IsNewLap = false;

        public float DistanceRoundTrack = 0;

        public OpponentData()
        {
            SessionTimesAtEndOfSectors.Add(1, -1); 
            SessionTimesAtEndOfSectors.Add(2, -1); 
            SessionTimesAtEndOfSectors.Add(3, -1);
        }
        
        public OpponentDelta getTimeDifferenceToPlayer(SessionData playerSessionData)
        {
            int lastSectorPlayerCompleted = playerSessionData.SectorNumber == 1 ? 3 : playerSessionData.SectorNumber - 1;
            float playerLapTimeToUse = playerSessionData.LapTimePrevious;
            if (playerLapTimeToUse == 0 || playerLapTimeToUse == -1) 
            {
                playerLapTimeToUse = playerSessionData.LapTimePreviousEstimateForInvalidLap;
            }

            if (playerSessionData.SessionTimesAtEndOfSectors[lastSectorPlayerCompleted] == -1 || SessionTimesAtEndOfSectors[lastSectorPlayerCompleted] == -1)
            {
                return null;
            }
            float timeDifference;
            if (Position == playerSessionData.Position + 1) 
            {
                timeDifference = -1 * playerSessionData.TimeDeltaBehind;
            }
            else if (Position == playerSessionData.Position - 1)
            {
                timeDifference = playerSessionData.TimeDeltaFront;
            }
            else 
            { 
                timeDifference = playerSessionData.SessionTimesAtEndOfSectors[lastSectorPlayerCompleted] - SessionTimesAtEndOfSectors[lastSectorPlayerCompleted];
            }
            // if the player is ahead, the time difference is negative

            if (((playerSessionData.CompletedLaps == CompletedLaps + 1 && timeDifference < 0 && CurrentSectorNumber < playerSessionData.SectorNumber) || 
                playerSessionData.CompletedLaps > CompletedLaps + 1 ||
                (playerSessionData.CompletedLaps == CompletedLaps - 1 && timeDifference > 0 && CurrentSectorNumber >= playerSessionData.SectorNumber) || 
                playerSessionData.CompletedLaps < CompletedLaps - 1))
            {
                // there's more than a lap difference
                return new OpponentDelta(-1, playerSessionData.CompletedLaps - CompletedLaps);
            }
            else if (playerSessionData.CompletedLaps == CompletedLaps + 1 && timeDifference > 0)
            {
                // the player has completed 1 more lap but is behind on track
                return new OpponentDelta(timeDifference - playerLapTimeToUse, 0);
            }
            else if (playerSessionData.CompletedLaps == CompletedLaps - 1 && timeDifference < 0)
            {
                // the player has completed 1 less lap but is ahead on track
                return new OpponentDelta(playerLapTimeToUse - timeDifference, 0);
            } 
            else 
            {
                return new OpponentDelta(timeDifference, 0);
            }
        }

        public class OpponentDelta
        {
            public float time;
            public int lapDifference;
            public OpponentDelta(float time, int lapDifference)
            {
                this.time = time;
                this.lapDifference = lapDifference;
            }
        }
    }

    public class ControlData
    {
        // ...
        public ControlType ControlType = ControlType.Unavailable;

        // ...
        public Single ThrottlePedal = 0;

        // ...
        public Single BrakePedal = 0;

        // ...
        public Single ClutchPedal = 0;

        // ...
        public Single BrakeBias = 0;

        // should DRS be in here?
        public Boolean DrsEnabled = false;

        public Boolean DrsAvailable = false;

        public Boolean DrsEngaged = false;

        // steering?
        // Anti roll bar?
        // traction control?
    }

    public class PitData
    {
        public PitWindow PitWindow = PitWindow.Unavailable;

        // The minute/lap into which you're allowed/obligated to pit
        // Unit: Minutes in time-based sessions, otherwise lap

        public Boolean InPitlane = false;

        public Boolean OnInLap = false;

        public Boolean OnOutLap = false;

        public int LapCountWhenLastEnteredPits = 0;

        public Boolean IsMakingMandatoryPitStop = false;

        // this is true for one tick, when the player is about to exit the pits
        public Boolean IsAtPitExit = false;

        public Boolean IsRefuellingAllowed = false;

        public Boolean HasRequestedPitStop = false;

        public Boolean LeaderIsPitting = false;

        public Boolean CarInFrontIsPitting = false;

        public Boolean CarBehindIsPitting = false;
    }

    public class PenatiesData
    {
        public Boolean HasDriveThrough = false;

        public Boolean HasStopAndGo = false;

        // from R3E data - what is this??
        public Boolean HasPitStop = false;

        public Boolean HasTimeDeduction = false;

        public Boolean HasSlowDown = false;

        // Number of penalties pending for the player
        public int NumPenalties = 0;

        // Total number of cut track warnings
        public int CutTrackWarnings = 0;

        public Boolean IsOffRacingSurface = false;
    }

    public class TyreData
    {
        public Boolean LeftFrontAttached = true;
        public Boolean RightFrontAttached = true;
        public Boolean LeftRearAttached = true;
        public Boolean RightRearAttached = true;

        public Boolean TireWearActive = false;

        // true if all tyres are the same type
        public Boolean HasMatchedTyreTypes = true;

        public TyreType FrontLeftTyreType = TyreType.Unknown_Race;
        public TyreType FrontRightTyreType = TyreType.Unknown_Race;
        public TyreType RearLeftTyreType = TyreType.Unknown_Race;
        public TyreType RearRightTyreType = TyreType.Unknown_Race;

        public Single FrontLeft_LeftTemp = 0;
        public Single FrontLeft_CenterTemp = 0;
        public Single FrontLeft_RightTemp = 0;

        public Single FrontRight_LeftTemp = 0;
        public Single FrontRight_CenterTemp = 0;
        public Single FrontRight_RightTemp = 0;

        public Single RearLeft_LeftTemp = 0;
        public Single RearLeft_CenterTemp = 0;
        public Single RearLeft_RightTemp = 0;

        public Single RearRight_LeftTemp = 0;
        public Single RearRight_CenterTemp = 0;
        public Single RearRight_RightTemp = 0;

        public Single PeakFrontLeftTemperatureForLap = 0;
        public Single PeakFrontRightTemperatureForLap = 0;
        public Single PeakRearLeftTemperatureForLap = 0;
        public Single PeakRearRightTemperatureForLap = 0;

        public float FrontLeftPercentWear = 0;
        public float FrontRightPercentWear = 0;
        public float RearLeftPercentWear = 0;
        public float RearRightPercentWear = 0;

        public Single FrontLeftPressure = 0;
        public Single FrontRightPressure = 0;
        public Single RearLeftPressure = 0;
        public Single RearRightPressure = 0;

        public CornerData TyreTempStatus = new CornerData();

        public CornerData TyreConditionStatus = new CornerData();

        public CornerData BrakeTempStatus = new CornerData();

        public Single LeftFrontBrakeTemp = 0;
        public Single RightFrontBrakeTemp = 0;
        public Single LeftRearBrakeTemp = 0;
        public Single RightRearBrakeTemp = 0;

        public Boolean LeftFrontIsLocked = false;
        public Boolean RightFrontIsLocked = false;
        public Boolean LeftRearIsLocked = false;
        public Boolean RightRearIsLocked = false;
        public Boolean LeftFrontIsSpinning = false;
        public Boolean RightFrontIsSpinning = false;
        public Boolean LeftRearIsSpinning = false;
        public Boolean RightRearIsSpinning = false;

    }

    public class GameStateData
    {
        public long Ticks;

        public DateTime Now;

        public CarData.CarClass carClass;

        public EngineData EngineData = new EngineData();

        public TransmissionData TransmissionData = new TransmissionData();

        public FuelData FuelData = new FuelData();

        public CarDamageData CarDamageData = new CarDamageData();

        public ControlData ControlData = new ControlData();

        public SessionData SessionData = new SessionData();

        public PitData PitData = new PitData();

        public PenatiesData PenaltiesData = new PenatiesData();

        public TyreData TyreData = new TyreData();

        public PositionAndMotionData PositionAndMotionData = new PositionAndMotionData();

        public Dictionary<Object, OpponentData> OpponentData = new Dictionary<Object, OpponentData>();

        public GameStateData(long ticks)
        {
            this.Ticks = ticks;
            this.Now = new DateTime(ticks);
        }

        // some convenience methods
        public Boolean isLast()
        {
            return SessionData.Position == SessionData.NumCars;
        }

        public List<String> getRawDriverNames()
        {
            List<String> rawDriverNames = new List<String>();
            foreach (KeyValuePair<Object, OpponentData> entry in OpponentData)
            {
                rawDriverNames.Add(entry.Value.DriverRawName);                
            }
            rawDriverNames.Sort();
            return rawDriverNames;
        }

        public OpponentData getOpponentAtPosition(int position) 
        {
            Object opponentKey = getOpponentKeyAtPosition(position);
            if (opponentKey != null && OpponentData.ContainsKey(opponentKey))
            {
                return OpponentData[opponentKey];
            }
            else
            {
                return null;
            }
        }

        public Object getOpponentKeyInFrontOnTrack()
        {
            Object opponentKeyClosestInFront = null;
            Object opponentKeyFurthestBehind = null;
            float closestDistanceFront = SessionData.TrackDefinition.trackLength;
            float furthestDistanceBehind = 0;
            foreach (KeyValuePair<Object, OpponentData> opponent in OpponentData)
            {
                if (opponent.Value.DistanceRoundTrack > PositionAndMotionData.DistanceRoundTrack &&
                    opponent.Value.DistanceRoundTrack - PositionAndMotionData.DistanceRoundTrack < closestDistanceFront)
                {
                    closestDistanceFront = opponent.Value.DistanceRoundTrack - PositionAndMotionData.DistanceRoundTrack;
                    opponentKeyClosestInFront = opponent.Value;
                }
                else if (opponent.Value.DistanceRoundTrack < PositionAndMotionData.DistanceRoundTrack &&
                    PositionAndMotionData.DistanceRoundTrack - opponent.Value.DistanceRoundTrack > furthestDistanceBehind)
                {
                    furthestDistanceBehind = PositionAndMotionData.DistanceRoundTrack - opponent.Value.DistanceRoundTrack;
                    opponentKeyFurthestBehind = opponent.Value;
                }
            }
            if (opponentKeyClosestInFront != null)
            {
                return opponentKeyClosestInFront;
            }
            else
            {
                return opponentKeyFurthestBehind;
            }
        }

        public Object getOpponentKeyBehindOnTrack()
        {
            Object opponentKeyClosestBehind = null;
            Object opponentKeyFurthestInFront = null;
            float closestDistanceBehind = SessionData.TrackDefinition.trackLength;
            float furthestDistanceInFront = 0;
            foreach (KeyValuePair<Object, OpponentData> opponent in OpponentData)
            {
                if (PositionAndMotionData.DistanceRoundTrack > opponent.Value.DistanceRoundTrack &&
                    PositionAndMotionData.DistanceRoundTrack - opponent.Value.DistanceRoundTrack < closestDistanceBehind)
                {
                    closestDistanceBehind = PositionAndMotionData.DistanceRoundTrack - opponent.Value.DistanceRoundTrack;
                    opponentKeyClosestBehind = opponent.Value;
                }
                else if (PositionAndMotionData.DistanceRoundTrack < opponent.Value.DistanceRoundTrack &&
                    opponent.Value.DistanceRoundTrack - PositionAndMotionData.DistanceRoundTrack > furthestDistanceInFront)
                {
                    furthestDistanceInFront = opponent.Value.DistanceRoundTrack - PositionAndMotionData.DistanceRoundTrack;
                    opponentKeyFurthestInFront = opponent.Value;
                }
            }
            if (opponentKeyClosestBehind != null)
            {
                return opponentKeyClosestBehind;
            }
            else
            {
                return opponentKeyFurthestInFront;
            }
        }

        public OpponentData getOpponentAtPositionWhenStartingSector3(int position)
        {
            if (OpponentData != null && OpponentData.Count != 0)
            {
                foreach (KeyValuePair<Object, OpponentData> entry in OpponentData)
                {
                    if (entry.Value.PositionAtSector3 == position)
                    {
                        return entry.Value;
                    }
                }
            }
            return null;
        }

        public Object getOpponentKeyInFront()
        {
            if (SessionData.Position > 1)
            {
                 return getOpponentKeyAtPosition(SessionData.Position - 1);
            }
            else
            {
                return null;
            }
        }

        public Object getOpponentKeyBehind()
        {
            if (SessionData.Position < SessionData.NumCars)
            {
                return getOpponentKeyAtPosition(SessionData.Position + 1);
            }
            else
            {
                return null;
            }
        }

        public Object getOpponentKeyAtPosition(int position)
        {
            if (OpponentData.Count != 0)
            {
                foreach (KeyValuePair<Object, OpponentData> entry in OpponentData)
                {
                    if (entry.Value.Position == position)
                    {
                        return entry.Key;
                    }
                }
            }
            return null;
        }

        public float getBestOpponentLapTime()
        {
            float bestLapTime = -1;
            foreach (KeyValuePair<Object, OpponentData> entry in OpponentData)
            {
                if (entry.Value.ApproximateLastLapTime > 0 &&
                    (bestLapTime == -1 || entry.Value.ApproximateLastLapTime < bestLapTime))
                {
                    bestLapTime = entry.Value.ApproximateLastLapTime;
                }
            }
            return bestLapTime;
        }

        public void display()
        {
            Console.WriteLine("Laps completed = " + SessionData.CompletedLaps);
            Console.WriteLine("Time elapsed = " + SessionData.SessionRunningTime);
            Console.WriteLine("Position = " + SessionData.Position);
            Console.WriteLine("Session phase = " + SessionData.SessionPhase);
        }

        public void displayOpponentData()
        {
            Console.WriteLine("got " + OpponentData.Count + " opponents");
            foreach (KeyValuePair<Object, OpponentData> opponent in OpponentData)
            {
                Console.WriteLine("last laptime " + opponent.Value.ApproximateLastLapTime + " completed laps " + opponent.Value.CompletedLaps + 
                    " ID " + opponent.Key + " name " + opponent.Value.DriverRawName + " active " + opponent.Value.IsActive + 
                    " approx speed " + opponent.Value.Speed + " position " + opponent.Value.Position);
            }
        }
    }
}
