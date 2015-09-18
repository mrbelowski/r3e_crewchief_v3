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
    }
}
