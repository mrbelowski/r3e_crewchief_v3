using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    class Position : AbstractEvent
    {
        private String positionValidationKey = "CURRENT_POSITION";

        public static String folderLeading = "position/leading";
        public static String folderPole = "position/pole";
        public static String folderStub = "position/p";
        public static String folderLast = "position/last";
        public static String folderAhead = "position/ahead";
        public static String folderBehind = "position/behind";
        public static String folderLapsAhead = "position/laps_ahead";
        public static String folderLapsBehind = "position/laps_behind"; 
        public static String folderOneLapAhead = "position/one_lap_ahead";
        public static String folderOneLapBehind = "position/one_lap_down";

        private String folderConsistentlyLast = "position/consistently_last";
        private String folderGoodStart = "position/good_start";
        private String folderOKStart = "position/ok_start";
        private String folderBadStart = "position/bad_start";
        private String folderTerribleStart = "position/terrible_start";

        private int currentPosition;

        private int previousPosition;

        private int lapNumberAtLastMessage;

        private Random rand = new Random();

        private int numberOfLapsInLastPlace;

        private Boolean playedRaceStartMessage;

        private Boolean enableRaceStartMessages = UserSettings.GetUserSettings().getBoolean("enable_race_start_messages");

        private Boolean enablePositionMessages = UserSettings.GetUserSettings().getBoolean("enable_position_messages");

        private int startMessageTime;

        private Boolean isLast;

        public Position(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            previousPosition = 0;
            lapNumberAtLastMessage = 0;
            numberOfLapsInLastPlace = 0;
            playedRaceStartMessage = false;
            startMessageTime = rand.Next(30, 50);
            isLast = false;
        }

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            Boolean isStillInThisPosition = true;
            if (validationData != null && validationData.ContainsKey(positionValidationKey) &&
                (int) validationData[positionValidationKey] != currentGameState.SessionData.Position)
            {
                isStillInThisPosition = false;
            }
            return isApplicableForCurrentSessionAndPhase(currentGameState.SessionData.SessionType, currentGameState.SessionData.SessionPhase) &&
                !currentGameState.PitData.InPitlane && isStillInThisPosition;
        }

        protected override void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            currentPosition = currentGameState.SessionData.Position;
            isLast = currentGameState.isLast();
            if (previousPosition == 0)
            {
                previousPosition = currentPosition;
            }
            if (currentGameState.SessionData.SessionPhase == SessionPhase.Green)
            {
                if (currentGameState.SessionData.SessionType == SessionType.Race &&
                    enableRaceStartMessages && !playedRaceStartMessage &&
                    currentGameState.SessionData.CompletedLaps == 0 && currentGameState.SessionData.LapTimeCurrent > startMessageTime)
                {
                    playedRaceStartMessage = true;
                    Console.WriteLine("Race start message... isLast = " + isLast +
                        " session start pos = " + currentGameState.SessionData.SessionStartPosition + " current pos = " + currentGameState.SessionData.Position);
                    if (currentGameState.SessionData.SessionStartPosition + 1 < currentGameState.SessionData.Position)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderBadStart, 0, this));
                    }
                    else if (currentGameState.SessionData.Position == 1 || currentGameState.SessionData.SessionStartPosition >= currentGameState.SessionData.Position)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderGoodStart, 0, this));
                    }
                    else if (currentGameState.SessionData.SessionStartPosition + 5 < currentGameState.SessionData.Position)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderTerribleStart, 0, this));
                    }
                    else if (!isLast && rand.NextDouble() > 0.6)
                    {
                        // only play the OK start message sometimes
                        audioPlayer.queueClip(new QueuedMessage(folderOKStart, 0, this));
                    }
                }
            }
            if (enablePositionMessages && currentGameState.SessionData.IsNewLap)
            {
                if (currentGameState.SessionData.CompletedLaps > 0)
                {
                    playedRaceStartMessage = true;
                }
                if (isLast)
                {
                    numberOfLapsInLastPlace++;
                }
                else
                {
                    numberOfLapsInLastPlace = 0;
                }
                if (previousPosition == 0 && currentGameState.SessionData.Position > 0)
                {
                    previousPosition = currentGameState.SessionData.Position;
                }
                else
                {
                    if (currentGameState.SessionData.CompletedLaps > lapNumberAtLastMessage + 3
                            || previousPosition != currentGameState.SessionData.Position)
                    {
                        Dictionary<String, Object> validationData = new Dictionary<String, Object>();
                        validationData.Add(positionValidationKey, currentGameState.SessionData.Position);
                        PearlsOfWisdom.PearlType pearlType = PearlsOfWisdom.PearlType.NONE;
                        float pearlLikelihood = 0.2f;
                        if (currentGameState.SessionData.SessionType == SessionType.Race)
                        {
                            if (!isLast && (previousPosition > currentGameState.SessionData.Position + 5 ||
                                (previousPosition > currentGameState.SessionData.Position && currentGameState.SessionData.Position <= 5)))
                            {
                                pearlType = PearlsOfWisdom.PearlType.GOOD;
                                pearlLikelihood = 0.8f;
                            }
                            else if (!isLast && previousPosition < currentGameState.SessionData.Position && currentGameState.SessionData.Position > 5)
                            {
                                // note that we don't play a pearl for being last - there's a special set of 
                                // insults reserved for this
                                pearlType = PearlsOfWisdom.PearlType.BAD;
                                pearlLikelihood = 0.5f;
                            }
                            else if (!isLast)
                            {
                                pearlType = PearlsOfWisdom.PearlType.NEUTRAL;
                            }
                        }
                        if (currentGameState.SessionData.Position == 1)
                        {
                            if (currentGameState.SessionData.SessionType == SessionType.Race)
                            {
                                audioPlayer.queueClip(new QueuedMessage(folderLeading, 0, this, validationData), pearlType, pearlLikelihood);
                            }
                            else if (currentGameState.SessionData.SessionType == SessionType.Practice)
                            {
                                audioPlayer.queueClip(new QueuedMessage(folderStub + 1, 0, this, validationData), pearlType, pearlLikelihood);
                            }
                            // no p1 for pole - this is in the laptime tracker (yuk)
                        }
                        else if (!isLast)
                        {
                            audioPlayer.queueClip(new QueuedMessage(folderStub + currentGameState.SessionData.Position, 0, this), pearlType, pearlLikelihood);
                        }
                        else if (isLast)
                        {
                            if (numberOfLapsInLastPlace > 3)
                            {
                                audioPlayer.suspendPearlsOfWisdom();
                                audioPlayer.queueClip(new QueuedMessage(folderConsistentlyLast, 0, this, validationData));
                            }
                            else
                            {
                                audioPlayer.suspendPearlsOfWisdom();
                                audioPlayer.queueClip(new QueuedMessage(folderLast, 0, this, validationData));
                            }
                        }
                        previousPosition = currentGameState.SessionData.Position;
                        lapNumberAtLastMessage = currentGameState.SessionData.CompletedLaps;
                    }
                }
            }
        }

        public override void respond(String voiceMessage)
        {
            if (voiceMessage.Contains(SpeechRecogniser.POSITION))
            {
                if (isLast)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderLast, 0, this), false);
                    audioPlayer.closeChannel();
                }
                else if (currentPosition == 1)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderLeading, 0, this), false);
                    audioPlayer.closeChannel();
                }
                else
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderStub + currentPosition, 0, this), false);
                    audioPlayer.closeChannel();
                }
            }
        }
    }
}
