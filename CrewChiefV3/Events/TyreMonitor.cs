using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.RaceRoom.RaceRoomData;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    class TyreMonitor : AbstractEvent
    {
        private String folderHotLeftFront = "tyre_monitor/hot_left_front";
        private String folderHotLeftRear = "tyre_monitor/hot_left_rear";
        private String folderHotRightFront = "tyre_monitor/hot_right_front";
        private String folderHotRightRear = "tyre_monitor/hot_right_rear";
        private String folderHotFronts = "tyre_monitor/hot_fronts";
        private String folderHotRears = "tyre_monitor/hot_rears";
        private String folderHotLefts = "tyre_monitor/hot_lefts";
        private String folderHotRights = "tyre_monitor/hot_rights";
        private String folderHotAllRound = "tyre_monitor/hot_all_round";
        private String folderGoodTemps = "tyre_monitor/good_temps";

        private String folderKnackeredLeftFront = "tyre_monitor/knackered_left_front";
        private String folderKnackeredLeftRear = "tyre_monitor/knackered_left_rear";
        private String folderKnackeredRightFront = "tyre_monitor/knackered_right_front";
        private String folderKnackeredRightRear = "tyre_monitor/knackered_right_rear";
        private String folderKnackeredFronts = "tyre_monitor/knackered_fronts";
        private String folderKnackeredRears = "tyre_monitor/knackered_rears";
        private String folderKnackeredLefts = "tyre_monitor/knackered_lefts";
        private String folderKnackeredRights = "tyre_monitor/knackered_rights";
        private String folderKnackeredAllRound = "tyre_monitor/knackered_all_round";
        private String folderGoodWear = "tyre_monitor/good_wear";

        private String folderWornLeftFront = "tyre_monitor/worn_left_front";
        private String folderWornLeftRear = "tyre_monitor/worn_left_rear";
        private String folderWornRightFront = "tyre_monitor/worn_right_front";
        private String folderWornRightRear = "tyre_monitor/worn_right_rear";
        private String folderWornFronts = "tyre_monitor/worn_fronts";
        private String folderWornRears = "tyre_monitor/worn_rears";
        private String folderWornLefts = "tyre_monitor/worn_lefts";
        private String folderWornRights = "tyre_monitor/worn_rights";
        private String folderWornAllRound = "tyre_monitor/worn_all_round";

        private String folderLapsOnCurrentTyresIntro = "tyre_monitor/laps_on_current_tyres_intro";
        private String folderLapsOnCurrentTyresOutro = "tyre_monitor/laps_on_current_tyres_outro";
        private String folderMinutesOnCurrentTyresIntro = "tyre_monitor/minutes_on_current_tyres_intro";
        private String folderMinutesOnCurrentTyresOutro = "tyre_monitor/minutes_on_current_tyres_outro";

        private static float maxColdTemp = UserSettings.GetUserSettings().getFloat("max_cold_tyre_temp");
        private static float maxGoodTemp = UserSettings.GetUserSettings().getFloat("max_good_tyre_temp");
        private static Boolean enableTyreTempWarnings = UserSettings.GetUserSettings().getBoolean("enable_tyre_temp_warnings");
        private static Boolean enableTyreWearWarnings = UserSettings.GetUserSettings().getBoolean("enable_tyre_wear_warnings");

        private int tyreTempMessageDelay = 0;

        private int lapsIntoSessionBeforeTempMessage = 2;

        private TyreTemps lastLapTyreTemps;
        private TyreTemps thisLapTyreTemps;

        private TyreTempStatus lastReportedStatus;

        private Boolean checkedTempsAtSector3;

        // -1 means we only check at the end of the lap
        private int checkAtSector = -1;

        private TyreWearStatus lastReportedKnackeredTyreStatus;
        private TyreWearStatus lastReportedWornTyreStatus;

        private Boolean reportedTyreWearForCurrentPitEntry;

        private Boolean reportedEstimatedTimeLeft;

        private TyreCondition leftFrontCondition;
        private TyreCondition rightFrontCondition;
        private TyreCondition leftRearCondition;
        private TyreCondition rightRearCondition;

        private float leftFrontWearPercent;
        private float rightFrontWearPercent;
        private float leftRearWearPercent;
        private float rightRearWearPercent;

        private int completedLaps;
        private int lapsInSession;
        private float timeInSession;
        private float timeElapsed;
        
        public TyreMonitor(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            lastLapTyreTemps = null;
            thisLapTyreTemps = null;
            lastReportedStatus = TyreTempStatus.NO_DATA;
            lastReportedKnackeredTyreStatus = TyreWearStatus.NOT_TRIGGERED;
            lastReportedWornTyreStatus = TyreWearStatus.NOT_TRIGGERED;
            checkedTempsAtSector3 = false;
            leftFrontCondition = TyreCondition.UNKNOWN;
            rightFrontCondition = TyreCondition.UNKNOWN;
            leftRearCondition = TyreCondition.UNKNOWN;
            rightRearCondition = TyreCondition.UNKNOWN;
            reportedTyreWearForCurrentPitEntry = false;
            reportedEstimatedTimeLeft = false;
            leftFrontWearPercent = 0;
            leftRearWearPercent = 0;
            rightFrontWearPercent = 0;
            rightRearWearPercent = 0;
            completedLaps = 0;
            lapsInSession = 0;
            timeInSession = 0;
            timeElapsed = 0;
        }

        
        private void checkTemps(TyreTemps tyreTempsToCheck)
        {
            // only give a message if we've completed more than the minimum laps here
            if (tyreTempsToCheck != null)
            {
                tyreTempsToCheck.displayAverages();
                TyreTempStatus tempsStatus = tyreTempsToCheck.getAverageTempStatus();
                if (tempsStatus != lastReportedStatus)
                {
                    String messageFolder = getMessage(tempsStatus);
                    if (messageFolder != null)
                    {
                        Console.WriteLine("Reporting tyre temp status: " + tempsStatus);
                        audioPlayer.queueClip(messageFolder, tyreTempMessageDelay, this);
                    }
                    lastReportedStatus = tempsStatus;
                }
                else
                {
                    Console.WriteLine("No tyre temp status change: " + tempsStatus);
                }
            }
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.TyreData.TireWearActive)
            {
                leftFrontCondition = currentGameState.TyreData.FrontLeftCondition;
                rightFrontCondition = currentGameState.TyreData.FrontRightCondition;
                leftRearCondition = currentGameState.TyreData.RearLeftCondition;
                rightRearCondition = currentGameState.TyreData.RearRightCondition;

                leftFrontWearPercent = currentGameState.TyreData.FrontLeftPercentWear;
                leftRearWearPercent = currentGameState.TyreData.RearLeftPercentWear;
                rightFrontWearPercent = currentGameState.TyreData.FrontRightPercentWear;
                rightRearWearPercent = currentGameState.TyreData.RearRightPercentWear;

                completedLaps = currentGameState.SessionData.CompletedLaps;
                lapsInSession = currentGameState.SessionData.SessionNumberOfLaps;
                timeInSession = currentGameState.SessionData.SessionRunTime;
                timeElapsed = currentGameState.SessionData.SessionRunningTime;
                    
                if (currentGameState.PitData.InPitlane && !currentGameState.SessionData.LeaderHasFinishedRace)
                {
                    if (enableTyreWearWarnings && !reportedTyreWearForCurrentPitEntry)
                    {
                        playTyreWearMessages(true, true);
                        reportedTyreWearForCurrentPitEntry = true;
                    }
                }
                else
                {
                    reportedTyreWearForCurrentPitEntry = false;
                }
                if (currentGameState.SessionData.IsNewLap && !currentGameState.PitData.InPitlane && enableTyreWearWarnings && !currentGameState.SessionData.LeaderHasFinishedRace)
                {
                    playTyreWearMessages(true, false);
                }
                if (!currentGameState.PitData.InPitlane && !reportedEstimatedTimeLeft && enableTyreWearWarnings && !currentGameState.SessionData.LeaderHasFinishedRace)
                {
                    reportEstimatedTyreLife(33, false);
                }
                // if the tyre wear has actually decreased, reset the 'reportdEstimatedTyreWear flag - assume this means the tyres have been changed
                if (previousGameState != null && (currentGameState.TyreData.FrontLeftPercentWear < previousGameState.TyreData.FrontLeftPercentWear ||
                    currentGameState.TyreData.FrontRightPercentWear < previousGameState.TyreData.FrontRightPercentWear ||
                    currentGameState.TyreData.RearRightPercentWear < previousGameState.TyreData.RearRightPercentWear ||
                    currentGameState.TyreData.RearLeftPercentWear < previousGameState.TyreData.RearLeftPercentWear))
                {
                    reportedEstimatedTimeLeft = true;
                }
            }
            if (currentGameState.SessionData.IsNewLap)
            {
                lastLapTyreTemps = thisLapTyreTemps;    // this might still be null
                thisLapTyreTemps = new TyreTemps();
                updateTyreTemps(currentGameState.TyreData, thisLapTyreTemps);
                if (!currentGameState.PitData.InPitlane && enableTyreTempWarnings && !checkedTempsAtSector3 &&
                    currentGameState.SessionData.CompletedLaps >= lapsIntoSessionBeforeTempMessage && !currentGameState.SessionData.LeaderHasFinishedRace)
                {
                    checkTemps(lastLapTyreTemps);
                }
                checkedTempsAtSector3 = false;                                                       
            }
            else
            {
                if (thisLapTyreTemps == null)
                {
                    thisLapTyreTemps = new TyreTemps();
                }
                updateTyreTemps(currentGameState.TyreData, thisLapTyreTemps);
                if (enableTyreTempWarnings && checkAtSector > 0 && currentGameState.SessionData.IsNewSector && currentGameState.SessionData.SectorNumber == checkAtSector)
                {
                    checkedTempsAtSector3 = true;
                    if (!currentGameState.PitData.InPitlane && currentGameState.SessionData.CompletedLaps >= lapsIntoSessionBeforeTempMessage && !currentGameState.SessionData.LeaderHasFinishedRace)
                    {
                        checkTemps(thisLapTyreTemps);
                    }
                }
            }
        }

        private void playEstimatedTypeLifeMinutes(int minutesRemainingOnTheseTyres, Boolean immediate)
        {
            if (immediate)
            {
                if (minutesRemainingOnTheseTyres > 59 || minutesRemainingOnTheseTyres > (timeInSession - timeElapsed) / 60)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderGoodWear, new QueuedMessage(0, this));
                    audioPlayer.closeChannel();
                    return;
                }
                else if (minutesRemainingOnTheseTyres < 1)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderKnackeredAllRound, new QueuedMessage(0, this));
                    audioPlayer.closeChannel();
                    return;
                }
            }
            if (minutesRemainingOnTheseTyres < 59 && minutesRemainingOnTheseTyres > 1 &&
                        minutesRemainingOnTheseTyres <= (timeInSession - timeElapsed) / 60)
            {
                List<String> messages = new List<String>();
                messages.Add(folderMinutesOnCurrentTyresIntro);
                messages.Add(QueuedMessage.folderNameNumbersStub + minutesRemainingOnTheseTyres);
                messages.Add(folderMinutesOnCurrentTyresOutro);
                if (immediate)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "minutes_on_current_tyres",
                        new QueuedMessage(messages, 0, this));
                    audioPlayer.closeChannel();
                }
                else
                {
                    audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "minutes_on_current_tyres",
                    new QueuedMessage(messages, 0, this));
                }
            }
            else if (immediate)
            {
                audioPlayer.openChannel();
                audioPlayer.playClipImmediately(AudioPlayer.folderNoData, new QueuedMessage(0, this));
                audioPlayer.closeChannel();
            }
        }

        private void playEstimatedTypeLifeLaps(int lapsRemainingOnTheseTyres, Boolean immediate)
        {
            if (immediate)
            {
                if (lapsRemainingOnTheseTyres > 59 || lapsRemainingOnTheseTyres > lapsInSession - completedLaps)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderGoodWear, new QueuedMessage(0, this));
                    audioPlayer.closeChannel();
                    return;
                }
                else if (lapsRemainingOnTheseTyres < 1)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderKnackeredAllRound, new QueuedMessage(0, this));
                    audioPlayer.closeChannel();
                    return;
                }
            }
            if (lapsRemainingOnTheseTyres < 59 && lapsRemainingOnTheseTyres > 1 &&
                        lapsRemainingOnTheseTyres <= lapsInSession - completedLaps)
            {
                List<String> messages = new List<String>();
                messages.Add(folderLapsOnCurrentTyresIntro);
                messages.Add(QueuedMessage.folderNameNumbersStub + lapsRemainingOnTheseTyres);
                messages.Add(folderLapsOnCurrentTyresOutro);
                if (immediate)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "laps_on_current_tyres",
                        new QueuedMessage(messages, 0, this));
                    audioPlayer.closeChannel();
                }
                else
                {
                    audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "laps_on_current_tyres",
                        new QueuedMessage(messages, 0, this));
                }                
            }
            else if (immediate)
            {
                audioPlayer.openChannel();
                audioPlayer.playClipImmediately(AudioPlayer.folderNoData, new QueuedMessage(0, this));
                audioPlayer.closeChannel();
            }
        }

        private void reportEstimatedTyreLife(float maxWearThreshold, Boolean immediate)
        {
            float maxWearPercent = Math.Max(leftFrontWearPercent, Math.Max(rightFrontWearPercent, Math.Max(leftRearWearPercent, rightRearWearPercent)));
            if (maxWearPercent >= maxWearThreshold)
            {
                // 1/3 through the tyre's life
                reportedEstimatedTimeLeft = true;
                if (lapsInSession > 0)
                {
                    int lapsRemainingOnTheseTyres = (int)(completedLaps / (maxWearPercent / 100)) - completedLaps - 1;
                    playEstimatedTypeLifeLaps(lapsRemainingOnTheseTyres, immediate);
                }
                else
                {
                    int minutesRemainingOnTheseTyres = (int)Math.Round((timeElapsed / (maxWearPercent / 100)) - timeElapsed - 1);
                    playEstimatedTypeLifeMinutes(minutesRemainingOnTheseTyres, immediate);
                }
            }
        }

        public override void respond(string voiceMessage)
        {
            if (voiceMessage.Contains(SpeechRecogniser.TYRE_TEMPS))
            {
                Boolean gotData = false;
                if (thisLapTyreTemps != null)
                {
                    TyreTempStatus status = thisLapTyreTemps.getCurrentTempStatus();
                    String messageFolder = getMessage(status);
                    if (messageFolder != null)
                    {
                        Console.WriteLine("Tyre temp status is: " + status);
                        thisLapTyreTemps.displayCurrent();
                        gotData = true;
                        audioPlayer.openChannel();
                        audioPlayer.playClipImmediately(messageFolder, new QueuedMessage(0, this));
                        audioPlayer.closeChannel();
                    }
                }
                if (!gotData)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(AudioPlayer.folderNoData, new QueuedMessage(0, this));
                    audioPlayer.closeChannel();
                }
            }
            else if (voiceMessage.Contains(SpeechRecogniser.TYRE_WEAR))
            {
                playTyreWearMessages(false, true);
                reportEstimatedTyreLife(10, true);
                audioPlayer.closeChannel();
            }
        }

        private void playTyreWearMessages(Boolean isQueuedMessage, Boolean playGoodWearMessage)
        {
            TyreWearStatus knackeredTyres = getKnackeredTyreWearStatus();
            TyreWearStatus wornTyres = getWornTyreWearStatus();
            if (knackeredTyres == TyreWearStatus.NOT_TRIGGERED && wornTyres == TyreWearStatus.NOT_TRIGGERED)
            {
                if (playGoodWearMessage)
                {
                    if (isQueuedMessage)
                    {
                        audioPlayer.queueClip(folderGoodWear, 1, this);
                    }
                    else
                    {
                        audioPlayer.openChannel();
                        audioPlayer.playClipImmediately(folderGoodWear, new QueuedMessage(0, this));
                    }
                }
            }
            else
            {
                lastReportedKnackeredTyreStatus = knackeredTyres;
                lastReportedWornTyreStatus = wornTyres;
                reportTyreWearStatus(knackeredTyres, isQueuedMessage);
                reportTyreWearStatus(wornTyres, isQueuedMessage);
            }
        }

        private TyreWearStatus getKnackeredTyreWearStatus()
        {
            if (leftFrontCondition == TyreCondition.WORN_OUT && leftRearCondition == TyreCondition.WORN_OUT &&
                rightFrontCondition == TyreCondition.WORN_OUT && rightRearCondition == TyreCondition.WORN_OUT)
            {
                // all knackered
                return TyreWearStatus.KNACKERED_ALL_ROUND;
            }
            else if (leftFrontCondition == TyreCondition.WORN_OUT && rightFrontCondition == TyreCondition.WORN_OUT)
            {
                // knackered fronts
                return TyreWearStatus.KNACKERED_FRONTS;
            }
            else if (leftRearCondition == TyreCondition.WORN_OUT && rightRearCondition == TyreCondition.WORN_OUT)
            {
                // knackered rears
                return TyreWearStatus.KNACKERED_REARS;
            }
            else if (leftFrontCondition == TyreCondition.WORN_OUT && leftRearCondition == TyreCondition.WORN_OUT)
            {
                // knackered lefts
                return TyreWearStatus.KNACKERED_LEFTS;
            }
            else if (rightFrontCondition == TyreCondition.WORN_OUT && rightRearCondition == TyreCondition.WORN_OUT)
            {
                // knackered rights
                return TyreWearStatus.KNACKERED_RIGHTS;
            }
            else if (leftFrontCondition == TyreCondition.WORN_OUT)
            {
                // knackered left front
                return TyreWearStatus.KNACKERED_LEFT_FRONT;
            }
            else if (leftRearCondition == TyreCondition.WORN_OUT)
            {
                // knackered left rear
                return TyreWearStatus.KNACKERED_LEFT_REAR;
            }
            else if (rightFrontCondition == TyreCondition.WORN_OUT)
            {
                // knackered right front
                return TyreWearStatus.KNACKERED_RIGHT_FRONT;
            }
            else if (rightRearCondition == TyreCondition.WORN_OUT)
            {
                // knackered right rear
                return TyreWearStatus.KNACKERED_RIGHT_REAR;
            }
            else
            {
                return TyreWearStatus.NOT_TRIGGERED;
            }
        }

        private TyreWearStatus getWornTyreWearStatus()
        {
            if (leftFrontCondition == TyreCondition.MAJOR_WEAR && leftRearCondition == TyreCondition.MAJOR_WEAR &&
                rightFrontCondition == TyreCondition.MAJOR_WEAR && rightRearCondition == TyreCondition.MAJOR_WEAR)
            {
                // all worn
                return TyreWearStatus.WORN_ALL_ROUND;
            }
            else if (leftFrontCondition == TyreCondition.MAJOR_WEAR && rightFrontCondition == TyreCondition.MAJOR_WEAR)
            {
                // worn fronts
                return TyreWearStatus.WORN_FRONTS;
            }
            else if (leftRearCondition == TyreCondition.MAJOR_WEAR && rightRearCondition == TyreCondition.MAJOR_WEAR)
            {
                // worn rears
                return TyreWearStatus.WORN_REARS;
            }
            else if (leftFrontCondition == TyreCondition.MAJOR_WEAR && leftRearCondition == TyreCondition.MAJOR_WEAR)
            {
                // worn lefts
                return TyreWearStatus.WORN_LEFTS;
            }
            else if (rightFrontCondition == TyreCondition.MAJOR_WEAR && rightRearCondition == TyreCondition.MAJOR_WEAR)
            {
                // worn rights
                return TyreWearStatus.WORN_RIGHTS;
            }
            else if (leftFrontCondition == TyreCondition.MAJOR_WEAR)
            {
                // worn left front
                return TyreWearStatus.WORN_LEFT_FRONT;
            }
            else if (leftRearCondition == TyreCondition.MAJOR_WEAR)
            {
                // worn left rear
                return TyreWearStatus.WORN_LEFT_REAR;
            }
            else if (rightFrontCondition == TyreCondition.MAJOR_WEAR)
            {
                // worn right front
                return TyreWearStatus.WORN_RIGHT_FRONT;
            }
            else if (rightRearCondition == TyreCondition.MAJOR_WEAR)
            {
                // worn right rear
                return TyreWearStatus.WORN_RIGHT_REAR;
            }
            else
            {
                return TyreWearStatus.NOT_TRIGGERED;
            }
        }

        private void reportTyreWearStatus(TyreWearStatus tyreWearStatus, Boolean isQueuedMessage)
        {
            String clipToPlay = null;
            switch (tyreWearStatus)
            {
                case TyreWearStatus.KNACKERED_ALL_ROUND:
                    clipToPlay = folderKnackeredAllRound;
                    break;
                case TyreWearStatus.KNACKERED_FRONTS:
                    clipToPlay = folderKnackeredFronts;
                    break;
                case TyreWearStatus.KNACKERED_REARS:
                    clipToPlay = folderKnackeredRears;
                    break;
                case TyreWearStatus.KNACKERED_LEFTS:
                    clipToPlay = folderKnackeredLefts;
                    break;
                case TyreWearStatus.KNACKERED_RIGHTS:
                    clipToPlay = folderKnackeredRights;
                    break;
                case TyreWearStatus.KNACKERED_LEFT_FRONT:
                    clipToPlay = folderKnackeredLeftFront;
                    break;
                case TyreWearStatus.KNACKERED_LEFT_REAR:
                    clipToPlay = folderKnackeredLeftRear;
                    break;
                case TyreWearStatus.KNACKERED_RIGHT_FRONT:
                    clipToPlay = folderKnackeredRightFront;
                    break;
                case TyreWearStatus.KNACKERED_RIGHT_REAR:
                    clipToPlay = folderKnackeredRightRear;
                    break;
                case TyreWearStatus.WORN_ALL_ROUND:
                    clipToPlay = folderWornAllRound;
                    break;
                case TyreWearStatus.WORN_FRONTS:
                    clipToPlay = folderWornFronts;
                    break;
                case TyreWearStatus.WORN_REARS:
                    clipToPlay = folderWornRears;
                    break;
                case TyreWearStatus.WORN_LEFTS:
                    clipToPlay = folderWornLefts;
                    break;
                case TyreWearStatus.WORN_RIGHTS:
                    clipToPlay = folderWornRights;
                    break;
                case TyreWearStatus.WORN_LEFT_FRONT:
                    clipToPlay = folderWornLeftFront;
                    break;
                case TyreWearStatus.WORN_LEFT_REAR:
                    clipToPlay = folderWornLeftRear;
                    break;
                case TyreWearStatus.WORN_RIGHT_FRONT:
                    clipToPlay = folderWornRightFront;
                    break;
                case TyreWearStatus.WORN_RIGHT_REAR:
                    clipToPlay = folderWornRightRear;
                    break;
            }
            if (clipToPlay != null)
            {
                if (isQueuedMessage)
                {
                    audioPlayer.queueClip(clipToPlay, 0, this);
                }
                else
                {
                    audioPlayer.playClipImmediately(clipToPlay, new QueuedMessage(0, this));
                    audioPlayer.closeChannel();
                }
            }
        }

        private String getMessage(TyreTempStatus tempStatus)
        {
            switch (tempStatus)
            {
                case TyreTempStatus.GOOD:
                    return folderGoodTemps;
                case TyreTempStatus.HOT_ALL_ROUND:
                    return folderHotAllRound;
                case TyreTempStatus.HOT_FRONTS:
                    return folderHotFronts;
                case TyreTempStatus.HOT_REARS:
                    return folderHotRears;
                case TyreTempStatus.HOT_LEFTS:
                    return folderHotLefts;
                case TyreTempStatus.HOT_RIGHTS:
                    return folderHotRights;
                case TyreTempStatus.HOT_LEFT_FRONT:
                    return folderHotLeftFront;
                case TyreTempStatus.HOT_LEFT_REAR:
                    return folderHotLeftRear;
                case TyreTempStatus.HOT_RIGHT_FRONT:
                    return folderHotRightFront;
                case TyreTempStatus.HOT_RIGHT_REAR:
                    return folderHotRightRear;
            }
            return null;
        }

        private void updateTyreTemps(TyreData tyreData, TyreTemps tyreTemps)
        {
            tyreTemps.addSample((tyreData.FrontLeft_LeftTemp + tyreData.FrontLeft_CenterTemp + tyreData.FrontLeft_RightTemp) / 3,
                (tyreData.FrontRight_LeftTemp + tyreData.FrontRight_CenterTemp + tyreData.FrontRight_RightTemp) / 3,
                (tyreData.RearLeft_LeftTemp + tyreData.RearLeft_CenterTemp + tyreData.RearLeft_RightTemp) / 3,
                (tyreData.RearRight_LeftTemp + tyreData.RearRight_CenterTemp + tyreData.RearRight_RightTemp) / 3);
        }

        private class TyreTemps
        {
            // these are for the average temp over a single lap
            private float totalLeftFrontTemp = 0;
            private float totalRightFrontTemp = 0;
            private float totalLeftRearTemp = 0;
            private float totalRightRearTemp = 0;

            // these are the instantaneous tyre temps
            public float currentLeftFrontTemp = 0;
            public float currentRightFrontTemp = 0;
            public float currentLeftRearTemp = 0;
            public float currentRightRearTemp = 0;

            private int tyreTempSamples = 0;

            public TyreTemps()
            {

            }
            public void displayAverages()
            {
                Console.WriteLine("Average temps: " + getAverageTempStatus() + "\nleft front: " + getLeftFrontAverage() + " right front: " + getRightFrontAverage() +
                    "\nleft rear: " + getLeftRearAverage() + " right rear: " + getRightRearAverage());
            }
            public void displayCurrent()
            {
                Console.WriteLine("Current temps: " + getCurrentTempStatus() + "\nleft front: " + currentLeftFrontTemp + " right front: " + currentRightFrontTemp +
                    "\nleft rear: " + currentLeftRearTemp + " right rear: " + currentRightRearTemp);
            }
            public void addSample(float leftFrontTemp, float rightFrontTemp, float leftRearTemp, float rightRearTemp)
            {
                tyreTempSamples++;
                totalLeftFrontTemp += leftFrontTemp;
                totalRightFrontTemp += rightFrontTemp;
                totalLeftRearTemp += leftRearTemp;
                totalRightRearTemp += rightRearTemp;

                currentLeftFrontTemp = leftFrontTemp;
                currentRightFrontTemp = rightFrontTemp;
                currentLeftRearTemp = leftRearTemp;
                currentRightRearTemp = rightRearTemp;
            }
            public float getLeftFrontAverage()
            {
                if (tyreTempSamples == 0)
                {
                    return 0;
                }
                return totalLeftFrontTemp / tyreTempSamples;
            }
            public float getLeftRearAverage()
            {
                if (tyreTempSamples == 0)
                {
                    return 0;
                }
                return totalLeftRearTemp / tyreTempSamples;
            }
            public float getRightFrontAverage()
            {
                if (tyreTempSamples == 0)
                {
                    return 0;
                }
                return totalRightFrontTemp / tyreTempSamples;
            }
            public float getRightRearAverage()
            {
                if (tyreTempSamples == 0)
                {
                    return 0;
                }
                return totalRightRearTemp / tyreTempSamples;
            }

            public TyreTempStatus getAverageTempStatus()
            {
                return getStatus(getLeftFrontAverage(), getRightFrontAverage(), getLeftRearAverage(), getRightRearAverage());
            }

            public TyreTempStatus getCurrentTempStatus()
            {
                return getStatus(currentLeftFrontTemp, currentRightFrontTemp, currentLeftRearTemp, currentRightRearTemp);
            }

            private TyreTempStatus getStatus(float leftFrontTemp, float rightFrontTemp, float leftRearTemp, float rightRearTemp)
            {
                if (leftFrontTemp < maxColdTemp && leftRearTemp < maxColdTemp &&
                    rightFrontTemp < maxColdTemp && rightRearTemp < maxColdTemp)
                {
                    return TyreTempStatus.COLD;
                }
                if (leftFrontTemp > maxGoodTemp && leftRearTemp > maxGoodTemp &&
                    rightFrontTemp > maxGoodTemp && rightRearTemp > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_ALL_ROUND;
                }
                else if (leftFrontTemp > maxGoodTemp && rightFrontTemp > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_FRONTS;
                }
                else if (leftRearTemp > maxGoodTemp && rightRearTemp > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_REARS;
                }
                else if (leftFrontTemp > maxGoodTemp && leftRearTemp > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_LEFTS;
                }
                else if (rightFrontTemp > maxGoodTemp && rightRearTemp > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_RIGHTS;
                }
                else if (leftFrontTemp > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_LEFT_FRONT;
                }
                else if (leftRearTemp > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_LEFT_REAR;
                }
                else if (rightFrontTemp > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_RIGHT_FRONT;
                }
                else if (rightRearTemp > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_RIGHT_REAR;
                }
                else
                {
                    return TyreTempStatus.GOOD;
                }
            }
        }

        private enum TyreTempStatus
        {
            HOT_LEFT_FRONT, HOT_RIGHT_FRONT, HOT_LEFT_REAR, HOT_RIGHT_REAR,
            HOT_FRONTS, HOT_REARS, HOT_LEFTS, HOT_RIGHTS, HOT_ALL_ROUND, GOOD, COLD, NO_DATA
        }

        private enum TyreWearStatus
        {
            KNACKERED_LEFT_FRONT, KNACKERED_RIGHT_FRONT, KNACKERED_LEFT_REAR, KNACKERED_RIGHT_REAR,
            KNACKERED_FRONTS, KNACKERED_REARS, KNACKERED_LEFTS, KNACKERED_RIGHTS, KNACKERED_ALL_ROUND,
            WORN_LEFT_FRONT, WORN_RIGHT_FRONT, WORN_LEFT_REAR, WORN_RIGHT_REAR,
            WORN_FRONTS, WORN_REARS, WORN_LEFTS, WORN_RIGHTS, WORN_ALL_ROUND, NOT_TRIGGERED
        }
    }
}
