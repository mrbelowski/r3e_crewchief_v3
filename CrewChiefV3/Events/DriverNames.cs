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
                        if (voiceMessage.Contains(DriverNameHelper.getPhoneticForRealName(entry.Value.DriverLastName)))
                        {
                            Console.WriteLine("Got opponent name, " + entry.Value.DriverLastName);
                            if (entry.Value.IsActive)
                            {
                                int position = entry.Value.Position;
                                OpponentData.OpponentDelta opponentDelta = entry.Value.getTimeDifferenceToPlayer(currentGameState.SessionData);
                                List<String> messages = new List<String>();
                                messages.Add(Position.folderStub);
                                messages.Add(QueuedMessage.folderNameNumbersStub + position);
                                audioPlayer.openChannel();
                                audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "_opponentPosition", new QueuedMessage(messages, 0, null));
                                if (opponentDelta != null && (opponentDelta.lapDifference != 0 || Math.Abs(opponentDelta.time) > 0.05))
                                {
                                    if (opponentDelta.lapDifference == 1)
                                    {
                                        audioPlayer.playClipImmediately(Position.folderOneLapBehind, new QueuedMessage(0, null));
                                    }
                                    else if (opponentDelta.lapDifference > 1)
                                    {
                                        List<String> messages2 = new List<String>();
                                        messages2.Add(QueuedMessage.folderNameNumbersStub + opponentDelta.lapDifference);
                                        messages2.Add(Position.folderLapsBehind);
                                        audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "_opponentTimeDelta", new QueuedMessage(messages2, 0, null));
                                    }
                                    else if (opponentDelta.lapDifference == -1)
                                    {
                                        audioPlayer.playClipImmediately(Position.folderOneLapAhead, new QueuedMessage(0, null));
                                    }
                                    else if (opponentDelta.lapDifference < -1)
                                    {
                                        List<String> messages2 = new List<String>();
                                        messages2.Add(QueuedMessage.folderNameNumbersStub + opponentDelta.lapDifference);
                                        messages2.Add(Position.folderLapsAhead);
                                        audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "_opponentTimeDelta", new QueuedMessage(messages2, 0, null));
                                    }
                                    else
                                    {
                                        String messageAfterTimespan = Position.folderAhead;
                                        TimeSpan delta = TimeSpan.FromSeconds(Math.Abs(opponentDelta.time));
                                        if (opponentDelta.time > 0)
                                        {
                                            messageAfterTimespan = Position.folderBehind;
                                        }
                                        audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "_opponentTimeDelta", new QueuedMessage(null, messageAfterTimespan, delta, 0, null));
                                    }
                                }
                                audioPlayer.closeChannel();
                            }
                            else
                            {
                                Console.WriteLine("Driver "+ entry.Value.DriverLastName + " is no longer active in this session");
                            }                            
                            foundDriver = true;
                            break;
                        }
                    }
                }
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHOS_BEHIND) && !currentGameState.isLast())
                {
                    OpponentData opponent = currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position + 1);
                    if (audioPlayer.hasDriverName(opponent.DriverLastName) && opponent.IsActive)
                    {
                        audioPlayer.openChannel();
                        audioPlayer.playClipImmediately(QueuedMessage.driverNameIdentifier + opponent.DriverLastName, new QueuedMessage(0, null));
                        audioPlayer.closeChannel();
                        foundDriver = true;
                    }
                }
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHOS_IN_FRONT) && currentGameState.SessionData.Position > 1)
                {
                    OpponentData opponent = currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1);
                    if (audioPlayer.hasDriverName(opponent.DriverLastName) && opponent.IsActive)
                    {
                        audioPlayer.openChannel();
                        audioPlayer.playClipImmediately(QueuedMessage.driverNameIdentifier + opponent.DriverLastName, new QueuedMessage(0, null));
                        audioPlayer.closeChannel();
                        foundDriver = true;
                    }
                }
            }
            if (!foundDriver)
            {
                audioPlayer.openChannel();
                audioPlayer.playClipImmediately(AudioPlayer.folderNoData, new QueuedMessage(0, null));
                audioPlayer.closeChannel();
            }       
        }
    }
}
