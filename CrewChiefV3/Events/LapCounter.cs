using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    class LapCounter : AbstractEvent
    {
        private String folderGreenGreenGreen = "lap_counter/green_green_green";

        private String folderGetReady = "lap_counter/get_ready";

        private String folderLastLap = "lap_counter/last_lap";

        private String folderTwoLeft = "lap_counter/two_to_go";

        private String folderLastLapLeading = "lap_counter/last_lap_leading";

        private String folderLastLapTopThree = "lap_counter/last_lap_top_three";

        private String folderTwoLeftLeading = "lap_counter/two_to_go_leading";

        private String folderTwoLeftTopThree = "lap_counter/two_to_go_top_three";
        
        Boolean playedGreenGreenGreen;
        Boolean playedGetReady;

        public Boolean playedFinished;

        private DateTime lastFinishMessageTime = DateTime.MinValue;

        private Boolean enableSessionEndMessages = UserSettings.GetUserSettings().getBoolean("enable_session_end_messages");

        protected override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Countdown, SessionPhase.Formation, SessionPhase.Green, SessionPhase.Checkered, SessionPhase.Finished }; }
        }

        public LapCounter(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            playedGreenGreenGreen = false;
            playedGetReady = false;
            playedFinished = false;
        }

        public override bool isMessageStillValid(String eventSubType, GameStateData currentGameState)
        {
            return applicableSessionPhases.Contains(currentGameState.SessionData.SessionPhase);
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (!playedGetReady && currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.SessionPhase == SessionPhase.Countdown)
            {
                audioPlayer.openChannel();
                audioPlayer.playClipImmediately(folderGetReady, new QueuedMessage(0, this));
                playedGetReady = true;
                audioPlayer.closeChannel();
            }
            /*if (!playedGreenGreenGreen && previousGameState != null && currentGameState.SessionData.SessionType == SessionType.Race &&
                (currentGameState.SessionData.SessionPhase == SessionPhase.Green &&
                    (previousGameState.SessionData.SessionPhase == SessionPhase.Formation ||
                     previousGameState.SessionData.SessionPhase == SessionPhase.Countdown)))
            {*/
            if (!playedGreenGreenGreen && previousGameState != null &&
                currentGameState.SessionData.SessionType == SessionType.Race &&
                currentGameState.SessionData.SessionPhase == SessionPhase.Green)
            {
                if (previousGameState.SessionData.SessionPhase == SessionPhase.Formation ||
                 previousGameState.SessionData.SessionPhase == SessionPhase.Countdown)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderGreenGreenGreen, new QueuedMessage(0, this));
                    audioPlayer.closeChannel();
                    playedGreenGreenGreen = true;
                }
            }
            if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.IsNewLap && currentGameState.SessionData.CompletedLaps > 0)
            {
                // a new lap has been started in race mode
                int position = currentGameState.SessionData.Position;
                if (currentGameState.SessionData.CompletedLaps == currentGameState.SessionData.SessionNumberOfLaps - 1)
                {
                    if (position == 1)
                    {
                        audioPlayer.queueClip(folderLastLapLeading, 0, this);
                    }
                    else if (position < 4)
                    {
                        audioPlayer.queueClip(folderLastLapTopThree, 0, this);
                    }
                    else if (position >= 4)
                    {
                        audioPlayer.queueClip(folderLastLap, 0, this, PearlsOfWisdom.PearlType.NEUTRAL, 0.5);
                    }
                    else if (position >= 10)
                    {
                        audioPlayer.queueClip(folderLastLap, 0, this, PearlsOfWisdom.PearlType.BAD, 0.5);
                    }
                    else
                    {
                        Console.WriteLine("1 lap left but position is < 1");
                    }
                }
                else if (currentGameState.SessionData.CompletedLaps == currentGameState.SessionData.SessionNumberOfLaps - 2)
                {
                    if (position == 1)
                    {
                        audioPlayer.queueClip(folderTwoLeftLeading, 0, this);
                    }
                    else if (position < 4)
                    {
                        audioPlayer.queueClip(folderTwoLeftTopThree, 0, this);
                    }
                    else if (position >= 4)
                    {
                        audioPlayer.queueClip(folderTwoLeft, 0, this, PearlsOfWisdom.PearlType.NEUTRAL, 0.5);
                    }
                    else if (position >= 10)
                    {
                        audioPlayer.queueClip(folderTwoLeft, 0, this, PearlsOfWisdom.PearlType.BAD, 0.5);
                    }
                    else
                    {
                        Console.WriteLine("2 laps left but position is < 1");
                    }
                }
            }
        }
    }
}
