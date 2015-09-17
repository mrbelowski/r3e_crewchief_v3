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
        Hard, Medium, Soft, Wet, Intermediate, Prime, Option, Unknown
    }
    public enum TyreCondition
    {
        UNKNOWN, NEW, SCRUBBED, MINOR_WEAR, MAJOR_WEAR, WORN_OUT
    }
    public enum DamageLevel
    {
        UNKNOWN, NONE, TRIVIAL, MINOR, MAJOR, DESTROYED
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
        
        public DamageLevel LeftFrontSuspensionDamage = DamageLevel.UNKNOWN;

        public DamageLevel LeftRearSuspensionDamage = DamageLevel.UNKNOWN;

        public DamageLevel RightFrontSuspensionDamage = DamageLevel.UNKNOWN;

        public DamageLevel RightRearSuspensionDamage = DamageLevel.UNKNOWN;

    }

    public class SessionData
    {
        public FlagEnum Flag = FlagEnum.GREEN;

        public Boolean IsNewSession = false;

        public Boolean SessionHasFixedTime = false;

        public SessionType SessionType = SessionType.Unavailable;

        public DateTime SessionStartTime = DateTime.Now;

        // in minutes, 0 if this session is a fixed number of laps rather than a fixed time.
        public float SessionRunTime = 0;

        public int SessionNumberOfLaps = 0;

        public int SessionStartPosition = 0;

        public int NumCarsAtStartOfSession = 0;

        public String TrackName = null;

        public String TrackLayout = null;

        public float TrackLength = 0;

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
        public Single LapTimeBest = -1;

        // Unit: Seconds (-1.0 = none)
        public Single LapTimePrevious = -1;

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
        public Single LapTimeBestLeader = 0;

        // ...
        public Single LapTimeBestLeaderClass = 0;

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

        public float SessionTimeAtEndOfLastSector1 = 0;

        public float SessionTimeAtEndOfLastSector2 = 0;

        public float SessionTimeAtEndOfLastSector3 = 0;
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

        public String DriverFirstName = null;

        public String DriverLastName = null;

        public int Position = 0;

        public Single DistanceRoundTrack = 0;

        public int CompletedLaps = 0;

        public int CurrentSectorNumber = 0;

        public Boolean IsPitting = false;
        
        public float SessionTimeAtEndOfLastSector1 = 0;

        public float SessionTimeAtEndOfLastSector2 = 0;

        public float SessionTimeAtEndOfLastSector3 = 0;

        public int LapsCompletedAtEndOfLastSector1 = 0;

        public int LapsCompletedAtEndOfLastSector2 = 0;

        public int LapsCompletedAtEndOfLastSector3 = 0;

        // TODO: the logic in this method is bascially bollocks
        public OpponentDelta getTimeDifferenceToPlayer(SessionData playerSessionData)
        {
            if (playerSessionData.SectorNumber == 1)
            {
                return new OpponentDelta(playerSessionData.SessionTimeAtEndOfLastSector3 - SessionTimeAtEndOfLastSector3,
                    playerSessionData.CompletedLaps - LapsCompletedAtEndOfLastSector3);
            }
            else if (playerSessionData.SectorNumber == 2)
            {
                return new OpponentDelta(playerSessionData.SessionTimeAtEndOfLastSector1 - SessionTimeAtEndOfLastSector1,
                     playerSessionData.CompletedLaps - LapsCompletedAtEndOfLastSector1);
            } 
            else if (playerSessionData.SectorNumber == 3)
            {
                return new OpponentDelta(playerSessionData.SessionTimeAtEndOfLastSector2 - SessionTimeAtEndOfLastSector2,
                     playerSessionData.CompletedLaps - LapsCompletedAtEndOfLastSector2);
            }
            else
            {
                return null;
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

        public Boolean isOffRacingSurface = false;
    }

    public class TyreData
    {
        public Boolean TireWearActive = false;

        // true if all tyres are the same type
        public Boolean HasMatchedTyreTypes = true;

        public TyreType FrontLeftTyreType = TyreType.Unknown;
        public TyreType FrontRightTyreType = TyreType.Unknown;
        public TyreType RearLeftTyreType = TyreType.Unknown;
        public TyreType RearRightTyreType = TyreType.Unknown;

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

        public TyreCondition FrontLeftCondition = TyreCondition.UNKNOWN;
        public TyreCondition FrontRightCondition = TyreCondition.UNKNOWN;
        public TyreCondition RearLeftCondition = TyreCondition.UNKNOWN;
        public TyreCondition RearRightCondition = TyreCondition.UNKNOWN;

        public float FrontLeftPercentWear = 0;
        public float FrontRightPercentWear = 0;
        public float RearLeftPercentWear = 0;
        public float RearRightPercentWear = 0;

        public Single FrontLeftPressure = 0;
        public Single FrontRightPressure = 0;
        public Single RearLeftPressure = 0;
        public Single RearRightPressure = 0;
    }

    class GameStateData
    {
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

        public Dictionary<int, OpponentData> OpponentData = new Dictionary<int, OpponentData>();

        // some convenience methods
        public Boolean isLast()
        {
            return SessionData.Position == SessionData.NumCars;
        }

        public List<String> getOpponentLastNames()
        {
            List<String> lastNames = new List<String>();
            foreach (KeyValuePair<int, OpponentData> entry in OpponentData)
            {
                String driverLastName = entry.Value.DriverLastName;
                if (driverLastName.Length > 0)
                {
                    lastNames.Add(entry.Value.DriverLastName);
                }
                else
                {
                    Console.WriteLine("Driver name invalid " + entry.Value.DriverFirstName + " " + entry.Value.DriverLastName);
                }
            }
            lastNames.Sort();
            return lastNames;
        }

        public OpponentData getOpponentAtPosition(int position) 
        {
            int opponentId = getOpponentIdAtPosition(position);
            if (opponentId != -1 && OpponentData.ContainsKey(opponentId))
            {
                return OpponentData[opponentId];
            }
            else
            {
                return null;
            }
        }

        public int getOpponentIdInFront()
        {
            if (SessionData.Position > 1)
            {
                 return getOpponentIdAtPosition(SessionData.Position - 1);
            }
            else
            {
                return -1;
            }
        }

        public int getOpponentIdBehind()
        {
            if (SessionData.Position < SessionData.NumCars)
            {
                return getOpponentIdAtPosition(SessionData.Position + 1);
            }
            else
            {
                return -1;
            }
        }

        public int getOpponentIdAtPosition(int position)
        {
            if (OpponentData.Count != 0)
            {
                foreach (KeyValuePair<int, OpponentData> entry in OpponentData)
                {
                    if (entry.Value.Position == position - 1)
                    {
                        return entry.Key;
                    }
                }
            }
            return -1;
        }

        public void display()
        {
            Console.WriteLine("Laps completed = " + SessionData.CompletedLaps);
            Console.WriteLine("Time elapsed = " + SessionData.SessionRunningTime);
            Console.WriteLine("Position = " + SessionData.Position);
            Console.WriteLine("Session phase = " + SessionData.SessionPhase);
        }
    }
}
