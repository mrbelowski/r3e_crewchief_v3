﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    class Opponents : AbstractEvent
    {
        private static String validationDriverAheadKey = "validationDriverAheadKey";
        public static String folderLeaderIsPitting = "opponents/the_leader_is_pitting";
        public static String folderCarAheadIsPitting = "opponents/the_car_ahead_is_pitting";
        public static String folderCarBehindIsPitting = "opponents/the_car_behind_is_pitting";

        public static String folderTheLeader = "opponents/the_leader";
        public static String folderIsPitting = "opponents/is_pitting";
        public static String folderAheadIsPitting = "opponents/ahead_is_pitting";
        public static String folderBehindIsPitting = "opponents/behind_is_pitting";

        public static String folderLeaderHasJustDoneA = "opponents/the_leader_has_just_done_a";
        public static String folderTheCarAheadHasJustDoneA = "opponents/the_car_ahead_has_just_done_a";
        public static String folderTheCarBehindHasJustDoneA = "opponents/the_car_behind_has_just_done_a";
        public static String folderNewFastestLapFor = "opponents/new_fastest_lap_for";

        public static String folderIsNowLeading = "opponents/is_now_leading";
        public static String folderNextCarIs = "opponents/next_car_is";

        public static String folderCantPronounceName = "opponents/cant_pronounce_name";

        public static String folderWeAre = "opponents/we_are";

        private int frequencyOfOpponentRaceLapTimes = UserSettings.GetUserSettings().getInt("frequency_of_opponent_race_lap_times");
        private int frequencyOfOpponentPracticeAndQualLapTimes = UserSettings.GetUserSettings().getInt("frequency_of_opponent_practice_and_qual_lap_times");
        private float minImprovementBeforeReadingOpponentRaceTime;
        private float maxOffPaceBeforeReadingOpponentRaceTime;

        private GameStateData currentGameState;

        private DateTime nextLeadChangeMessage = DateTime.MinValue;

        private DateTime nextCarAheadChangeMessage = DateTime.MinValue;

        private Object positionIsPlayerKey = new Object();

        private Random random = new Random();

        public Opponents(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
            maxOffPaceBeforeReadingOpponentRaceTime = (float)frequencyOfOpponentRaceLapTimes / 10f;
            minImprovementBeforeReadingOpponentRaceTime = (1f - maxOffPaceBeforeReadingOpponentRaceTime) / 5f;
        }

        public override void clearState()
        {
            currentGameState = null;
            nextLeadChangeMessage = DateTime.MinValue;
            nextCarAheadChangeMessage = DateTime.MinValue;
        }

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            if (validationData != null && validationData.ContainsKey(validationDriverAheadKey)) {
                String expectedOpponentName = (String)validationData[validationDriverAheadKey];
                OpponentData opponentInFront = currentGameState.SessionData.Position > 1 ? currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1, false) : null;
                String actualOpponentName = opponentInFront == null ? null : opponentInFront.DriverRawName;
                if (actualOpponentName != expectedOpponentName)
                {
                    if (actualOpponentName != null && expectedOpponentName != null)
                    {
                        Console.WriteLine("new car in front message for opponent " + expectedOpponentName +
                            " no longer valid - driver in front is now " + actualOpponentName);
                    }
                    return false;
                }
            }
            return true;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            this.currentGameState = currentGameState;
            if (nextCarAheadChangeMessage == DateTime.MinValue)
            {
                nextCarAheadChangeMessage = currentGameState.Now.Add(TimeSpan.FromSeconds(30));
            }
            if (nextLeadChangeMessage == DateTime.MinValue)
            {
                nextLeadChangeMessage = currentGameState.Now.Add(TimeSpan.FromSeconds(30));
            }
            if (currentGameState.SessionData.SessionType != SessionType.Race || frequencyOfOpponentRaceLapTimes > 0)
            {
                foreach (KeyValuePair<Object, OpponentData> entry in currentGameState.OpponentData)
                {
                    Object opponentKey = entry.Key;
                    OpponentData opponentData = entry.Value;

                    if (opponentData.IsNewLap && opponentData.LastLapTime > 0 && opponentData.OpponentLapData.Count > 1 &&
                        opponentData.LastLapValid && opponentData.CurrentBestLapTime != -1)
                    {
                        float currentFastestLap;
                        if (currentGameState.SessionData.PlayerLapTimeSessionBest == -1)
                        {
                            currentFastestLap = currentGameState.SessionData.OpponentsLapTimeSessionBestOverall;
                        }
                        else if (currentGameState.SessionData.OpponentsLapTimeSessionBestOverall == -1)
                        {
                            currentFastestLap = currentGameState.SessionData.PlayerLapTimeSessionBest;
                        }
                        else
                        {
                            currentFastestLap = Math.Min(currentGameState.SessionData.PlayerLapTimeSessionBest, currentGameState.SessionData.OpponentsLapTimeSessionBestOverall);
                        }

                        // this opponent has just completed a lap - do we need to report it? if it's fast overall and more than
                        // a tenth quicker then his previous best we do...
                        if (((currentGameState.SessionData.SessionType == SessionType.Race && opponentData.CompletedLaps > 2) ||
                            (currentGameState.SessionData.SessionType != SessionType.Race && opponentData.CompletedLaps > 1)) && opponentData.LastLapTime <= currentFastestLap &&
                            AudioPlayer.availableDriverNames.Contains(DriverNameHelper.getUsableNameForRawName(opponentData.DriverRawName)))
                        {
                            audioPlayer.queueClip(new QueuedMessage("new_fastest_lap", MessageContents(folderNewFastestLapFor, opponentData,
                                        TimeSpan.FromSeconds(opponentData.LastLapTime)), 0, this));
                        }
                        else if ((currentGameState.SessionData.SessionType == SessionType.Race &&
                                (opponentData.LastLapTime <= opponentData.CurrentBestLapTime &&
                                 opponentData.LastLapTime < opponentData.PreviousBestLapTime - minImprovementBeforeReadingOpponentRaceTime &&
                                 opponentData.LastLapTime < currentFastestLap + maxOffPaceBeforeReadingOpponentRaceTime)) ||
                           ((currentGameState.SessionData.SessionType == SessionType.Practice || currentGameState.SessionData.SessionType == SessionType.Qualify) &&
                                 opponentData.LastLapTime <= opponentData.CurrentBestLapTime))
                        {
                            if (currentGameState.SessionData.UnFilteredPosition > 1 && opponentData.UnFilteredPosition == 1 &&
                                (currentGameState.SessionData.SessionType == SessionType.Race || frequencyOfOpponentPracticeAndQualLapTimes > 0))
                            {
                                // he's leading, and has recorded 3 or more laps, and this one's his fastest
                                Console.WriteLine("Leader fast lap - this lap time = " + opponentData.LastLapTime +" session best = " + currentFastestLap);
                                audioPlayer.queueClip(new QueuedMessage("leader_good_laptime", MessageContents(folderLeaderHasJustDoneA,
                                        TimeSpan.FromSeconds(opponentData.LastLapTime)), 0, this));
                            }
                            else if (currentGameState.SessionData.UnFilteredPosition > 1 && opponentData.UnFilteredPosition == currentGameState.SessionData.Position - 1 &&
                                (currentGameState.SessionData.SessionType == SessionType.Race || random.Next(10) < frequencyOfOpponentPracticeAndQualLapTimes))
                            {
                                // he's ahead of us, and has recorded 3 or more laps, and this one's his fastest
                                Console.WriteLine("Car ahead fast lap - this lap time = " + opponentData.LastLapTime + " session best = " + currentFastestLap);
                                 audioPlayer.queueClip(new QueuedMessage("car_ahead_good_laptime", MessageContents(folderTheCarAheadHasJustDoneA,
                                        TimeSpan.FromSeconds(opponentData.LastLapTime)), 0, this));
                            }
                            else if (!currentGameState.isLast() && opponentData.UnFilteredPosition == currentGameState.SessionData.Position + 1 &&
                                (currentGameState.SessionData.SessionType == SessionType.Race || random.Next(10) < frequencyOfOpponentPracticeAndQualLapTimes))
                            {
                                // he's behind us, and has recorded 3 or more laps, and this one's his fastest
                                Console.WriteLine("Car behind fast lap - this lap time = " + opponentData.LastLapTime + " session best = " + currentFastestLap);
                                audioPlayer.queueClip(new QueuedMessage("car_behind_good_laptime", MessageContents(folderTheCarBehindHasJustDoneA,
                                        TimeSpan.FromSeconds(opponentData.LastLapTime)), 0, this));
                            }
                        }
                    }
                }
            }

            if (currentGameState.SessionData.SessionType == SessionType.Race)
            {
                if (!currentGameState.SessionData.IsRacingSameCarInFront)
                {
                    if (currentGameState.SessionData.Position > 2 && currentGameState.Now > nextCarAheadChangeMessage && !currentGameState.PitData.InPitlane
                        && currentGameState.SessionData.CompletedLaps > 0)
                    {
                        OpponentData opponentData = currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1, false);
                        if (opponentData != null && !opponentData.isEnteringPits())
                        {
                            audioPlayer.queueClip(new QueuedMessage("new_car_ahead", MessageContents(folderNextCarIs, opponentData),
                                random.Next(Position.secondsToWaitBeforeReportingPass + 3, Position.secondsToWaitBeforeReportingPass + 6), this,
                                new Dictionary<string, object> { { validationDriverAheadKey, opponentData.DriverRawName } }));
                            nextCarAheadChangeMessage = currentGameState.Now.Add(TimeSpan.FromSeconds(30));
                        }                        
                    }
                }
                if (currentGameState.SessionData.HasLeadChanged)
                {
                    OpponentData leader = currentGameState.getOpponentAtPosition(1, false);
                    if (leader != null)
                    {
                        String name = leader.DriverRawName;
                        if (currentGameState.SessionData.Position > 1 && previousGameState.SessionData.Position > 1 && currentGameState.Now > nextLeadChangeMessage)
                        {
                            Console.WriteLine("Lead change, current leader is " + name + " laps completed = " + currentGameState.SessionData.CompletedLaps);
                            audioPlayer.queueClip(new QueuedMessage("new_leader", MessageContents(currentGameState.getOpponentAtPosition(1, false), folderIsNowLeading), 0, this));
                            nextLeadChangeMessage = currentGameState.Now.Add(TimeSpan.FromSeconds(30));
                        }
                    }                    
                }

                if (currentGameState.PitData.LeaderIsPitting && 
                    currentGameState.SessionData.SessionPhase != SessionPhase.Countdown && currentGameState.SessionData.SessionPhase != SessionPhase.Formation)
                {
                    audioPlayer.queueClip(new QueuedMessage("leader_is_pitting", MessageContents(folderTheLeader, currentGameState.PitData.OpponentForLeaderPitting, 
                        folderIsPitting), MessageContents(folderLeaderIsPitting), 0, this));
                }

                if (currentGameState.PitData.CarInFrontIsPitting && currentGameState.SessionData.TimeDeltaFront > 3 &&
                    currentGameState.SessionData.SessionPhase != SessionPhase.Countdown && currentGameState.SessionData.SessionPhase != SessionPhase.Formation)
                {
                    audioPlayer.queueClip(new QueuedMessage("car_in_front_is_pitting", MessageContents(currentGameState.PitData.OpponentForCarAheadPitting, 
                        folderAheadIsPitting), MessageContents(folderCarAheadIsPitting), 0, this));
                }

                if (currentGameState.PitData.CarBehindIsPitting && currentGameState.SessionData.TimeDeltaBehind > 3 &&
                    currentGameState.SessionData.SessionPhase != SessionPhase.Countdown && currentGameState.SessionData.SessionPhase != SessionPhase.Formation)
                {
                    audioPlayer.queueClip(new QueuedMessage("car_behind_is_pitting", MessageContents(currentGameState.PitData.OpponentForCarBehindPitting,
                        folderBehindIsPitting), MessageContents(folderCarBehindIsPitting), 0, this));
                }
            }
        }

        private Object getOpponentKey(String voiceMessage, String expectedNumberSuffix)
        {
            Object opponentKey = null;
            if (voiceMessage.Contains(SpeechRecogniser.THE_LEADER))
            {
                if (currentGameState.SessionData.Position > 1)
                {
                    opponentKey = currentGameState.getOpponentKeyAtPosition(1, false);
                }
                else if (currentGameState.SessionData.Position == 1)
                {
                    opponentKey = positionIsPlayerKey;
                }
            }
            if ((voiceMessage.Contains(SpeechRecogniser.THE_CAR_AHEAD) || voiceMessage.Contains(SpeechRecogniser.THE_GUY_AHEAD) ||
                voiceMessage.Contains(SpeechRecogniser.THE_GUY_IN_FRONT) || voiceMessage.Contains(SpeechRecogniser.THE_CAR_IN_FRONT)) && currentGameState.SessionData.Position > 1)
            {
                opponentKey = currentGameState.getOpponentKeyInFront(false);
            }
            else if ((voiceMessage.Contains(SpeechRecogniser.THE_CAR_BEHIND) || voiceMessage.Contains(SpeechRecogniser.THE_GUY_BEHIND)) &&
                            !currentGameState.isLast())
            {
                opponentKey = currentGameState.getOpponentKeyBehind(false);
            }
            else if (voiceMessage.Contains(SpeechRecogniser.POSITION) || voiceMessage.Contains(SpeechRecogniser.PEA))
            {
                int position = 0;
                foreach (KeyValuePair<String, int> entry in SpeechRecogniser.numberToNumber)
                {
                    if (expectedNumberSuffix.Length > 0)
                    {
                        if (voiceMessage.Contains(" " + entry.Key + expectedNumberSuffix))
                        {
                            position = entry.Value;
                            break;
                        }
                    }
                    else
                    {
                        if (voiceMessage.EndsWith(" " + entry.Key))
                        {
                            position = entry.Value;
                            break;
                        }
                    }
                }
                if (position != currentGameState.SessionData.Position)
                {
                    opponentKey = currentGameState.getOpponentKeyAtPosition(position, false);
                }
                else
                {
                    opponentKey = positionIsPlayerKey;
                }
            }
            else
            {
                foreach (KeyValuePair<Object, OpponentData> entry in currentGameState.OpponentData)
                {
                    String usableDriverName = DriverNameHelper.getUsableNameForRawName(entry.Value.DriverRawName);
                    if (voiceMessage.Contains(usableDriverName))
                    {
                        opponentKey = entry.Key;
                        break;
                    }
                }
            }
            return opponentKey;
        }

        private float getOpponentLastLap(Object opponentKey)
        {
            if (opponentKey != null && currentGameState.OpponentData.ContainsKey(opponentKey))
            {
                return currentGameState.OpponentData[opponentKey].LastLapTime;
            }
            return -1;
        }

        private float getOpponentBestLap(Object opponentKey)
        {
            if (opponentKey != null && currentGameState.OpponentData.ContainsKey(opponentKey))
            {
                return currentGameState.OpponentData[opponentKey].CurrentBestLapTime;
            }
            return -1;
        }
        
        public override void respond(String voiceMessage)
        {
            Boolean gotData = false;
            if (currentGameState != null)
            {
                if (voiceMessage.StartsWith(SpeechRecogniser.WHAT_TYRE_IS) || voiceMessage.StartsWith(SpeechRecogniser.WHAT_TYRES_IS))
                {
                    Object opponentKey = getOpponentKey(voiceMessage, " on");
                    if (opponentKey != null)
                    {
                        OpponentData opponentData = currentGameState.OpponentData[opponentKey];
                        if (opponentData.CurrentTyres == TyreType.Option)
                        {
                            gotData = true;
                            audioPlayer.playClipImmediately(new QueuedMessage(MandatoryPitStops.folderMandatoryPitStopsOptionTyres, 0, null), false);
                            audioPlayer.closeChannel();
                        }
                        else if (opponentData.CurrentTyres == TyreType.Prime)
                        {
                            gotData = true;
                            audioPlayer.playClipImmediately(new QueuedMessage(MandatoryPitStops.folderMandatoryPitStopsPrimeTyres, 0, null), false);
                            audioPlayer.closeChannel();
                        }
                    }
                }

                if (voiceMessage.StartsWith(SpeechRecogniser.WHATS) && 
                    (voiceMessage.EndsWith(SpeechRecogniser.LAST_LAP) || voiceMessage.EndsWith(SpeechRecogniser.BEST_LAP)))
                {
                    if (voiceMessage.EndsWith(SpeechRecogniser.LAST_LAP))
                    {
                        float lastLap = getOpponentLastLap(getOpponentKey(voiceMessage, "'s "));
                        if (lastLap != -1)
                        {
                            gotData = true;
                            audioPlayer.playClipImmediately(new QueuedMessage("opponentLastLap", MessageContents(
                                TimeSpanWrapper.FromSeconds(lastLap, true)), 0, null), false);
                            audioPlayer.closeChannel();
                        }                       
                    }
                    else
                    {
                        float bestLap = getOpponentBestLap(getOpponentKey(voiceMessage, "'s "));
                        if (bestLap != -1)
                        {
                            gotData = true;
                            audioPlayer.playClipImmediately(new QueuedMessage("opponentBestLap", MessageContents(
                                TimeSpanWrapper.FromSeconds(bestLap, true)), 0, null), false);
                            audioPlayer.closeChannel();
                        }
                    }  
                } 
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHERE_IS))
                {
                    foreach (KeyValuePair<Object, OpponentData> entry in currentGameState.OpponentData)
                    {
                        String usableDriverName = DriverNameHelper.getUsableNameForRawName(entry.Value.DriverRawName);
                        if (voiceMessage.Contains(usableDriverName))
                        {
                            Console.WriteLine("Got opponent name, raw name = " + entry.Value.DriverRawName + ", using " + usableDriverName);
                            if (entry.Value.IsActive)
                            {
                                int position = entry.Value.Position;
                                OpponentData.OpponentDelta opponentDelta = entry.Value.getTimeDifferenceToPlayer(currentGameState.SessionData);
                                audioPlayer.playClipImmediately(new QueuedMessage("opponentPosition", 
                                    MessageContents(Position.folderStub, QueuedMessage.folderNameNumbersStub + position), 0, null), false);
                                if (opponentDelta != null && (opponentDelta.lapDifference != 0 || Math.Abs(opponentDelta.time) > 0.05))
                                {
                                    if (opponentDelta.lapDifference == 1)
                                    {
                                        audioPlayer.playClipImmediately(new QueuedMessage(Position.folderOneLapBehind, 0, null), false);
                                    }
                                    else if (opponentDelta.lapDifference > 1)
                                    {
                                        audioPlayer.playClipImmediately(new QueuedMessage("opponentTimeDelta",
                                            MessageContents(QueuedMessage.folderNameNumbersStub + opponentDelta.lapDifference, Position.folderLapsBehind), 0, null), false);
                                    }
                                    else if (opponentDelta.lapDifference == -1)
                                    {
                                        audioPlayer.playClipImmediately(new QueuedMessage(Position.folderOneLapAhead, 0, null), false);
                                    }
                                    else if (opponentDelta.lapDifference < -1)
                                    {
                                        audioPlayer.playClipImmediately(new QueuedMessage("opponentTimeDelta",
                                            MessageContents(QueuedMessage.folderNameNumbersStub + opponentDelta.lapDifference, Position.folderLapsAhead), 0, null), false);
                                    }
                                    else
                                    {
                                        TimeSpanWrapper delta = TimeSpanWrapper.FromSeconds(Math.Abs(opponentDelta.time), true);
                                        String aheadOrBehind = Position.folderAhead;
                                        if (opponentDelta.time < 0)
                                        {
                                            aheadOrBehind = Position.folderBehind;
                                        }
                                        audioPlayer.playClipImmediately(new QueuedMessage("opponentTimeDelta",
                                            MessageContents(delta, aheadOrBehind), 0, null), false);
                                    }                                    
                                }
                                audioPlayer.closeChannel();
                                gotData = true;
                            }
                            else
                            {
                                Console.WriteLine("Driver "+ entry.Value.DriverRawName + " is no longer active in this session");
                            }                 
                            break;
                        }
                    }
                }
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHOS_BEHIND_ON_TRACK))
                {
                    Object opponentKey = currentGameState.getOpponentKeyBehindOnTrack();
                    if (opponentKey != null)
                    {
                        OpponentData opponent = currentGameState.OpponentData[opponentKey];
                        QueuedMessage queuedMessage = new QueuedMessage("opponentNameAndPosition", MessageContents(currentGameState.OpponentData[opponentKey],
                            Position.folderStub, QueuedMessage.folderNameNumbersStub + opponent.Position),
                            MessageContents(Position.folderStub, QueuedMessage.folderNameNumbersStub + opponent.Position, folderCantPronounceName), 0, null);
                        if (queuedMessage.canBePlayed)
                        {
                            audioPlayer.playClipImmediately(queuedMessage, false);
                            audioPlayer.closeChannel();
                            gotData = true;
                        }
                    }
                }
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHOS_IN_FRONT_ON_TRACK) || voiceMessage.StartsWith(SpeechRecogniser.WHOS_AHEAD_ON_TRACK))
                {
                    Object opponentKey = currentGameState.getOpponentKeyInFrontOnTrack();
                    if (opponentKey != null)
                    {
                        OpponentData opponent = currentGameState.OpponentData[opponentKey];
                        QueuedMessage queuedMessage = new QueuedMessage("opponentName", MessageContents(currentGameState.OpponentData[opponentKey],
                            Position.folderStub, QueuedMessage.folderNameNumbersStub + opponent.Position),
                            MessageContents(Position.folderStub, QueuedMessage.folderNameNumbersStub + opponent.Position, folderCantPronounceName), 0, null);
                        if (queuedMessage.canBePlayed)
                        {
                            audioPlayer.playClipImmediately(queuedMessage, false);
                            audioPlayer.closeChannel();
                            gotData = true;
                        }
                    }
                }
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHOS_BEHIND_IN_THE_RACE) || voiceMessage.StartsWith(SpeechRecogniser.WHOS_BEHIND))
                {
                    if (currentGameState.isLast())
                    {
                        audioPlayer.playClipImmediately(new QueuedMessage(Position.folderLast, 0, null), false);
                        audioPlayer.closeChannel();
                        gotData = true;
                    } 
                    else 
                    {
                        OpponentData opponent = currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position + 1, false);
                        if (opponent != null)
                        {
                            QueuedMessage queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent), MessageContents(folderCantPronounceName), 0, null);
                            if (queuedMessage.canBePlayed)
                            {
                                audioPlayer.playClipImmediately(queuedMessage, false);
                                audioPlayer.closeChannel();
                                gotData = true;
                            }
                        }
                    }
                }
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHOS_IN_FRONT_IN_THE_RACE) || voiceMessage.StartsWith(SpeechRecogniser.WHOS_AHEAD_IN_THE_RACE)
                    || voiceMessage.StartsWith(SpeechRecogniser.WHOS_IN_FRONT) || voiceMessage.StartsWith(SpeechRecogniser.WHOS_AHEAD))
                {
                    if (currentGameState.SessionData.Position == 1)
                    {
                        audioPlayer.playClipImmediately(new QueuedMessage(Position.folderLeading, 0, null), false);
                        audioPlayer.closeChannel();
                        gotData = true;
                    }
                    else
                    {
                        OpponentData opponent = currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1, false);
                        if (opponent != null)
                        {
                            QueuedMessage queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent), MessageContents(folderCantPronounceName), 0, null);
                            if (queuedMessage.canBePlayed)
                            {
                                audioPlayer.playClipImmediately(queuedMessage, false);
                                audioPlayer.closeChannel();
                                gotData = true;
                            }
                        }
                    }
                }                
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHOS_LEADING) && currentGameState.SessionData.Position > 1)
                {
                    OpponentData opponent = currentGameState.getOpponentAtPosition(1, false);
                    if (opponent != null)
                    {
                        QueuedMessage queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent), MessageContents(folderCantPronounceName), 0, null);
                        if (queuedMessage.canBePlayed)
                        {
                            audioPlayer.playClipImmediately(queuedMessage, false);
                            audioPlayer.closeChannel();
                            gotData = true;
                        }
                    }
                }
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHOS_IN))
                {
                    Object opponentKey = getOpponentKey(voiceMessage, "");
                    if (opponentKey != null)
                    {
                        if (opponentKey == positionIsPlayerKey)
                        {
                            audioPlayer.playClipImmediately(new QueuedMessage(folderWeAre, 0, null), false);
                            audioPlayer.closeChannel();
                            gotData = true;
                        }
                        else if (currentGameState.OpponentData.ContainsKey(opponentKey))
                        {
                            OpponentData opponent = currentGameState.OpponentData[opponentKey];
                            QueuedMessage queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent), MessageContents(folderCantPronounceName), 0, null);
                            if (queuedMessage.canBePlayed)
                            {
                                audioPlayer.playClipImmediately(queuedMessage, false);
                                audioPlayer.closeChannel();
                                gotData = true;
                            }
                        }
                    }
                }
            }
            if (!gotData)
            {
                audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                audioPlayer.closeChannel();
            }       
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
