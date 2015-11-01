﻿using CrewChiefV3.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV3.Events
{
    class LapTimes : AbstractEvent
    {        
        int frequencyOfRaceSectorDeltaReports = UserSettings.GetUserSettings().getInt("frequency_of_race_sector_delta_reports");
        int frequencyOfPracticeAndQualSectorDeltaReports = UserSettings.GetUserSettings().getInt("frequency_of_prac_and_qual_sector_delta_reports");
        int frequencyOfPlayerRaceLapTimeReports = UserSettings.GetUserSettings().getInt("frequency_of_player_race_lap_time_reports");
        Boolean raceSectorReportsAtEachSector = UserSettings.GetUserSettings().getBoolean("race_sector_reports_at_each_sector");
        Boolean practiceAndQualSectorReportsAtEachSector = UserSettings.GetUserSettings().getBoolean("practice_and_qual_sector_reports_at_each_sector"); 
        Boolean raceSectorReportsAtLapEnd = UserSettings.GetUserSettings().getBoolean("race_sector_reports_at_lap_end");
        Boolean practiceAndQualSectorReportsLapEnd = UserSettings.GetUserSettings().getBoolean("practice_and_qual_sector_reports_at_lap_end");

        int maxQueueLengthForRaceSectorDeltaReports = 0;
        int maxQueueLengthForRaceLapTimeReports = 0;

        // for qualifying:
        // "that was a 1:34.2, you're now 0.4 seconds off the pace"
        public static String folderLapTimeIntro = "lap_times/time_intro";
        private String folderGapIntro = "lap_times/gap_intro";
        private String folderGapOutroOffPace = "lap_times/gap_outro_off_pace";
        // "that was a 1:34.2, you're fastest in your class"
        private String folderFastestInClass = "lap_times/fastest_in_your_class";

        private String folderLessThanATenthOffThePace = "lap_times/less_than_a_tenth_off_the_pace";

        private String folderQuickerThanSecondPlace = "lap_times/quicker_than_second_place";

        private String folderQuickestOverall = "lap_times/quickest_overall";

        private String folderPaceOK = "lap_times/pace_ok";
        private String folderPaceBad = "lap_times/pace_bad";
        private String folderNeedToFindOneMoreTenth = "lap_times/need_to_find_one_more_tenth";
        private String folderNeedToFindASecond = "lap_times/need_to_find_a_second";
        private String folderNeedToFindMoreThanASecond = "lap_times/need_to_find_more_than_a_second";
        private String folderNeedToFindAFewMoreTenths = "lap_times/need_to_find_a_few_more_tenths";

        // for race:
        private String folderBestLapInRace = "lap_times/best_lap_in_race";
        private String folderBestLapInRaceForClass = "lap_times/best_lap_in_race_for_class";

        private String folderGoodLap = "lap_times/good_lap";

        private String folderConsistentTimes = "lap_times/consistent";

        private String folderImprovingTimes = "lap_times/improving";

        private String folderWorseningTimes = "lap_times/worsening";

        private String folderPersonalBest = "lap_times/personal_best";
        private String folderSettingCurrentRacePace = "lap_times/setting_current_race_pace";
        private String folderMatchingCurrentRacePace = "lap_times/matching_race_pace";

        private String folderSector1Fastest = "lap_times/sector1_fastest";
        private String folderSector2Fastest = "lap_times/sector2_fastest";
        private String folderSector3Fastest = "lap_times/sector3_fastest";

        private String folderSector1Fast = "lap_times/sector1_fast";
        private String folderSector2Fast = "lap_times/sector2_fast";
        private String folderSector3Fast = "lap_times/sector3_fast";

        private String folderSector1ATenthOffThePace = "lap_times/sector1_a_tenth_off_pace";
        private String folderSector2ATenthOffThePace = "lap_times/sector2_a_tenth_off_pace";
        private String folderSector3ATenthOffThePace = "lap_times/sector3_a_tenth_off_pace";

        private String folderSector1TwoTenthsOffThePace = "lap_times/sector1_two_tenths_off_pace";
        private String folderSector2TwoTenthsOffThePace = "lap_times/sector2_two_tenths_off_pace";
        private String folderSector3TwoTenthsOffThePace = "lap_times/sector3_two_tenths_off_pace";

        private String folderSector1AFewTenthsOffThePace = "lap_times/sector1_a_few_tenths_off_pace";
        private String folderSector2AFewTenthsOffThePace = "lap_times/sector2_a_few_tenths_off_pace";
        private String folderSector3AFewTenthsOffThePace = "lap_times/sector3_a_few_tenths_off_pace";

        private String folderSector1ASecondOffThePace = "lap_times/sector1_a_second_off_pace";
        private String folderSector2ASecondOffThePace = "lap_times/sector2_a_second_off_pace";
        private String folderSector3ASecondOffThePace = "lap_times/sector3_a_second_off_pace";

        private String folderSector1MoreThanASecondOffThePace = "lap_times/sector1_more_than_a_second_off_pace";
        private String folderSector2MoreThanASecondOffThePace = "lap_times/sector2_more_than_a_second_off_pace";
        private String folderSector3MoreThanASecondOffThePace = "lap_times/sector3_more_than_a_second_off_pace";


        public static String folderSectorFastest = "lap_times/sector_fastest";
        public static String folderSectorFast = "lap_times/sector_fast";
        public static String folderSectorATenthOffPace = "lap_times/sector_a_tenth_off_pace";
        public static String folderSectorTwoTenthsOffPace = "lap_times/sector_two_tenths_off_pace";
        public static String folderSectorAFewTenthsOffPace = "lap_times/sector_a_few_tenths_off_pace";
        public static String folderSectorASecondOffPace = "lap_times/sector_a_second_off_pace";
        public static String folderSectorMoreThanASecondOffPace = "lap_times/sector_more_than_a_second_off_pace";

        public static String folderSector1Is = "lap_times/sector1_is";
        public static String folderSector2Is = "lap_times/sector2_is";
        public static String folderSector3Is = "lap_times/sector3_is";
        public static String folderSectors1and2Are = "lap_times/sectors_1_and_2_are";
        public static String folderSectors1and3Are = "lap_times/sectors_1_and_3_are";
        public static String folderSectors2and3Are = "lap_times/sectors_2_and_3_are";
        public static String folderSectorsAllThreeAre = "lap_times/all_three_sectors_are";


        // if the lap is within 0.3% of the best lap time play a message
        private Single goodLapPercent = 0.3f;

        private Single matchingRacePacePercent = 0.1f;

        // if the lap is within 0.5% of the previous lap it's considered consistent
        private Single consistencyLimit = 0.5f;

        private List<float> lapTimesWindow;

        private int lapTimesWindowSize = 3;

        private ConsistencyResult lastConsistencyMessage;

        // lap number when the last consistency update was made
        private int lastConsistencyUpdate;

        private Boolean lapIsValid;

        private LastLapRating lastLapRating;

        private TimeSpan deltaPlayerBestToSessionBestInClass;

        private TimeSpan deltaPlayerBestToSessionBestOverall;

        private TimeSpan deltaPlayerLastToSessionBestInClass;

        private TimeSpan deltaPlayerLastToSessionBestOverall;

        private float lastLapTime;

        private int currentPosition;

        private Random random = new Random();

        private SessionType sessionType;

        private GameStateData currentGameState;

        private int paceCheckLapsWindowForRace = 3;

        public LapTimes(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
            if (frequencyOfRaceSectorDeltaReports > 7)
            {
                maxQueueLengthForRaceSectorDeltaReports = 7;
            } 
            else if (frequencyOfRaceSectorDeltaReports > 5)
            {
                maxQueueLengthForRaceSectorDeltaReports = 5;
            }
            else
            {
                maxQueueLengthForRaceSectorDeltaReports = 4;
            }
            if (frequencyOfPlayerRaceLapTimeReports > 7)
            {
                maxQueueLengthForRaceLapTimeReports = 7;
            }
            else if (frequencyOfPlayerRaceLapTimeReports > 5)
            {
                maxQueueLengthForRaceLapTimeReports = 5;
            }
            else
            {
                maxQueueLengthForRaceLapTimeReports = 4;
            }
        }

        public override void clearState()
        {
            lapTimesWindow = new List<float>(lapTimesWindowSize);
            lastConsistencyUpdate = 0;
            lastConsistencyMessage = ConsistencyResult.NOT_APPLICABLE;
            lapIsValid = true;
            lastLapRating = LastLapRating.NO_DATA;
            deltaPlayerBestToSessionBestInClass = TimeSpan.MaxValue;
            deltaPlayerBestToSessionBestOverall = TimeSpan.MaxValue;
            deltaPlayerLastToSessionBestInClass = TimeSpan.MaxValue;
            deltaPlayerLastToSessionBestOverall = TimeSpan.MaxValue;
            lastLapTime = 0;
            currentPosition = -1;
            currentGameState = null;
        }

        protected override void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            sessionType = currentGameState.SessionData.SessionType;
            this.currentGameState = currentGameState;
            if (currentGameState.SessionData.IsNewLap)
            {
                if (currentGameState.SessionData.LapTimePrevious > 0)
                {
                    if (currentGameState.OpponentData.Count > 0)
                    {
                        if (currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass > 0)
                        {
                            deltaPlayerLastToSessionBestInClass = TimeSpan.FromSeconds(
                                currentGameState.SessionData.LapTimePrevious - currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass);
                        }
                        if (currentGameState.SessionData.OpponentsLapTimeSessionBestOverall > 0)
                        {
                            deltaPlayerLastToSessionBestOverall = TimeSpan.FromSeconds(
                                currentGameState.SessionData.LapTimePrevious - currentGameState.SessionData.OpponentsLapTimeSessionBestOverall);
                        }
                    }
                    else if (currentGameState.SessionData.PlayerLapTimeSessionBest > 0)
                    {
                        deltaPlayerLastToSessionBestOverall = TimeSpan.FromSeconds(currentGameState.SessionData.LapTimePrevious - currentGameState.SessionData.PlayerLapTimeSessionBest);
                        deltaPlayerLastToSessionBestInClass = deltaPlayerLastToSessionBestOverall;
                    }
                    if (currentGameState.SessionData.LapTimePrevious <= currentGameState.SessionData.PlayerLapTimeSessionBest)
                    {
                        deltaPlayerBestToSessionBestInClass = deltaPlayerLastToSessionBestInClass;
                        deltaPlayerBestToSessionBestOverall = deltaPlayerLastToSessionBestOverall;
                    }
                }
                else
                {
                    // the last lap was invalid so the delta is undefined
                    deltaPlayerLastToSessionBestInClass = TimeSpan.MaxValue;
                    deltaPlayerLastToSessionBestOverall = TimeSpan.MaxValue;
                }
            }
            currentPosition = currentGameState.SessionData.Position;

            // check the current lap is still valid
            if (lapIsValid && currentGameState.SessionData.CompletedLaps > 0 &&
                !currentGameState.SessionData.IsNewLap && !currentGameState.SessionData.CurrentLapIsValid)
            {
                lapIsValid = false;
            }
            if (currentGameState.SessionData.IsNewLap)
            {
                lastLapTime = currentGameState.SessionData.LapTimePrevious;
            }

            if (!currentGameState.PitData.OnInLap && !currentGameState.PitData.OnOutLap &&
                ((currentGameState.SessionData.SessionType == SessionType.HotLap && currentGameState.SessionData.CompletedLaps > 0) || currentGameState.SessionData.CompletedLaps > 1))
            {
                // report sector delta at the completion of a sector?
                if (currentGameState.SessionData.IsNewSector && (practiceAndQualSectorReportsAtEachSector || raceSectorReportsAtEachSector))
                {
                    double r = random.NextDouble() * 10;
                    Boolean canPlayForRace = frequencyOfRaceSectorDeltaReports > r;
                    Boolean canPlayForPracAndQual = frequencyOfPracticeAndQualSectorDeltaReports > r;
                    if ((currentGameState.SessionData.SessionType == SessionType.Race && canPlayForRace) ||
                        ((currentGameState.SessionData.SessionType == SessionType.Practice || currentGameState.SessionData.SessionType == SessionType.Qualify ||
                        currentGameState.SessionData.SessionType == SessionType.HotLap) && canPlayForPracAndQual))
                    {
                        float playerSector = -1;
                        float comparisonSector = -1;
                        switch (currentGameState.SessionData.SectorNumber) {
                            case 1:
                                playerSector = currentGameState.SessionData.LastSector1Time;
                                comparisonSector = currentGameState.SessionData.BestLapSector1Time;
                                break;
                            case 2:
                                playerSector = currentGameState.SessionData.LastSector2Time;
                                comparisonSector = currentGameState.SessionData.BestLapSector2Time;
                                break;
                            case 3:
                                playerSector = currentGameState.SessionData.LastSector3Time;
                                comparisonSector = currentGameState.SessionData.BestLapSector3Time;
                                break;
                        }
                        if (playerSector != -1 && comparisonSector != -1) {
                            List<MessageFragment> messageFragments = new List<MessageFragment>();
                            messageFragments.Add(getSectorDeltaMessageFragment(currentGameState.SessionData.SectorNumber, playerSector - comparisonSector));
                            audioPlayer.queueClip(new QueuedMessage("sector" + currentGameState.SessionData.SectorNumber + "SectorDelta", 
                                messageFragments, 0, this));
                        }
                    }
                }

                if (currentGameState.SessionData.IsNewLap)
                {
                    if (lapTimesWindow == null)
                    {
                        lapTimesWindow = new List<float>(lapTimesWindowSize);
                    }
                    // this might be NO_DATA
                    float[] opponentsBestLapData = new float[] { -1, -1, -1, -1 };
                    if (currentGameState.SessionData.SessionType == SessionType.Race)
                    {
                        opponentsBestLapData = currentGameState.getTimeAndSectorsForBestOpponentLapInWindow(paceCheckLapsWindowForRace, currentGameState.carClass.carClassEnum);
                    }
                    else if (currentGameState.SessionData.SessionType == SessionType.Qualify || currentGameState.SessionData.SessionType == SessionType.Practice)
                    {
                        opponentsBestLapData = currentGameState.getTimeAndSectorsForBestOpponentLapInWindow(-1, currentGameState.carClass.carClassEnum);
                    }
                    lastLapRating = getLastLapRating(currentGameState, opponentsBestLapData);

                    if (currentGameState.SessionData.PreviousLapWasValid)
                    {
                        lapTimesWindow.Insert(0, currentGameState.SessionData.LapTimePrevious);
                        if (lapIsValid)
                        {
                            Boolean playedLapTime = false;
                            Boolean isHotLapping = currentGameState.SessionData.SessionType == SessionType.HotLap ||
                                (currentGameState.OpponentData.Count == 0 || (
                                    currentGameState.OpponentData.Count == 1 && currentGameState.OpponentData.First().Value.DriverRawName == currentGameState.SessionData.DriverRawName));
                            if (isHotLapping)
                            {
                                // always play the laptime in hotlap mode
                                audioPlayer.queueClip(new QueuedMessage("laptime",
                                        MessageContents(folderLapTimeIntro, TimeSpan.FromSeconds(currentGameState.SessionData.LapTimePrevious)), 0, this));
                                playedLapTime = true;
                            }
                            else if (currentGameState.SessionData.SessionType == SessionType.Qualify || currentGameState.SessionData.SessionType == SessionType.Practice
                                || (currentGameState.SessionData.SessionType == SessionType.Race && frequencyOfPlayerRaceLapTimeReports > random.NextDouble() * 10))
                            {
                                // usually play it in practice / qual mode, occasionally play it in race mode
                                QueuedMessage gapFillerLapTime = new QueuedMessage("laptime",
                                    MessageContents(folderLapTimeIntro, TimeSpan.FromSeconds(currentGameState.SessionData.LapTimePrevious)), 0, this);
                                if (currentGameState.SessionData.SessionType == SessionType.Race)
                                {
                                    gapFillerLapTime.maxPermittedQueueLengthForMessage = maxQueueLengthForRaceLapTimeReports;
                                }
                                audioPlayer.queueClip(gapFillerLapTime);
                                playedLapTime = true;
                            }

                            if (currentGameState.SessionData.SessionType == SessionType.Qualify || currentGameState.SessionData.SessionType == SessionType.Practice ||
                                currentGameState.SessionData.SessionType == SessionType.HotLap)
                            {
                                if (currentGameState.SessionData.SessionType == SessionType.HotLap || currentGameState.OpponentData.Count == 0)
                                {
                                    if (lastLapRating == LastLapRating.BEST_IN_CLASS || deltaPlayerLastToSessionBestOverall <= TimeSpan.Zero)
                                    {
                                        audioPlayer.queueClip(new QueuedMessage(folderPersonalBest, 0, this));
                                    }
                                    else if (deltaPlayerLastToSessionBestOverall < TimeSpan.FromMilliseconds(50))
                                    {
                                        audioPlayer.queueClip(new QueuedMessage(folderLessThanATenthOffThePace, 0, this));
                                    }
                                    else if (deltaPlayerLastToSessionBestOverall < TimeSpan.MaxValue)
                                    {
                                        audioPlayer.queueClip(new QueuedMessage("lapTimeNotRaceGap",
                                            MessageContents(folderGapIntro, deltaPlayerLastToSessionBestOverall, folderGapOutroOffPace), 0, this));
                                    }
                                    if (practiceAndQualSectorReportsLapEnd && frequencyOfPracticeAndQualSectorDeltaReports > random.NextDouble() * 10)
                                    {
                                        // mix up the sector reporting approach a bit...
                                        SectorReportOption reportOption = random.NextDouble() > 0.5 ? SectorReportOption.ALL_SECTORS : SectorReportOption.COMBINED;
                                        List<MessageFragment> sectorMessageFragments = getSectorDeltaMessages(reportOption, currentGameState.SessionData.LastSector1Time, currentGameState.SessionData.BestLapSector1Time,
                                            currentGameState.SessionData.LastSector2Time, currentGameState.SessionData.BestLapSector2Time, currentGameState.SessionData.LastSector3Time, currentGameState.SessionData.BestLapSector3Time);
                                        if (sectorMessageFragments.Count > 0)
                                        {
                                            audioPlayer.queueClip(new QueuedMessage("sectorsHotLap", sectorMessageFragments, 0, this));
                                        }
                                    }
                                }
                                else if (lastLapRating == LastLapRating.BEST_IN_CLASS)
                                {
                                    audioPlayer.queueClip(new QueuedMessage(folderFastestInClass, 0, this));
                                    if (deltaPlayerLastToSessionBestInClass < TimeSpan.Zero)
                                    {
                                        TimeSpan gapBehind = deltaPlayerLastToSessionBestInClass.Negate();
                                        if ((gapBehind.Seconds > 0 || gapBehind.Milliseconds > 50) &&
                                            gapBehind.Seconds < 60)
                                        {
                                            // delay this a bit...
                                            audioPlayer.queueClip(new QueuedMessage("lapTimeNotRaceGap",
                                                MessageContents(folderGapIntro, gapBehind, folderQuickerThanSecondPlace), random.Next(0, 20), this));
                                        }
                                    }
                                }
                                else if (lastLapRating == LastLapRating.BEST_OVERALL)
                                {
                                    if (currentGameState.SessionData.SessionType == SessionType.Qualify)
                                    {
                                        audioPlayer.queueClip(new QueuedMessage(Position.folderPole, 0, this));
                                    }
                                    else if (currentGameState.SessionData.SessionType == SessionType.Practice)
                                    {
                                        audioPlayer.queueClip(new QueuedMessage(Position.folderStub + currentGameState.SessionData.Position, 0, this));
                                    }
                                    if (deltaPlayerLastToSessionBestOverall < TimeSpan.Zero)
                                    {
                                        TimeSpan gapBehind = deltaPlayerLastToSessionBestOverall.Negate();
                                        if ((gapBehind.Seconds > 0 || gapBehind.Milliseconds > 50) &&
                                            gapBehind.Seconds < 60)
                                        {
                                            // delay this a bit...
                                            audioPlayer.queueClip(new QueuedMessage("lapTimeNotRaceGap",
                                                MessageContents(folderGapIntro, gapBehind, folderQuickerThanSecondPlace), random.Next(0, 20), this));
                                        }
                                    }
                                }
                                else
                                {
                                    if (lastLapRating == LastLapRating.PERSONAL_BEST_STILL_SLOW || lastLapRating == LastLapRating.PERSONAL_BEST_CLOSE_TO_CLASS_LEADER ||
                                        lastLapRating == LastLapRating.PERSONAL_BEST_CLOSE_TO_OVERALL_LEADER)
                                    {
                                        audioPlayer.queueClip(new QueuedMessage(folderPersonalBest, 0, this));
                                    }
                                    // don't read this message if the rounded time gap is 0.0 seconds or it's more than 59 seconds
                                    if ((deltaPlayerLastToSessionBestInClass.Seconds > 0 || deltaPlayerLastToSessionBestInClass.Milliseconds > 50) &&
                                        deltaPlayerLastToSessionBestInClass.Seconds < 60)
                                    {
                                        // delay this a bit...
                                        audioPlayer.queueClip(new QueuedMessage("lapTimeNotRaceGap",
                                            MessageContents(folderGapIntro, deltaPlayerLastToSessionBestInClass, folderGapOutroOffPace), random.Next(0, 20), this));
                                    }
                                    if (practiceAndQualSectorReportsLapEnd && frequencyOfPracticeAndQualSectorDeltaReports > random.NextDouble() * 10)
                                    {
                                        SectorReportOption reportOption = random.NextDouble() > 0.5 ? SectorReportOption.ALL_SECTORS : SectorReportOption.COMBINED;
                                        List<MessageFragment> sectorMessageFragments = getSectorDeltaMessages(reportOption, currentGameState.SessionData.LastSector1Time, opponentsBestLapData[1],
                                            currentGameState.SessionData.LastSector2Time, opponentsBestLapData[2], currentGameState.SessionData.LastSector3Time, opponentsBestLapData[3]);
                                        if (sectorMessageFragments.Count > 0)
                                        {
                                            audioPlayer.queueClip(new QueuedMessage("sectorsQual", sectorMessageFragments, 0, this));
                                        }
                                    }
                                }
                            }
                            else if (currentGameState.SessionData.SessionType == SessionType.Race)
                            {
                                Boolean playedLapMessage = false;
                                if (frequencyOfPlayerRaceLapTimeReports > random.NextDouble() * 10)
                                {
                                    float pearlLikelihood = 0.8f;
                                    switch (lastLapRating)
                                    {
                                        case LastLapRating.BEST_OVERALL:
                                            playedLapMessage = true;
                                            audioPlayer.queueClip(new QueuedMessage(folderBestLapInRace, 0, this), PearlsOfWisdom.PearlType.GOOD, pearlLikelihood);
                                            break;
                                        case LastLapRating.BEST_IN_CLASS:
                                            playedLapMessage = true;
                                            audioPlayer.queueClip(new QueuedMessage(folderBestLapInRaceForClass, 0, this), PearlsOfWisdom.PearlType.GOOD, pearlLikelihood);
                                            break;
                                        case LastLapRating.SETTING_CURRENT_PACE:
                                            playedLapMessage = true;
                                            audioPlayer.queueClip(new QueuedMessage(folderSettingCurrentRacePace, 0, this), PearlsOfWisdom.PearlType.GOOD, pearlLikelihood);
                                            break;
                                        case LastLapRating.CLOSE_TO_CURRENT_PACE:
                                            // don't keep playing this one
                                            if (random.NextDouble() < 0.5)
                                            {
                                                playedLapMessage = true;
                                                audioPlayer.queueClip(new QueuedMessage(folderMatchingCurrentRacePace, 0, this), PearlsOfWisdom.PearlType.GOOD, pearlLikelihood);
                                            }
                                            break;
                                        case LastLapRating.PERSONAL_BEST_CLOSE_TO_OVERALL_LEADER:
                                        case LastLapRating.PERSONAL_BEST_CLOSE_TO_CLASS_LEADER:
                                            playedLapMessage = true;
                                            audioPlayer.queueClip(new QueuedMessage(folderGoodLap, 0, this), PearlsOfWisdom.PearlType.GOOD, pearlLikelihood);
                                            break;
                                        case LastLapRating.PERSONAL_BEST_STILL_SLOW:
                                            playedLapMessage = true;
                                            audioPlayer.queueClip(new QueuedMessage(folderPersonalBest, 0, this), PearlsOfWisdom.PearlType.NEUTRAL, pearlLikelihood);
                                            break;
                                        case LastLapRating.CLOSE_TO_OVERALL_LEADER:
                                        case LastLapRating.CLOSE_TO_CLASS_LEADER:
                                            // this is an OK lap but not a PB. We only want to say "decent lap" occasionally here
                                            if (random.NextDouble() < 0.2)
                                            {
                                                playedLapMessage = true;
                                                audioPlayer.queueClip(new QueuedMessage(folderGoodLap, 0, this), PearlsOfWisdom.PearlType.NEUTRAL, pearlLikelihood);
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                }

                                // only report sectors if we've not reported the laptime
                                if (raceSectorReportsAtLapEnd && frequencyOfRaceSectorDeltaReports > random.NextDouble() * 10 && !playedLapTime)
                                {
                                    double r = random.NextDouble();
                                    SectorReportOption reportOption = SectorReportOption.COMBINED;
                                    if (r > 0.66)
                                    {
                                        reportOption = SectorReportOption.BEST_AND_WORST;
                                    }
                                    else if (r > 0.33)
                                    {
                                        reportOption = SectorReportOption.WORST_ONLY;
                                    }
                                    List<MessageFragment> sectorMessageFragments = getSectorDeltaMessages(reportOption, currentGameState.SessionData.LastSector1Time, opponentsBestLapData[1],
                                            currentGameState.SessionData.LastSector2Time, opponentsBestLapData[2], currentGameState.SessionData.LastSector3Time, opponentsBestLapData[3]);
                                    if (sectorMessageFragments.Count > 0)
                                    {
                                        QueuedMessage message = new QueuedMessage("sectorsRace", sectorMessageFragments, 0, this);
                                        message.maxPermittedQueueLengthForMessage = maxQueueLengthForRaceSectorDeltaReports;
                                        audioPlayer.queueClip(message);
                                    }
                                }

                                // play the consistency message if we've not played the good lap message, or sometimes
                                // play them both
                                Boolean playConsistencyMessage = !playedLapMessage || random.NextDouble() < 0.25;
                                if (playConsistencyMessage && currentGameState.SessionData.CompletedLaps >= lastConsistencyUpdate + lapTimesWindowSize &&
                                    lapTimesWindow.Count >= lapTimesWindowSize)
                                {
                                    ConsistencyResult consistency = checkAgainstPreviousLaps();
                                    if (consistency == ConsistencyResult.CONSISTENT)
                                    {
                                        lastConsistencyUpdate = currentGameState.SessionData.CompletedLaps;
                                        audioPlayer.queueClip(new QueuedMessage(folderConsistentTimes, 0, this));
                                    }
                                    else if (consistency == ConsistencyResult.IMPROVING)
                                    {
                                        lastConsistencyUpdate = currentGameState.SessionData.CompletedLaps;
                                        audioPlayer.queueClip(new QueuedMessage(folderImprovingTimes, 0, this));
                                    }
                                    else if (consistency == ConsistencyResult.WORSENING)
                                    {
                                        // don't play the worsening message if the lap rating is good
                                        if (lastLapRating == LastLapRating.BEST_IN_CLASS || lastLapRating == LastLapRating.BEST_OVERALL ||
                                            lastLapRating == LastLapRating.SETTING_CURRENT_PACE || lastLapRating == LastLapRating.CLOSE_TO_CURRENT_PACE)
                                        {
                                            Console.WriteLine("Skipping 'worsening' laptimes message - inconsistent with lap rating");
                                        }
                                        else
                                        {
                                            lastConsistencyUpdate = currentGameState.SessionData.CompletedLaps;
                                            audioPlayer.queueClip(new QueuedMessage(folderWorseningTimes, 0, this));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    lapIsValid = true;
                }
            }
        }
               
        private ConsistencyResult checkAgainstPreviousLaps()
        {
            Boolean isImproving = true;
            Boolean isWorsening = true;
            Boolean isConsistent = true;

            for (int index = 0; index < lapTimesWindowSize - 1; index++)
            {
                // check the lap time was recorded
                if (lapTimesWindow[index] <= 0)
                {
                    Console.WriteLine("no data for consistency check");
                    lastConsistencyMessage = ConsistencyResult.NOT_APPLICABLE;
                    return ConsistencyResult.NOT_APPLICABLE;
                }
                if (lapTimesWindow[index] >= lapTimesWindow[index + 1])
                {
                    isImproving = false;
                    break;
                }
            }

            for (int index = 0; index < lapTimesWindowSize - 1; index++)
            {
                if (lapTimesWindow[index] <= lapTimesWindow[index + 1])
                {
                    isWorsening = false;
                }
            }

            for (int index = 0; index < lapTimesWindowSize - 1; index++)
            {
                float lastLap = lapTimesWindow[index];
                float lastButOneLap = lapTimesWindow[index + 1];
                float consistencyRange = (lastButOneLap * consistencyLimit) / 100;
                if (lastLap > lastButOneLap + consistencyRange || lastLap < lastButOneLap - consistencyRange)
                {
                    isConsistent = false;
                }
            }

            // todo: untangle this mess....
            if (isImproving)
            {
                if (lastConsistencyMessage == ConsistencyResult.IMPROVING)
                {
                    // don't play the same improving message - see if the consistent message might apply
                    if (isConsistent)
                    {
                        lastConsistencyMessage = ConsistencyResult.CONSISTENT;
                        return ConsistencyResult.CONSISTENT;
                    }
                }
                else
                {
                    lastConsistencyMessage = ConsistencyResult.IMPROVING;
                    return ConsistencyResult.IMPROVING;
                }
            }
            if (isWorsening)
            {
                if (lastConsistencyMessage == ConsistencyResult.WORSENING)
                {
                    // don't play the same worsening message - see if the consistent message might apply
                    if (isConsistent)
                    {
                        lastConsistencyMessage = ConsistencyResult.CONSISTENT;
                        return ConsistencyResult.CONSISTENT;
                    }
                }
                else
                {
                    lastConsistencyMessage = ConsistencyResult.WORSENING;
                    return ConsistencyResult.WORSENING;
                }
            }
            if (isConsistent)
            {
                lastConsistencyMessage = ConsistencyResult.CONSISTENT;
                return ConsistencyResult.CONSISTENT;
            }
            return ConsistencyResult.NOT_APPLICABLE;
        }

        private enum ConsistencyResult
        {
            NOT_APPLICABLE, CONSISTENT, IMPROVING, WORSENING
        }

        private LastLapRating getLastLapRating(GameStateData currentGameState, float[] bestLapDataForOpponents)
        {
            if (currentGameState.SessionData.PreviousLapWasValid && currentGameState.SessionData.LapTimePrevious > 0)
            {
                float closeThreshold = currentGameState.SessionData.LapTimePrevious * goodLapPercent / 100;
                float matchingRacePaceThreshold = currentGameState.SessionData.LapTimePrevious * matchingRacePacePercent / 100;
                if (currentGameState.SessionData.OverallSessionBestLapTime >= currentGameState.SessionData.LapTimePrevious)
                {
                    return LastLapRating.BEST_OVERALL;
                }
                else if (currentGameState.SessionData.PlayerClassSessionBestLapTime >= currentGameState.SessionData.LapTimePrevious)
                {
                    return LastLapRating.BEST_IN_CLASS;
                }
                else if (currentGameState.SessionData.SessionType == SessionType.Race && bestLapDataForOpponents[0] > 0 && bestLapDataForOpponents[0] >= currentGameState.SessionData.LapTimePrevious) 
                {
                    return LastLapRating.SETTING_CURRENT_PACE;                
                }
                else if (currentGameState.SessionData.SessionType == SessionType.Race && bestLapDataForOpponents[0] > 0 && bestLapDataForOpponents[0] > currentGameState.SessionData.LapTimePrevious - closeThreshold)
                {
                    return LastLapRating.CLOSE_TO_CURRENT_PACE;
                }
                else if (currentGameState.SessionData.LapTimePrevious <= currentGameState.SessionData.PlayerLapTimeSessionBest)
                {
                    if (currentGameState.SessionData.OpponentsLapTimeSessionBestOverall > currentGameState.SessionData.PlayerLapTimeSessionBest - closeThreshold)
                    {
                        return LastLapRating.PERSONAL_BEST_CLOSE_TO_OVERALL_LEADER;
                    }
                    else if (currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass > currentGameState.SessionData.PlayerLapTimeSessionBest - closeThreshold)
                    {
                        return LastLapRating.PERSONAL_BEST_CLOSE_TO_CLASS_LEADER;
                    }
                    else if (currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass > 0 || currentGameState.SessionData.OpponentsLapTimeSessionBestOverall > 0)
                    {
                        return LastLapRating.PERSONAL_BEST_STILL_SLOW;
                    }
                }
                else if (currentGameState.SessionData.OpponentsLapTimeSessionBestOverall >= currentGameState.SessionData.LapTimePrevious - closeThreshold)
                {
                    return LastLapRating.CLOSE_TO_OVERALL_LEADER;
                }
                else if (currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass >= currentGameState.SessionData.LapTimePrevious - closeThreshold)
                {
                    return LastLapRating.CLOSE_TO_CLASS_LEADER;
                }
                else if (currentGameState.SessionData.PlayerLapTimeSessionBest >= currentGameState.SessionData.LapTimePrevious - closeThreshold)
                {
                    return LastLapRating.CLOSE_TO_PERSONAL_BEST;
                }
                else if (currentGameState.SessionData.PlayerLapTimeSessionBest > 0)
                {
                    return LastLapRating.MEH;
                }
            }
            return LastLapRating.NO_DATA;
        }

        public override void respond(String voiceMessage)
        {
            if (voiceMessage.Contains(SpeechRecogniser.WHAT_ARE_MY_SECTOR_TIMES))
            {
                if (currentGameState.SessionData.LastSector1Time > -1 && currentGameState.SessionData.LastSector2Time > -1 && currentGameState.SessionData.LastSector3Time > -1)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage("sectorTimes",
                        MessageContents(TimeSpan.FromSeconds(currentGameState.SessionData.LastSector1Time), 
                        TimeSpan.FromSeconds(currentGameState.SessionData.LastSector2Time), TimeSpan.FromSeconds(currentGameState.SessionData.LastSector3Time)), 0, this), false);
                }
                else
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, this), false);
                }
                audioPlayer.closeChannel();
            }
            if (voiceMessage.Contains(SpeechRecogniser.WHATS_MY_LAST_SECTOR_TIME))
            {
                if (currentGameState.SessionData.SectorNumber == 1 && currentGameState.SessionData.LastSector3Time > -1)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage("sector3Time",
                        MessageContents(TimeSpan.FromSeconds(currentGameState.SessionData.LastSector3Time)), 0, this), false);
                }
                else if (currentGameState.SessionData.SectorNumber == 2 && currentGameState.SessionData.LastSector1Time > -1)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage("sector1Time",
                        MessageContents(TimeSpan.FromSeconds(currentGameState.SessionData.LastSector1Time)), 0, this), false);
                }
                else if (currentGameState.SessionData.SectorNumber == 3 && currentGameState.SessionData.LastSector2Time > -1)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage("sector2Time",
                        MessageContents(TimeSpan.FromSeconds(currentGameState.SessionData.LastSector2Time)), 0, this), false);
                }
                else
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, this), false);
                }
                audioPlayer.closeChannel();
            }
            if ((voiceMessage.Contains(SpeechRecogniser.LAST_LAP_TIME) ||
                voiceMessage.Contains(SpeechRecogniser.LAP_TIME) ||
                voiceMessage.Contains(SpeechRecogniser.LAST_LAP)))
            {
                if (lastLapTime > 0)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage("lapTimeNotRaceTime",
                        MessageContents(folderLapTimeIntro, TimeSpan.FromSeconds(lastLapTime)), 0, this), false);
                    audioPlayer.closeChannel();
                }
                else
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, this), false);
                    audioPlayer.closeChannel();
                }
            }
            else if (voiceMessage.Contains(SpeechRecogniser.PACE))
            {
                if (sessionType == SessionType.Race)
                {
                    float[] bestOpponentLapData = currentGameState.getTimeAndSectorsForBestOpponentLapInWindow(paceCheckLapsWindowForRace, currentGameState.carClass.carClassEnum);

                    if (bestOpponentLapData[0] > -1 && lastLapRating != LastLapRating.NO_DATA)
                    {
                        TimeSpan lapToCompare = TimeSpan.FromSeconds(lastLapTime - bestOpponentLapData[0]);
                        String timeToFindFolder = null;
                        if (lapToCompare.Seconds == 0 && lapToCompare.Milliseconds < 200)
                        {
                            timeToFindFolder = folderNeedToFindOneMoreTenth;
                        }
                        else if (lapToCompare.Seconds == 0 && lapToCompare.Milliseconds < 600)
                        {
                            timeToFindFolder = folderNeedToFindAFewMoreTenths;
                        }
                        else if ((lapToCompare.Seconds == 1 && lapToCompare.Milliseconds < 500) ||
                            (lapToCompare.Seconds == 0 && lapToCompare.Milliseconds >= 600))
                        {
                            timeToFindFolder = folderNeedToFindASecond;
                        }
                        else if ((lapToCompare.Seconds == 1 && lapToCompare.Milliseconds >= 500) ||
                            lapToCompare.Seconds > 1)
                        {
                            timeToFindFolder = folderNeedToFindMoreThanASecond;
                        }
                        List<MessageFragment> messages = new List<MessageFragment>();
                        switch (lastLapRating)
                        {
                            case LastLapRating.BEST_OVERALL:
                            case LastLapRating.BEST_IN_CLASS:
                            case LastLapRating.SETTING_CURRENT_PACE:
                                audioPlayer.playClipImmediately(new QueuedMessage(folderSettingCurrentRacePace, 0, null), false);
                                break;
                            case LastLapRating.PERSONAL_BEST_CLOSE_TO_OVERALL_LEADER:
                            case LastLapRating.PERSONAL_BEST_CLOSE_TO_CLASS_LEADER:
                            case LastLapRating.CLOSE_TO_OVERALL_LEADER:
                            case LastLapRating.CLOSE_TO_CLASS_LEADER:
                            case LastLapRating.PERSONAL_BEST_STILL_SLOW:
                            case LastLapRating.CLOSE_TO_PERSONAL_BEST:
                            case LastLapRating.CLOSE_TO_CURRENT_PACE:
                                if (timeToFindFolder == null || timeToFindFolder != folderNeedToFindMoreThanASecond)
                                {
                                    if (lastLapRating == LastLapRating.CLOSE_TO_CURRENT_PACE)
                                    {
                                        messages.Add(MessageFragment.Text(folderMatchingCurrentRacePace));
                                    }
                                    else
                                    {
                                        messages.Add(MessageFragment.Text(folderPaceOK));
                                    }
                                }
                                if (timeToFindFolder != null)
                                {
                                    messages.Add(MessageFragment.Text(timeToFindFolder));
                                }
                                if (messages.Count > 0)
                                {
                                    audioPlayer.playClipImmediately(new QueuedMessage("lapTimeRacePaceReport", messages, 0, null), false);
                                }                                    
                                break;
                            case LastLapRating.MEH:
                                messages.Add(MessageFragment.Text(folderPaceBad));
                                if (timeToFindFolder != null)
                                {
                                    messages.Add(MessageFragment.Text(timeToFindFolder));
                                }
                                audioPlayer.playClipImmediately(new QueuedMessage("lapTimeRacePaceReport", messages, 0, null), false);
                                    break;
                            default:
                                audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                                break;                     
                        }
                        SectorReportOption reportOption = SectorReportOption.COMBINED;
                        double r = random.NextDouble();
                        if (r > 0.66)
                        {
                            reportOption = SectorReportOption.BEST_AND_WORST;
                        }
                        else if (r > 0.33)
                        {
                            reportOption = SectorReportOption.WORST_ONLY;
                        }
                        List<MessageFragment> sectorDeltaMessages = getSectorDeltaMessages(reportOption, currentGameState.SessionData.LastSector1Time, bestOpponentLapData[1],
                            currentGameState.SessionData.LastSector2Time, bestOpponentLapData[2], currentGameState.SessionData.LastSector3Time, bestOpponentLapData[3]);
                        if (sectorDeltaMessages.Count > 0)
                        {
                            audioPlayer.playClipImmediately(new QueuedMessage("race_sector_times_report", sectorDeltaMessages, 0, null), false);
                        }
                        audioPlayer.closeChannel();
                    }
                    else {
                        audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                        audioPlayer.closeChannel();
                    }
                }
                else
                {
                    if (deltaPlayerBestToSessionBestInClass != TimeSpan.MaxValue)
                    {
                        float[] bestOpponentLapData = currentGameState.getTimeAndSectorsForBestOpponentLapInWindow(-1, currentGameState.carClass.carClassEnum);
                        if (deltaPlayerBestToSessionBestInClass <= TimeSpan.Zero)
                        {   
                            if (sessionType == SessionType.Qualify && currentPosition == 1)
                            {
                                audioPlayer.playClipImmediately(new QueuedMessage(Position.folderPole, 0, null), false);
                                audioPlayer.closeChannel();
                            }
                            else
                            {
                                audioPlayer.playClipImmediately(new QueuedMessage(folderQuickestOverall, 0, null), false);
                                audioPlayer.closeChannel();
                            }
                            TimeSpan gapBehind = deltaPlayerBestToSessionBestInClass.Negate();
                            if ((gapBehind.Seconds > 0 || gapBehind.Milliseconds > 50) &&
                                gapBehind.Seconds < 60)
                            {
                                // delay this a bit...
                                audioPlayer.queueClip(new QueuedMessage("lapTimeNotRaceGap",
                                    MessageContents(folderGapIntro, gapBehind, folderQuickerThanSecondPlace), random.Next(0, 20), this));
                            }
                            
                        }
                        else if (deltaPlayerBestToSessionBestInClass.Seconds == 0 && deltaPlayerBestToSessionBestInClass.Milliseconds < 50)
                        {
                            if (currentPosition > 1)
                            {
                                // should always trigger
                                audioPlayer.playClipImmediately(new QueuedMessage(Position.folderStub + currentPosition, 0, null), false);
                            }
                            audioPlayer.playClipImmediately(new QueuedMessage(folderLessThanATenthOffThePace, 0, null), false);
                            audioPlayer.closeChannel();
                        }
                        else
                        {
                            if (currentPosition > 1)
                            {
                                // should always trigger
                                audioPlayer.playClipImmediately(new QueuedMessage(Position.folderStub + currentPosition, 0, null), false);
                            }
                            audioPlayer.playClipImmediately(new QueuedMessage("lapTimeNotRaceGap",
                                MessageContents(deltaPlayerBestToSessionBestInClass, folderGapOutroOffPace), 0, null), false);
                        }
                        List<MessageFragment> sectorDeltaMessages = getSectorDeltaMessages(SectorReportOption.ALL_SECTORS, currentGameState.SessionData.BestLapSector1Time, bestOpponentLapData[1],
                            currentGameState.SessionData.BestLapSector2Time, bestOpponentLapData[2], currentGameState.SessionData.BestLapSector3Time, bestOpponentLapData[3]);
                        if (sectorDeltaMessages.Count > 0)
                        {
                            audioPlayer.playClipImmediately(new QueuedMessage("non-race_sector_times_report", sectorDeltaMessages, 0, null), false);
                        }
                        audioPlayer.closeChannel();
                    }
                    else {
                        audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                        audioPlayer.closeChannel();
                    }
                }
            }
        }

        private enum LastLapRating
        {
            BEST_OVERALL, BEST_IN_CLASS, SETTING_CURRENT_PACE, CLOSE_TO_CURRENT_PACE, PERSONAL_BEST_CLOSE_TO_OVERALL_LEADER, PERSONAL_BEST_CLOSE_TO_CLASS_LEADER,
            PERSONAL_BEST_STILL_SLOW, CLOSE_TO_OVERALL_LEADER, CLOSE_TO_CLASS_LEADER, CLOSE_TO_PERSONAL_BEST, MEH, NO_DATA
        }


        // TODO: refactor all this mess

        // ----------------- 'mode 1' - will attempt to piece together sector pace reports from fragments ---------------
        private enum SectorDeltaEnum
        {
            FASTEST, FAST, TENTH_OFF_PACE, TWO_TENTHS_OFF_PACE, A_FEW_TENTHS_OFF_PACE, A_SECOND_OFF_PACE, MORE_THAN_A_SECOND_OFF_PACE, UNKNOWN
        }

        private SectorDeltaEnum getEnumForSectorDelta(float delta)
        {
            if (delta == float.MaxValue)
            {
                return SectorDeltaEnum.UNKNOWN;
            }
            if (delta <= 0.0)
            {
                return SectorDeltaEnum.FASTEST;
            }
            else if (delta > 0.0 && delta < 0.05)
            {
                return SectorDeltaEnum.FAST;
            }
            else if (delta >= 0.05 && delta < 0.15)
            {
                return SectorDeltaEnum.TENTH_OFF_PACE;
            }
            else if (delta >= 0.15 && delta < 0.3)
            {
                return SectorDeltaEnum.TWO_TENTHS_OFF_PACE;
            }
            else if (delta >= 0.3 && delta < 0.7)
            {
                return SectorDeltaEnum.A_FEW_TENTHS_OFF_PACE;
            }
            else if (delta >= 0.7 && delta < 1.2)
            {
                return SectorDeltaEnum.A_SECOND_OFF_PACE;
            }
            else if (delta >= 1.2)
            {
                return SectorDeltaEnum.MORE_THAN_A_SECOND_OFF_PACE;
            }
            return SectorDeltaEnum.UNKNOWN;
        }

        private Dictionary<SectorDeltaEnum, List<int>> getSectorsAndDeltas(float sector1delta, float sector2delta, float sector3delta)
        {
            Dictionary<SectorDeltaEnum, List<int>> sectorsAndDeltas = new Dictionary<SectorDeltaEnum, List<int>>();
            SectorDeltaEnum s1 = getEnumForSectorDelta(sector1delta);
            SectorDeltaEnum s2 = getEnumForSectorDelta(sector2delta);
            SectorDeltaEnum s3 = getEnumForSectorDelta(sector3delta);
            if (s1 != SectorDeltaEnum.UNKNOWN)
            {
                sectorsAndDeltas.Add(s1, new List<int>());
                sectorsAndDeltas[s1].Add(1);
            }
            if (s2 != SectorDeltaEnum.UNKNOWN)
            {
                if (!sectorsAndDeltas.ContainsKey(s2))
                {
                    sectorsAndDeltas.Add(s2, new List<int>());
                }
                sectorsAndDeltas[s2].Add(2);
            }
            if (s3 != SectorDeltaEnum.UNKNOWN)
            {
                if (!sectorsAndDeltas.ContainsKey(s3))
                {
                    sectorsAndDeltas.Add(s3, new List<int>());
                }
                sectorsAndDeltas[s3].Add(3);
            }
            return sectorsAndDeltas;
        }

        private List<MessageFragment> getSectorDeltasMessageFragments(SectorDeltaEnum sectorDeltaEnum, List<int> applicableSectors)
        {
            List<MessageFragment> messageFragments = new List<MessageFragment>();
            String deltaFolder = getFolderForSectorDeltaEnum(sectorDeltaEnum);
            String sectorsFolder = getFolderForApplicableSectors(applicableSectors);
            if (deltaFolder != null && sectorsFolder != null)
            {
                messageFragments.Add(MessageFragment.Text(sectorsFolder));
                messageFragments.Add(MessageFragment.Text(deltaFolder));
            }
            return messageFragments;
        }

        private String getFolderForSectorDeltaEnum(SectorDeltaEnum sectorDeltaEnum)
        {
            switch (sectorDeltaEnum)
            {
                case SectorDeltaEnum.FASTEST:
                    return folderSectorFastest;
                case SectorDeltaEnum.FAST:
                    return folderSectorFast;
                case SectorDeltaEnum.TENTH_OFF_PACE:
                    return folderSectorATenthOffPace;
                case SectorDeltaEnum.TWO_TENTHS_OFF_PACE:
                    return folderSectorTwoTenthsOffPace;
                case SectorDeltaEnum.A_FEW_TENTHS_OFF_PACE:
                    return folderSectorAFewTenthsOffPace;
                case SectorDeltaEnum.A_SECOND_OFF_PACE:
                    return folderSectorASecondOffPace;
                case SectorDeltaEnum.MORE_THAN_A_SECOND_OFF_PACE:
                    return folderSectorMoreThanASecondOffPace;
                default:
                    return null;
            }
        }

        private String getFolderForApplicableSectors(List<int> applicableSectors)
        {
            if (applicableSectors.Contains(1) && applicableSectors.Contains(2) && applicableSectors.Contains(3))
            {
                return folderSectorsAllThreeAre;
            }
            if (applicableSectors.Contains(1) && applicableSectors.Contains(2))
            {
                return folderSectors1and2Are;
            }
            if (applicableSectors.Contains(1) && applicableSectors.Contains(3))
            {
                return folderSectors1and3Are;
            }
            if (applicableSectors.Contains(2) && applicableSectors.Contains(3))
            {
                return folderSectors2and3Are;
            }
            if (applicableSectors.Contains(1))
            {
                return folderSector1Is;
            }
            if (applicableSectors.Contains(2))
            {
                return folderSector2Is;
            }
            if (applicableSectors.Contains(3))
            {
                return folderSector3Is;
            }
            return null;
        }

        private List<MessageFragment> getSectorDeltaMessagesCombined(float playerSector1, float comparisonSector1, float playerSector2, 
            float comparisonSector2, float playerSector3, float comparisonSector3)
        {
            List<MessageFragment> messageFragments = new List<MessageFragment>();
            float sector1delta = float.MaxValue;
            float sector2delta = float.MaxValue;
            float sector3delta = float.MaxValue;
            if (playerSector1 > 0 && comparisonSector1 > 0)
            {
                sector1delta = playerSector1 - comparisonSector1;
            }
            if (playerSector2 > 0 && comparisonSector2 > 0)
            {
                sector2delta = playerSector2 - comparisonSector2;
            }
            if (playerSector3 > 0 && comparisonSector3 > 0)
            {
                sector3delta = playerSector3 - comparisonSector3;
            }
            Dictionary<SectorDeltaEnum, List<int>> sectorsAndDeltas = getSectorsAndDeltas(sector1delta, sector2delta, sector3delta);

            List <MessageFragment> messageFragmentsToPlayLast = new List<MessageFragment>();
            foreach (KeyValuePair<SectorDeltaEnum, List<int>> sectorAndDeltas in sectorsAndDeltas)
            {
                if (sectorAndDeltas.Value.Count == 2)
                {
                    messageFragmentsToPlayLast.AddRange(getSectorDeltasMessageFragments(sectorAndDeltas.Key, sectorAndDeltas.Value));
                }
                else
                {
                    messageFragments.AddRange(getSectorDeltasMessageFragments(sectorAndDeltas.Key, sectorAndDeltas.Value));
                }
            }
            messageFragments.AddRange(messageFragmentsToPlayLast); 
            return messageFragments;
        }



        // -------------- mode 2: will play 2 or 3 sector reports depending on the options and data. Sounds more natural but isn't as informative -------------
        private List<MessageFragment> getSectorDeltaMessages(SectorReportOption reportOption, float playerSector1, float comparisonSector1, float playerSector2,
            float comparisonSector2, float playerSector3, float comparisonSector3)
        {
            List<MessageFragment> messageFragments = new List<MessageFragment>();
            if (reportOption == SectorReportOption.ALL_SECTORS)
            {
                if (playerSector1 > 0 && comparisonSector1 > 0)
                {
                    MessageFragment fragment = getSectorDeltaMessageFragment(1, playerSector1 - comparisonSector1);
                    if (fragment != null)
                    {
                        messageFragments.Add(fragment);
                    }
                }
                if (playerSector2 > 0 && comparisonSector2 > 0)
                {
                    MessageFragment fragment = getSectorDeltaMessageFragment(2, playerSector2 - comparisonSector2);
                    if (fragment != null)
                    {
                        messageFragments.Add(fragment);
                    }
                }
                if (playerSector3 > 0 && comparisonSector3 > 0)
                {
                    MessageFragment fragment = getSectorDeltaMessageFragment(3, playerSector3 - comparisonSector3);
                    if (fragment != null)
                    {
                        messageFragments.Add(fragment);
                    }
                }
            }
            else if (reportOption == SectorReportOption.COMBINED)
            {
                return getSectorDeltaMessagesCombined(playerSector1, comparisonSector1, playerSector2, comparisonSector2, playerSector3, comparisonSector3);
            }
            else
            {
                float maxDelta = float.MinValue;
                int maxDeltaSectorNumber = 0;
                float minDelta = float.MaxValue;
                int minDeltaSectorNumber = 0;

                if (playerSector1 > 0 && comparisonSector1 > 0)
                {
                    float delta = playerSector1 - comparisonSector1;
                    if (delta > maxDelta)
                    {
                        maxDelta = delta;
                        maxDeltaSectorNumber = 1;
                    }
                    else if (delta < minDelta)
                    {
                        minDelta = delta;
                        minDeltaSectorNumber = 1;
                    }
                }
                if (playerSector2 > 0 && comparisonSector2 > 0)
                {
                    float delta = playerSector2 - comparisonSector2;
                    if (delta > maxDelta)
                    {
                        if (maxDeltaSectorNumber != 0)
                        {
                            minDelta = maxDelta;
                            minDeltaSectorNumber = maxDeltaSectorNumber;
                        }
                        maxDelta = playerSector2 - comparisonSector2;
                        maxDeltaSectorNumber = 2;
                    }
                    else if (delta < minDelta)
                    {
                        if (minDeltaSectorNumber != 0)
                        {
                            maxDelta = minDelta;
                            maxDeltaSectorNumber = minDeltaSectorNumber;
                        }
                        minDelta = playerSector2 - comparisonSector2;
                        minDeltaSectorNumber = 2;
                    }
                }
                if (playerSector3 > 0 && comparisonSector3 > 0)
                {
                    float delta = playerSector3 - comparisonSector3;
                    if (delta > maxDelta)
                    {
                        if (maxDeltaSectorNumber != 0 && minDeltaSectorNumber == 0)
                        {
                            minDelta = maxDelta;
                            minDeltaSectorNumber = maxDeltaSectorNumber;
                        }
                        maxDelta = playerSector3 - comparisonSector3;
                        maxDeltaSectorNumber = 3;
                    }
                    else if (delta < minDelta)
                    {
                        if (minDeltaSectorNumber != 0 && maxDeltaSectorNumber == 0)
                        {
                            maxDelta = minDelta;
                            maxDeltaSectorNumber = minDeltaSectorNumber;
                        }
                        minDelta = playerSector3 - comparisonSector3;
                        minDeltaSectorNumber = 3;
                    }
                }
                if (minDeltaSectorNumber != 0 && reportOption != SectorReportOption.WORST_ONLY)
                {
                    messageFragments.Add(getSectorDeltaMessageFragment(minDeltaSectorNumber, minDelta));
                }
                if (maxDeltaSectorNumber != 0)
                {
                    messageFragments.Add(getSectorDeltaMessageFragment(maxDeltaSectorNumber, maxDelta));
                }
            }
            return messageFragments;
        }

        private MessageFragment getSectorDeltaMessageFragment(int sector, float delta)
        {
            if (delta <= 0.0)
            {
                switch (sector)
                {
                    case 1:
                        return MessageFragment.Text(folderSector1Fastest);
                    case 2:
                        return MessageFragment.Text(folderSector2Fastest);
                    case 3:
                        return MessageFragment.Text(folderSector3Fastest);
                }
            }
            else if (delta > 0.0 && delta < 0.05)
            {
                switch (sector)
                {
                    case 1:
                        return MessageFragment.Text(folderSector1Fast);
                    case 2:
                        return MessageFragment.Text(folderSector2Fast);
                    case 3:
                        return MessageFragment.Text(folderSector3Fast);
                }
            }
            else if (delta >= 0.05 && delta < 0.15)
            {
                switch (sector)
                {
                    case 1:
                        return MessageFragment.Text(folderSector1ATenthOffThePace);
                    case 2:
                        return MessageFragment.Text(folderSector2ATenthOffThePace);
                    case 3:
                        return MessageFragment.Text(folderSector3ATenthOffThePace);
                }
            }
            else if (delta >= 0.15 && delta < 0.3)
            {
                switch (sector)
                {
                    case 1:
                        return MessageFragment.Text(folderSector1TwoTenthsOffThePace);
                    case 2:
                        return MessageFragment.Text(folderSector2TwoTenthsOffThePace);
                    case 3:
                        return MessageFragment.Text(folderSector3TwoTenthsOffThePace);
                }
            }
            else if (delta >= 0.3 && delta < 0.7)
            {
                switch (sector)
                {
                    case 1:
                        return MessageFragment.Text(folderSector1AFewTenthsOffThePace);
                    case 2:
                        return MessageFragment.Text(folderSector2AFewTenthsOffThePace);
                    case 3:
                        return MessageFragment.Text(folderSector3AFewTenthsOffThePace);
                }
            }
            else if (delta >= 0.7 && delta < 1.2)
            {
                switch (sector)
                {
                    case 1:
                        return MessageFragment.Text(folderSector1ASecondOffThePace);
                    case 2:
                        return MessageFragment.Text(folderSector2ASecondOffThePace);
                    case 3:
                        return MessageFragment.Text(folderSector3ASecondOffThePace);
                }
            }
            else if (delta >= 1.2)
            {
                switch (sector)
                {
                    case 1:
                        return MessageFragment.Text(folderSector1MoreThanASecondOffThePace);
                    case 2:
                        return MessageFragment.Text(folderSector2MoreThanASecondOffThePace);
                    case 3:
                        return MessageFragment.Text(folderSector3MoreThanASecondOffThePace);
                }
            }
            return null;
        }

        private enum SectorReportOption
        {
            ALL_SECTORS, BEST_AND_WORST, WORST_ONLY, COMBINED
        }
    }
}
