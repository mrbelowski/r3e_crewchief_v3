﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.RaceRoom.RaceRoomData;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    // note this only works properly in race events as the TimeDeltas aren't populated in practice / qual
    class Timings : AbstractEvent
    {
        public static String folderGapInFrontIncreasing = "timings/gap_in_front_increasing";
        public static String folderGapInFrontDecreasing = "timings/gap_in_front_decreasing";

        public static String folderGapBehindIncreasing = "timings/gap_behind_increasing";
        public static String folderGapBehindDecreasing = "timings/gap_behind_decreasing";

        // for when we have a driver name...
        public static String folderTheGapTo = "timings/the_gap_to";   // "the gap to..."
        public static String folderAheadIsIncreasing = "timings/ahead_is_increasing"; // [bob] "ahead is increasing, it's now..."
        public static String folderBehindIsIncreasing = "timings/behind_is_increasing"; // [bob] "behind is increasing, it's now..."

        public static String folderYoureReeling = "timings/youre_reeling";    // "you're reeling..."
        public static String folderInTheGapIsNow = "timings/in_the_gap_is_now";  // [bob] "in, the gap is now..."

        public static String folderIsReelingYouIn = "timings/is_reeling_you_in";    // [bob] "is reeling you in, the gap is now...."

        public static String folderSeconds = "timings/seconds";

        private String folderBeingHeldUp = "timings/being_held_up";
        private String folderBeingPressured = "timings/being_pressured";

        private List<float> gapsInFront;

        private List<float> gapsBehind;

        private float gapBehindAtLastReport;

        private float gapInFrontAtLastReport;

        private int sectorsSinceLastGapReport;

        private int sectorsSinceLastCloseCarReport;

        private int sectorsUntilNextGapReport;

        private int sectorsUntilNextCloseCarReport;

        private Random rand = new Random();

        private float currentGapInFront;

        private float currentGapBehind;

        private Boolean enableGapMessages = UserSettings.GetUserSettings().getBoolean("enable_gap_messages");

        private Boolean isLeading;

        private Boolean isLast;

        private Boolean isRace;
        
        public Timings(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            gapsInFront = new List<float>();
            gapsBehind = new List<float>();            
            gapBehindAtLastReport = -1;
            gapInFrontAtLastReport = -1;
            sectorsSinceLastGapReport = 0;
            sectorsSinceLastCloseCarReport = 0;
            sectorsUntilNextGapReport = 0;
            sectorsUntilNextCloseCarReport = 0;
            currentGapBehind = -1;
            currentGapInFront = -1;
            isLast = false;
            isLeading = false;
            isRace = false;
        }

        protected override void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            isLeading = currentGameState.SessionData.Position == 1;
            isLast = currentGameState.isLast();
            isRace = currentGameState.SessionData.SessionType == SessionType.Race;
            currentGapInFront = currentGameState.SessionData.TimeDeltaFront;
            currentGapBehind = currentGameState.SessionData.TimeDeltaBehind;

            if (gapsInFront == null || gapsBehind == null)
            {
                clearState();
            }
            if (!currentGameState.SessionData.IsRacingSameCarInFront)
            {
                gapsInFront.Clear();
            }
            if (!currentGameState.SessionData.IsRacingSameCarBehind)
            {
                gapsBehind.Clear();
            }
            if (enableGapMessages && currentGameState.SessionData.IsNewSector && 
                !currentGameState.PitData.InPitlane)
            {
                sectorsSinceLastGapReport++;    
                sectorsSinceLastCloseCarReport++;
                GapStatus gapInFrontStatus = GapStatus.NONE;
                GapStatus gapBehindStatus = GapStatus.NONE;
                if (currentGameState.SessionData.Position != 1)
                {
                    gapsInFront.Insert(0, currentGameState.SessionData.TimeDeltaFront);
                    gapInFrontStatus = getGapStatus(gapsInFront, gapInFrontAtLastReport);
                }
                if (!isLast)
                {
                    gapsBehind.Insert(0, currentGameState.SessionData.TimeDeltaBehind);
                    gapBehindStatus = getGapStatus(gapsBehind, gapBehindAtLastReport);
                }

                if (isRace)
                {
                    // Play which ever is the smaller gap, but we're not interested if the gap is < 0.5 or > 20 seconds or hasn't changed:
                    Boolean playGapInFront = gapInFrontStatus != GapStatus.NONE &&
                        (gapBehindStatus == GapStatus.NONE || (gapsInFront.Count() > 0 && gapsBehind.Count() > 0 && gapsInFront[0] < gapsBehind[0]));

                    Boolean playGapBehind = !playGapInFront && gapBehindStatus != GapStatus.NONE;
                    if (playGapInFront)
                    {
                        if (gapInFrontStatus == GapStatus.CLOSE)
                        {
                            if (sectorsSinceLastCloseCarReport >= sectorsUntilNextCloseCarReport)
                            {
                                sectorsSinceLastCloseCarReport = 0;
                                sectorsUntilNextCloseCarReport = rand.Next(5, 7);
                                audioPlayer.queueClip(new QueuedMessage(folderBeingHeldUp, 0, this));
                                gapInFrontAtLastReport = gapsInFront[0];
                            }
                        }
                        else if (gapInFrontStatus != GapStatus.NONE && sectorsSinceLastGapReport >= sectorsUntilNextGapReport)
                        {
                            sectorsSinceLastGapReport = 0;
                            sectorsUntilNextGapReport = rand.Next(2, 4);
                            TimeSpan gapInFront = TimeSpan.FromMilliseconds(gapsInFront[0] * 1000);
                            Boolean readGap = gapInFront.Seconds > 0 || gapInFront.Milliseconds > 50;
                            if (readGap)
                            {
                                if (gapInFrontStatus == GapStatus.INCREASING)
                                {
                                    audioPlayer.queueClip(new QueuedMessage("Timings/gap_in_front",
                                        MessageContents(folderTheGapTo, currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1), folderAheadIsIncreasing,
                                        gapInFront, folderSeconds), MessageContents(folderGapInFrontIncreasing, gapInFront, folderSeconds), 0, this));
                                }
                                else if (gapInFrontStatus == GapStatus.DECREASING)
                                {
                                    audioPlayer.queueClip(new QueuedMessage("Timings/gap_in_front",
                                        MessageContents(folderYoureReeling, currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1),
                                        folderInTheGapIsNow, gapInFront, folderSeconds), MessageContents(folderGapInFrontDecreasing, gapInFront, folderSeconds), 0, this));
                                }
                            }
                            gapInFrontAtLastReport = gapsInFront[0];
                        }
                    }
                    else if (playGapBehind)
                    {
                        if (gapBehindStatus == GapStatus.CLOSE)
                        {
                            if (sectorsSinceLastCloseCarReport >= sectorsUntilNextCloseCarReport)
                            {
                                sectorsSinceLastCloseCarReport = 0;
                                sectorsUntilNextCloseCarReport = rand.Next(5, 7);
                                audioPlayer.queueClip(new QueuedMessage(folderBeingPressured, 0, this));
                                gapBehindAtLastReport = gapsInFront[0];
                            }
                        }
                        else if (gapBehindStatus != GapStatus.NONE && sectorsSinceLastGapReport >= sectorsUntilNextGapReport)
                        {
                            sectorsSinceLastGapReport = 0;
                            sectorsUntilNextGapReport = rand.Next(2, 4);
                            TimeSpan gapBehind = TimeSpan.FromMilliseconds(gapsBehind[0] * 1000);
                            Boolean readGap = gapBehind.Seconds > 0 || gapBehind.Milliseconds > 50;
                            if (readGap)
                            {
                                if (gapBehindStatus == GapStatus.INCREASING)
                                {
                                    audioPlayer.queueClip(new QueuedMessage("Timings/gap_behind",
                                       MessageContents(folderTheGapTo, currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position + 1),
                                       folderBehindIsIncreasing, gapBehind, folderSeconds), MessageContents(folderGapBehindIncreasing, gapBehind, folderSeconds), 0, this));
                                }
                                else if (gapBehindStatus == GapStatus.DECREASING)
                                {
                                    audioPlayer.queueClip(new QueuedMessage("Timings/gap_behind",
                                        MessageContents(currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position + 1), folderIsReelingYouIn, gapBehind, folderSeconds),
                                        MessageContents(folderGapBehindDecreasing, gapBehind, folderSeconds), 0, this));
                                }
                            }
                            gapInFrontAtLastReport = gapsInFront[0];
                        }
                    }
                }
            }
        }

        private GapStatus getGapStatus(List<float> gaps, float lastReportedGap)
        {
            // if we have less than 3 gaps in the list, or the last gap is too big, or the change in the gap is too big,
            // we don't want to report anything

            // when comparing gaps round to 1 decimal place
            if (gaps.Count < 3 || gaps[0] <= 0 || gaps[1] <= 0 || gaps[2] <= 0 || gaps[0] > 20 || Math.Abs(gaps[0] - gaps[1]) > 5)
            {
                return GapStatus.NONE;
            }
            else if (gaps[0] < 0.5 && gaps[1] < 0.5)
            {
                // this car has been close for 2 sectors
                return GapStatus.CLOSE;
            }
            if ((lastReportedGap == -1 || Math.Round(gaps[0], 1) > Math.Round(lastReportedGap)) &&
                Math.Round(gaps[0], 1) > Math.Round(gaps[1], 1) && Math.Round(gaps[1], 1) > Math.Round(gaps[2], 1))
            {
                return GapStatus.INCREASING;
            }
            else if ((lastReportedGap == -1 || Math.Round(gaps[0], 1) < Math.Round(lastReportedGap)) &&
                Math.Round(gaps[0], 1) < Math.Round(gaps[1], 1) && Math.Round(gaps[1], 1) < Math.Round(gaps[2], 1))
            {
                return GapStatus.DECREASING;
            }
            else
            {
                return GapStatus.NONE;
            }
        }

        public override void respond(String voiceMessage)
        {
            Boolean haveData = false;
            if ((voiceMessage.Contains(SpeechRecogniser.GAP_IN_FRONT) || 
                voiceMessage.Contains(SpeechRecogniser.GAP_AHEAD)) &&
                currentGapInFront != -1)
            {
                if (isLeading && isRace)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(Position.folderLeading, 0, this));
                    audioPlayer.closeChannel();
                    haveData = true;
                }
                else if (currentGapInFront < 60)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage("Timings/gaps",
                        MessageContents(TimeSpan.FromMilliseconds(currentGapInFront * 1000), folderSeconds), 0, this));
                    audioPlayer.closeChannel();
                    haveData = true;
                }
                else
                {
                    Console.WriteLine("Unable to read gap as it's more than 59 seconds");
                }
            }
            else if (voiceMessage.Contains(SpeechRecogniser.GAP_BEHIND) &&
                currentGapBehind != -1)
            {
                if (isLast && isRace)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage(Position.folderLast, 0, this));
                    audioPlayer.closeChannel();
                    haveData = true;
                }
                else if (currentGapBehind < 60)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(new QueuedMessage("Timings/gaps",
                        MessageContents(TimeSpan.FromMilliseconds(currentGapBehind * 1000), folderSeconds), 0, this));
                    audioPlayer.closeChannel();
                    haveData = true;
                }
                else
                {
                    Console.WriteLine("Unable to read gap as it's more than 59 seconds");
                }
            }
            if (!haveData)
            {
                audioPlayer.openChannel();
                audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, this));
                audioPlayer.closeChannel();
            }
        }

        private enum GapStatus
        {
            CLOSE, INCREASING, DECREASING, NONE
        }

        private float getOpponentBestLap(List<float> opponentLapTimes, int lapsToCheck)
        {
            if (opponentLapTimes == null && opponentLapTimes.Count == 0)
            {
                return -1;
            }
            float bestLap = opponentLapTimes[opponentLapTimes.Count - 1];
            int minIndex = opponentLapTimes.Count - lapsToCheck;
            for (int i = opponentLapTimes.Count - 1; i >= minIndex; i--)
            {
                if (opponentLapTimes[i] > 0 && opponentLapTimes[i] < bestLap)
                {
                    bestLap = opponentLapTimes[i];
                }
            }
            return bestLap;
        }
    }
}
