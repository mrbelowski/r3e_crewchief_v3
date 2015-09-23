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
        private string folderLeaderIsPitting = "opponents/the_leader_is_pitting";
        private string folderCarAheadIsPitting = "opponents/the_car_ahead_is_pitting";
        private string folderCarBehindIsPitting = "opponents/the_car_behind_is_pitting";

        private String folderTheLeader = "opponents/the_leader";
        private string folderIsPitting = "opponents/is_pitting";
        private string folderAheadIsPitting = "opponents/ahead_is_pitting";
        private string folderBehindIsPitting = "opponents/behind_is_pitting";

        private List<float> leaderLastLaps = new List<float>();

        private List<float> carAheadLastLaps = new List<float>();

        private List<float> carBehindLastLaps = new List<float>();

        private GameStateData currentGameState;

        public Opponents(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            currentGameState = null;
            leaderLastLaps = new List<float>();
            carAheadLastLaps = new List<float>();
            carBehindLastLaps = new List<float>();
        }

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            return true;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            this.currentGameState = currentGameState;
            if (currentGameState.SessionData.SessionType == SessionType.Race)
            {
                if (!currentGameState.SessionData.IsRacingSameCarInFront)
                {
                    carAheadLastLaps.Clear();
                }
                else if (currentGameState.SessionData.Position > 1)
                {
                    OpponentData carAheadCurrentState = currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1);
                    OpponentData carAheadPreviousState = previousGameState == null ? null : previousGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1);
                    if (carAheadCurrentState != null && carAheadPreviousState != null && carAheadCurrentState.CompletedLaps == carAheadPreviousState.CompletedLaps + 1)
                    {
                        carAheadLastLaps.Add(carAheadCurrentState.approximateLastLapTime);
                    }
                }
                if (!currentGameState.SessionData.IsRacingSameCarBehind)
                {
                    carBehindLastLaps.Clear();
                }
                else if (!currentGameState.isLast())
                {
                    OpponentData carBehindCurrentState = currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position + 1);
                    OpponentData carBehindPreviousState = previousGameState == null ? null : previousGameState.getOpponentAtPosition(currentGameState.SessionData.Position + 1);
                    if (carBehindCurrentState != null && carBehindPreviousState != null && carBehindCurrentState.CompletedLaps == carBehindPreviousState.CompletedLaps + 1)
                    {
                        carBehindLastLaps.Add(carBehindCurrentState.approximateLastLapTime);
                    }
                }
                if (currentGameState.SessionData.HasLeadChanged)
                {
                    leaderLastLaps.Clear();
                }
                else if (currentGameState.SessionData.Position > 1)
                {
                    OpponentData leaderCurrentState = currentGameState.getOpponentAtPosition(1);
                    OpponentData leaderPreviousState = previousGameState == null ? null : previousGameState.getOpponentAtPosition(1);
                    if (leaderCurrentState != null && leaderPreviousState != null && leaderCurrentState.CompletedLaps == leaderPreviousState.CompletedLaps + 1)
                    {
                        leaderLastLaps.Add(leaderCurrentState.approximateLastLapTime);
                    }
                }

                if (currentGameState.PitData.LeaderIsPitting)
                {
                    audioPlayer.queueClip(new QueuedMessage("leader_is_pitting", MessageContents(folderTheLeader, currentGameState.getOpponentAtPosition(1), folderIsPitting), 
                        MessageContents(folderLeaderIsPitting), 0, this));
                }
                if (currentGameState.PitData.CarInFrontIsPitting)
                {
                    audioPlayer.queueClip(new QueuedMessage("car_in_front_is_pitting", MessageContents(currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1), 
                        folderAheadIsPitting), MessageContents(folderCarAheadIsPitting), 0, this));
                }
                if (currentGameState.PitData.CarBehindIsPitting)
                {
                    audioPlayer.queueClip(new QueuedMessage("car_behind_is_pitting", MessageContents(currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position + 1), 
                        folderBehindIsPitting), MessageContents(folderCarBehindIsPitting), 0, this));
                }
            }
        }
        
        public override void respond(String voiceMessage)
        {
            Boolean foundDriver = false;
            if (currentGameState != null)
            {
                if (voiceMessage.StartsWith(SpeechRecogniser.WHERE_IS))
                {
                    foreach (KeyValuePair<int, OpponentData> entry in currentGameState.OpponentData)
                    {
                        String usableDriverName = DriverNameHelper.getUsableNameForRawName(entry.Value.DriverRawName);
                        if (voiceMessage.Contains(usableDriverName))
                        {
                            Console.WriteLine("Got opponent name, raw name = " + entry.Value.DriverRawName + ", using " + usableDriverName);
                            if (entry.Value.IsActive)
                            {
                                int position = entry.Value.Position;
                                OpponentData.OpponentDelta opponentDelta = entry.Value.getTimeDifferenceToPlayer(currentGameState.SessionData);
                                audioPlayer.openChannel();
                                audioPlayer.playClipImmediately(new QueuedMessage("opponentPosition", MessageContents(Position.folderStub, QueuedMessage.folderNameNumbersStub + position), 0, null));
                                if (opponentDelta != null && (opponentDelta.lapDifference != 0 || Math.Abs(opponentDelta.time) > 0.05))
                                {
                                    if (opponentDelta.lapDifference == 1)
                                    {
                                        audioPlayer.playClipImmediately(new QueuedMessage(Position.folderOneLapBehind, 0, null));
                                    }
                                    else if (opponentDelta.lapDifference > 1)
                                    {
                                        audioPlayer.playClipImmediately(new QueuedMessage("opponentTimeDelta",
                                            MessageContents(QueuedMessage.folderNameNumbersStub + opponentDelta.lapDifference, Position.folderLapsBehind), 0, null));
                                    }
                                    else if (opponentDelta.lapDifference == -1)
                                    {
                                        audioPlayer.playClipImmediately(new QueuedMessage(Position.folderOneLapAhead, 0, null));
                                    }
                                    else if (opponentDelta.lapDifference < -1)
                                    {
                                        audioPlayer.playClipImmediately(new QueuedMessage("opponentTimeDelta",
                                            MessageContents(QueuedMessage.folderNameNumbersStub + opponentDelta.lapDifference, Position.folderLapsAhead), 0, null));
                                    }
                                    else
                                    {
                                        TimeSpan delta = TimeSpan.FromSeconds(Math.Abs(opponentDelta.time));
                                        String aheadOrBehind = Position.folderAhead;
                                        if (opponentDelta.time > 0)
                                        {
                                            aheadOrBehind = Position.folderBehind;
                                        }
                                        audioPlayer.playClipImmediately(new QueuedMessage("opponentTimeDelta",
                                            MessageContents(delta, aheadOrBehind), 0, null));
                                    }
                                }
                                audioPlayer.closeChannel();
                            }
                            else
                            {
                                Console.WriteLine("Driver "+ entry.Value.DriverRawName + " is no longer active in this session");
                            }                            
                            foundDriver = true;
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
                            audioPlayer.openChannel();
                            audioPlayer.playClipImmediately(queuedMessage);
                            audioPlayer.closeChannel();
                            foundDriver = true;
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
                            audioPlayer.openChannel();
                            audioPlayer.playClipImmediately(queuedMessage);
                            audioPlayer.closeChannel();
                            foundDriver = true;
                        }
                    }
                }
            }
            if (!foundDriver)
            {
                audioPlayer.openChannel();
                audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
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
