using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    class DriverNames : AbstractEvent
    {
        private GameStateData currentGameState;

        public DriverNames(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            currentGameState = null;
        }

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState)
        {
            return true;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            this.currentGameState = currentGameState;
        }
        
        public override void respond(String voiceMessage)
        {
            // todo: replace this grotty string manipulation crap

            // todo: more questions and responses for opponent drivers
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
                                List<MessageFragment> messages = new List<MessageFragment>();
                                messages.Add(MessageFragment.Text(Position.folderStub));
                                messages.Add(MessageFragment.Text(QueuedMessage.folderNameNumbersStub + position));
                                audioPlayer.openChannel();
                                audioPlayer.playClipImmediately(new QueuedMessage( "opponentPosition", messages, 0, null));
                                if (opponentDelta != null && (opponentDelta.lapDifference != 0 || Math.Abs(opponentDelta.time) > 0.05))
                                {
                                    if (opponentDelta.lapDifference == 1)
                                    {
                                        audioPlayer.playClipImmediately(new QueuedMessage(Position.folderOneLapBehind, 0, null));
                                    }
                                    else if (opponentDelta.lapDifference > 1)
                                    {
                                        List<MessageFragment> messages2 = new List<MessageFragment>();
                                        messages2.Add(MessageFragment.Text(QueuedMessage.folderNameNumbersStub + opponentDelta.lapDifference));
                                        messages2.Add(MessageFragment.Text(Position.folderLapsBehind));
                                        audioPlayer.playClipImmediately(new QueuedMessage("opponentTimeDelta", messages2, 0, null));
                                    }
                                    else if (opponentDelta.lapDifference == -1)
                                    {
                                        audioPlayer.playClipImmediately(new QueuedMessage(Position.folderOneLapAhead, 0, null));
                                    }
                                    else if (opponentDelta.lapDifference < -1)
                                    {
                                        List<MessageFragment> messages2 = new List<MessageFragment>();
                                        messages2.Add(MessageFragment.Text(QueuedMessage.folderNameNumbersStub + opponentDelta.lapDifference));
                                        messages2.Add(MessageFragment.Text(Position.folderLapsAhead));
                                        audioPlayer.playClipImmediately(new QueuedMessage("opponentTimeDelta", messages2, 0, null));
                                    }
                                    else
                                    {
                                        TimeSpan delta = TimeSpan.FromSeconds(Math.Abs(opponentDelta.time));
                                        List<MessageFragment> messages2 = new List<MessageFragment>();
                                        messages2.Add(MessageFragment.Time(delta));
                                        if (opponentDelta.time > 0)
                                        {
                                            messages2.Add(MessageFragment.Text(Position.folderBehind));
                                        }
                                        else
                                        {
                                            messages2.Add(MessageFragment.Text(Position.folderAhead));
                                        }
                                        audioPlayer.playClipImmediately(new QueuedMessage("opponentTimeDelta", messages2, 0, null));
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
                    QueuedMessage queuedMessage = new QueuedMessage(DriverNameHelper.getUsableNameForRawName(opponent.DriverRawName), 0, null);
                    if (queuedMessage.canBePlayed && opponent.IsActive)
                    {
                        audioPlayer.openChannel();
                        audioPlayer.playClipImmediately(queuedMessage);
                        audioPlayer.closeChannel();
                        foundDriver = true;
                    }
                }
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHOS_IN_FRONT) && currentGameState.SessionData.Position > 1)
                {
                    OpponentData opponent = currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1);
                    QueuedMessage queuedMessage = new QueuedMessage(DriverNameHelper.getUsableNameForRawName(opponent.DriverRawName), 0, null);
                    if (queuedMessage.canBePlayed && opponent.IsActive)
                    {
                        audioPlayer.openChannel();
                        audioPlayer.playClipImmediately(queuedMessage);
                        audioPlayer.closeChannel();
                        foundDriver = true;
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
    }
}
