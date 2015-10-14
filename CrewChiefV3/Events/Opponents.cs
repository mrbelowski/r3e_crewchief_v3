using System;
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

        public static String folderIsNowLeading = "opponents/is_now_leading";
        public static String folderNextCarIs = "opponents/next_car_is";

        private Dictionary<Object, List<float>> opponentLapTimes = new Dictionary<Object, List<float>>();

        private GameStateData currentGameState;

        private DateTime nextLeadChangeMessage = DateTime.MinValue;

        private DateTime nextCarAheadChangeMessage = DateTime.MinValue;

        public Opponents(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            currentGameState = null;
            opponentLapTimes.Clear();
            nextLeadChangeMessage = DateTime.MinValue;
            nextCarAheadChangeMessage = DateTime.MinValue;
        }

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            if (validationData != null && validationData.ContainsKey(validationDriverAheadKey)) {
                String expectedOpponentName = (String)validationData[validationDriverAheadKey];
                OpponentData opponentInFront = currentGameState.SessionData.Position > 1 ? currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1) : null;
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
            foreach (KeyValuePair<Object, OpponentData> entry in currentGameState.OpponentData)
            {
                Object opponentKey = entry.Key;
                OpponentData opponentData = entry.Value;
                if (!opponentLapTimes.ContainsKey(opponentKey))
                {
                    opponentLapTimes.Add(opponentKey, new List<float>());
                }
                if (opponentData.IsNewLap && opponentData.ApproximateLastLapTime > 0)
                {
                    // this opponent has just completed a lap - do we need to report it?
                    if (opponentLapTimes[opponentKey].Count > 2 && opponentLapTimes[opponentKey].Min() > opponentData.ApproximateLastLapTime)
                    {
                        if (currentGameState.SessionData.Position > 1 && opponentData.Position == 1)
                        {
                            // he's leading, and has recorded 2 or more laps, and this one's his fastest
                            audioPlayer.queueClip(new QueuedMessage("leader_good_laptime", MessageContents(folderLeaderHasJustDoneA,
                                    TimeSpan.FromSeconds(opponentData.ApproximateLastLapTime)), 0, this));
                        }
                        else if (currentGameState.SessionData.Position > 1 && opponentData.Position == currentGameState.SessionData.Position - 1)
                        {
                            // he's ahead of us, and has recorded 2 or more laps, and this one's his fastest
                            audioPlayer.queueClip(new QueuedMessage("car_ahead_good_laptime", MessageContents(folderTheCarAheadHasJustDoneA,
                                    TimeSpan.FromSeconds(opponentData.ApproximateLastLapTime)), 0, this));
                        }
                        else if (!currentGameState.isLast() && opponentData.Position == currentGameState.SessionData.Position + 1)
                        {
                            // he's behind us, and has recorded 2 or more laps, and this one's his fastest
                            audioPlayer.queueClip(new QueuedMessage("car_behind_good_laptime", MessageContents(folderTheCarBehindHasJustDoneA,
                                    TimeSpan.FromSeconds(opponentData.ApproximateLastLapTime)), 0, this));
                        }
                    }
                    opponentLapTimes[opponentKey].Add(opponentData.ApproximateLastLapTime);
                }
            }

            if (currentGameState.SessionData.SessionType == SessionType.Race)
            {
                if (!currentGameState.SessionData.IsRacingSameCarInFront)
                {
                    if (currentGameState.SessionData.Position > 2 && currentGameState.Now > nextCarAheadChangeMessage && !currentGameState.PitData.InPitlane
                        && currentGameState.SessionData.CompletedLaps > 0)
                    {
                        OpponentData opponentData = currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1);
                        if (opponentData != null && !opponentData.IsEnteringPits)
                        {
                            audioPlayer.queueClip(new QueuedMessage("new_car_ahead", MessageContents(folderNextCarIs, opponentData), 4, this,
                                new Dictionary<string, object> { { validationDriverAheadKey, opponentData.DriverRawName } }));
                            nextCarAheadChangeMessage = currentGameState.Now.Add(TimeSpan.FromSeconds(30));
                        }                        
                    }
                }
                if (currentGameState.SessionData.HasLeadChanged)
                {
                    String name = currentGameState.getOpponentAtPosition(1) != null ? currentGameState.getOpponentAtPosition(1).DriverRawName : "no data";
                    Console.WriteLine("Lead change, current leader is " + name + " laps completed = " + currentGameState.SessionData.CompletedLaps);
                    if (currentGameState.SessionData.Position > 1 && previousGameState.SessionData.Position > 1 && currentGameState.Now > nextLeadChangeMessage)
                    {
                        audioPlayer.queueClip(new QueuedMessage("new_leader", MessageContents(currentGameState.getOpponentAtPosition(1), folderIsNowLeading), 0, this));
                        nextLeadChangeMessage = currentGameState.Now.Add(TimeSpan.FromSeconds(30));
                    }
                }

                if (currentGameState.PitData.LeaderIsPitting && 
                    currentGameState.SessionData.SessionPhase != SessionPhase.Countdown && currentGameState.SessionData.SessionPhase != SessionPhase.Formation)
                {
                    audioPlayer.queueClip(new QueuedMessage("leader_is_pitting", MessageContents(folderTheLeader, currentGameState.getOpponentAtPositionWhenStartingSector3(1), 
                        folderIsPitting), MessageContents(folderLeaderIsPitting), 0, this));
                }

                if (currentGameState.PitData.CarInFrontIsPitting && currentGameState.SessionData.TimeDeltaFront > 3 &&
                    currentGameState.SessionData.SessionPhase != SessionPhase.Countdown && currentGameState.SessionData.SessionPhase != SessionPhase.Formation)
                {
                    audioPlayer.queueClip(new QueuedMessage("car_in_front_is_pitting", MessageContents(currentGameState.getOpponentAtPositionWhenStartingSector3(
                        currentGameState.SessionData.Position - 1), folderAheadIsPitting), MessageContents(folderCarAheadIsPitting), 0, this));
                }

                if (currentGameState.PitData.CarBehindIsPitting && currentGameState.SessionData.TimeDeltaBehind > 3 &&
                    currentGameState.SessionData.SessionPhase != SessionPhase.Countdown && currentGameState.SessionData.SessionPhase != SessionPhase.Formation)
                {
                    audioPlayer.queueClip(new QueuedMessage("car_behind_is_pitting", MessageContents(currentGameState.getOpponentAtPositionWhenStartingSector3(
                        currentGameState.SessionData.Position + 1), folderBehindIsPitting), MessageContents(folderCarBehindIsPitting), 0, this));
                }
            }
        }

        public override void respond(String voiceMessage)
        {
            Boolean gotData = false;
            if (currentGameState != null)
            {
                if (voiceMessage.StartsWith(SpeechRecogniser.WHAT_WAS) && voiceMessage.EndsWith(SpeechRecogniser.LAST_LAP))
                {
                    foreach (KeyValuePair<Object, OpponentData> entry in currentGameState.OpponentData)
                    {
                        String usableDriverName = DriverNameHelper.getUsableNameForRawName(entry.Value.DriverRawName);
                        if (voiceMessage.Contains(usableDriverName))
                        {
                            if (opponentLapTimes.ContainsKey(entry.Key))
                            {
                                List<float> requestedOpponentLapTimes = opponentLapTimes[entry.Key];
                                if (requestedOpponentLapTimes.Count > 0)
                                {
                                    gotData = true;
                                    audioPlayer.playClipImmediately(new QueuedMessage("opponentLastLap", MessageContents(
                                            TimeSpanWrapper.FromSeconds(requestedOpponentLapTimes[requestedOpponentLapTimes.Count - 1], true)), 0, null), false);
                                        audioPlayer.closeChannel();
                                }
                            }
                            break;
                        }
                    }
                    if (!gotData)
                    {
                        if ((voiceMessage.Contains(SpeechRecogniser.THE_CAR_AHEAD) || voiceMessage.Contains(SpeechRecogniser.THE_GUY_AHEAD) ||
                            voiceMessage.Contains(SpeechRecogniser.THE_GUY_IN_FRONT) || voiceMessage.Contains(SpeechRecogniser.THE_CAR_IN_FRONT)) && currentGameState.SessionData.Position > 1)
                        {
                            Object opponentKey = currentGameState.getOpponentKeyInFront();
                            if (opponentKey != null && currentGameState.OpponentData.ContainsKey(opponentKey))
                            {
                                OpponentData opponent = currentGameState.OpponentData[opponentKey];
                                if (opponent != null && opponentLapTimes.ContainsKey(opponentKey))
                                {
                                    List<float> requestedOpponentLapTimes = opponentLapTimes[opponentKey];
                                    if (requestedOpponentLapTimes.Count > 0)
                                    {
                                        gotData = true;
                                        if (voiceMessage.Contains(SpeechRecogniser.BEST_LAP))
                                        {
                                            audioPlayer.playClipImmediately(new QueuedMessage("opponentLastLap", MessageContents(
                                                TimeSpanWrapper.FromSeconds(requestedOpponentLapTimes.Min(), true)), 0, null), false);
                                        }
                                        else if (voiceMessage.Contains(SpeechRecogniser.LAST_LAP))
                                        {
                                            audioPlayer.playClipImmediately(new QueuedMessage("opponentLastLap", MessageContents(
                                                TimeSpanWrapper.FromSeconds(requestedOpponentLapTimes[requestedOpponentLapTimes.Count - 1], true)), 0, null), false);
                                        }                                        
                                        audioPlayer.closeChannel();
                                    }
                                }
                            }
                        } 
                        else if ((voiceMessage.Contains(SpeechRecogniser.THE_CAR_BEHIND) || voiceMessage.Contains(SpeechRecogniser.THE_GUY_BEHIND)) && 
                            !currentGameState.isLast())
                        {
                            Object opponentKey = currentGameState.getOpponentKeyBehind();
                            if (opponentKey != null && currentGameState.OpponentData.ContainsKey(opponentKey))
                            {
                                OpponentData opponent = currentGameState.OpponentData[opponentKey];
                                if (opponent != null && opponentLapTimes.ContainsKey(opponentKey))
                                {
                                    List<float> requestedOpponentLapTimes = opponentLapTimes[opponentKey];
                                    if (requestedOpponentLapTimes.Count > 0)
                                    {
                                        gotData = true;
                                        if (voiceMessage.Contains(SpeechRecogniser.BEST_LAP))
                                        {
                                            audioPlayer.playClipImmediately(new QueuedMessage("opponentLastLap", MessageContents(
                                                TimeSpanWrapper.FromSeconds(requestedOpponentLapTimes.Min(), true)), 0, null), false);
                                        }
                                        else if (voiceMessage.Contains(SpeechRecogniser.LAST_LAP))
                                        {
                                            audioPlayer.playClipImmediately(new QueuedMessage("opponentLastLap", MessageContents(
                                                TimeSpanWrapper.FromSeconds(requestedOpponentLapTimes[requestedOpponentLapTimes.Count - 1], true)), 0, null), false);
                                        }                                        
                                        audioPlayer.closeChannel();
                                    }
                                }
                            }
                        }
                    }
                } 
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHAT_IS) && voiceMessage.EndsWith(SpeechRecogniser.BEST_LAP))
                {
                    foreach (KeyValuePair<Object, OpponentData> entry in currentGameState.OpponentData)
                    {
                        String usableDriverName = DriverNameHelper.getUsableNameForRawName(entry.Value.DriverRawName);
                        if (voiceMessage.Contains(usableDriverName))
                        {
                            if (opponentLapTimes.ContainsKey(entry.Key))
                            {
                                List<float> requestedOpponentLapTimes = opponentLapTimes[entry.Key];
                                if (requestedOpponentLapTimes.Count > 0)
                                {
                                    gotData = true;
                                    audioPlayer.playClipImmediately(new QueuedMessage("opponentBestLap", MessageContents(
                                            TimeSpanWrapper.FromSeconds(requestedOpponentLapTimes.Min(), true)), 0, null), false);
                                        audioPlayer.closeChannel();                                    
                                }
                            }
                            break;
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
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHOS_BEHIND) && !currentGameState.isLast())
                {
                    OpponentData opponent = currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position + 1);
                    if (opponent != null)
                    {
                        QueuedMessage queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent), 0, null);
                        if (queuedMessage.canBePlayed)
                        {
                            audioPlayer.playClipImmediately(queuedMessage, false);
                            audioPlayer.closeChannel();
                            gotData = true;
                        }                        
                    }
                }
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHOS_IN_FRONT) && currentGameState.SessionData.Position > 1)
                {
                    OpponentData opponent = currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1);
                    if (opponent != null)
                    {
                        QueuedMessage queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent), 0, null);
                        if (queuedMessage.canBePlayed)
                        {
                            audioPlayer.playClipImmediately(queuedMessage, false);
                            audioPlayer.closeChannel();
                            gotData = true;
                        }
                    }
                }
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHOS_LEADING) && currentGameState.SessionData.Position > 1)
                {
                    OpponentData opponent = currentGameState.getOpponentAtPosition(1);
                    if (opponent != null)
                    {
                        QueuedMessage queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent), 0, null);
                        if (queuedMessage.canBePlayed)
                        {
                            audioPlayer.playClipImmediately(queuedMessage, false);
                            audioPlayer.closeChannel();
                            gotData = true;
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
