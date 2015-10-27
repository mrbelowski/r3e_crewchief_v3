using CrewChiefV3.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV3.Events
{
    class LapTimes : AbstractEvent
    {
        Boolean readLapTimes = UserSettings.GetUserSettings().getBoolean("read_lap_times");
        Boolean reportSectorDeltas = UserSettings.GetUserSettings().getBoolean("report_sector_deltas");
        Boolean reportSectorTimes = UserSettings.GetUserSettings().getBoolean("report_sector_times");

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
        private String folderPaceGood = "lap_times/pace_good";
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

        // if the lap is within 0.2% of the best lap time play a message
        private Single goodLapPercent = 0.2f;

        // if the lap is within 1% of the previous lap it's considered consistent
        private Single consistencyLimit = 1f;

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

        private Boolean enableLapTimeMessages = UserSettings.GetUserSettings().getBoolean("enable_laptime_messages");

        private SessionType sessionType;

        private GameStateData currentGameState;

        private int paceCheckLapsWindowForRace = 3;

        public LapTimes(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
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
            if (currentGameState.SessionData.LapTimePrevious > 0 && currentGameState.SessionData.IsNewLap)
            {
                deltaPlayerLastToSessionBestInClass = TimeSpan.FromSeconds(
                    currentGameState.SessionData.LapTimePrevious - currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass);
                deltaPlayerLastToSessionBestOverall = TimeSpan.FromSeconds(
                    currentGameState.SessionData.PlayerLapTimeSessionBest - currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass);
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
            if (currentGameState.SessionData.IsNewLap && !currentGameState.PitData.OnInLap && !currentGameState.PitData.OnOutLap &&
                ((currentGameState.SessionData.SessionType == SessionType.HotLap && currentGameState.SessionData.CompletedLaps > 0) || currentGameState.SessionData.CompletedLaps > 1))
            {
                if (lapTimesWindow == null)
                {
                    lapTimesWindow = new List<float>(lapTimesWindowSize);
                }
                // this might be NO_DATA
                float[] opponentsBestLapData = new float[] {-1, -1, -1, -1};
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
                        // queue the actual laptime as a 'gap filler' - this is only played if the
                        // queue would otherwise be empty
                        if (enableLapTimeMessages && readLapTimes && currentGameState.SessionData.SessionType != SessionType.HotLap)
                        {
                            QueuedMessage gapFillerLapTime = new QueuedMessage("laptime",
                                MessageContents(folderLapTimeIntro, TimeSpan.FromSeconds(currentGameState.SessionData.LapTimePrevious)), 0, this);
                            gapFillerLapTime.gapFiller = true;
                            audioPlayer.queueClip(gapFillerLapTime);
                        }

                        if (enableLapTimeMessages &&
                            (currentGameState.SessionData.SessionType == SessionType.Qualify || currentGameState.SessionData.SessionType == SessionType.Practice ||
                            currentGameState.SessionData.SessionType == SessionType.HotLap))
                        {
                            if (currentGameState.SessionData.SessionType == SessionType.HotLap)
                            {
                                // special case for hot lapping - read best lap message and the laptime
                                audioPlayer.queueClip(new QueuedMessage("laptime", 
                                    MessageContents(folderLapTimeIntro, TimeSpan.FromSeconds(currentGameState.SessionData.LapTimePrevious)), 0, this));
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
                                List<MessageFragment> sectorMessageFragments = getSectorDeltaMessages(true, currentGameState.SessionData.LastSector1Time, currentGameState.SessionData.BestSector1Time,
                                    currentGameState.SessionData.LastSector2Time, currentGameState.SessionData.BestSector2Time, currentGameState.SessionData.LastSector3Time, currentGameState.SessionData.BestSector3Time);
                                if (sectorMessageFragments.Count > 0)
                                {
                                    audioPlayer.queueClip(new QueuedMessage("sectorsHotLap", sectorMessageFragments, 0, this));
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
                                List<MessageFragment> sectorMessageFragments = getSectorDeltaMessages(true, currentGameState.SessionData.LastSector1Time, opponentsBestLapData[1],
                                    currentGameState.SessionData.LastSector2Time, opponentsBestLapData[2], currentGameState.SessionData.LastSector3Time, opponentsBestLapData[3]);
                                if (sectorMessageFragments.Count > 0)
                                {
                                    audioPlayer.queueClip(new QueuedMessage("sectorsQual", sectorMessageFragments, 0, this));
                                }
                            }
                        }
                        else if (enableLapTimeMessages && currentGameState.SessionData.SessionType == SessionType.Race)
                        {
                            Boolean playedLapMessage = false;
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
                                    playedLapMessage = true;
                                    audioPlayer.queueClip(new QueuedMessage(folderMatchingCurrentRacePace, 0, this), PearlsOfWisdom.PearlType.GOOD, pearlLikelihood);
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
                                    if (random.NextDouble() > 0.8)
                                    {
                                        playedLapMessage = true;
                                        audioPlayer.queueClip(new QueuedMessage(folderGoodLap, 0, this), PearlsOfWisdom.PearlType.NEUTRAL, pearlLikelihood);
                                    }
                                    break;
                                default:
                                    break;
                            }
                            List<MessageFragment> sectorMessageFragments = getSectorDeltaMessages(false, currentGameState.SessionData.LastSector1Time, opponentsBestLapData[1],
                                    currentGameState.SessionData.LastSector2Time, opponentsBestLapData[2], currentGameState.SessionData.LastSector3Time, opponentsBestLapData[3]);
                            if (sectorMessageFragments.Count > 0)
                            {
                                audioPlayer.queueClip(new QueuedMessage("sectorsRace", sectorMessageFragments, 0, this));
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
                                if (consistency == ConsistencyResult.WORSENING)
                                {
                                    lastConsistencyUpdate = currentGameState.SessionData.CompletedLaps;
                                    audioPlayer.queueClip(new QueuedMessage(folderWorseningTimes, 0, this));
                                }
                            }
                        }
                    }
                }               
                lapIsValid = true;
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
                if (currentGameState.SessionData.OpponentsLapTimeSessionBestOverall >= currentGameState.SessionData.LapTimePrevious)
                {
                    return LastLapRating.BEST_OVERALL;
                }
                else if (currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass >= currentGameState.SessionData.LapTimePrevious)
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
                        List<MessageFragment> sectorDeltaMessages = getSectorDeltaMessages(false, currentGameState.SessionData.LastSector1Time, bestOpponentLapData[1],
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
                        List<MessageFragment> sectorDeltaMessages = getSectorDeltaMessages(true, currentGameState.SessionData.BestSector1Time, bestOpponentLapData[1],
                            currentGameState.SessionData.BestSector2Time, bestOpponentLapData[2], currentGameState.SessionData.BestSector3Time, bestOpponentLapData[3]);
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

        private List<MessageFragment> getSectorDeltaMessages(Boolean reportAllDeltas, float playerSector1, float comparisonSector1, float playerSector2, 
            float comparisonSector2, float playerSector3, float comparisonSector3)
        {
            List<MessageFragment> messageFragments = new List<MessageFragment>();
            if (!reportSectorTimes)
            {
                return messageFragments;
            }
            if (reportAllDeltas)
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
                if (minDeltaSectorNumber != 0)
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
                switch (sector) {
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
    }
}
