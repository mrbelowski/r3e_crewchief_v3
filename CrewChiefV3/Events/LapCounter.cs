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

        private String folderLapsMakeThemCount = "lap_counter/laps_make_them_count";
        private String folderMinutesYouNeedToGetOnWithIt = "lap_counter/minutes_you_need_to_get_on_with_it";
        
        private Boolean playedGetReady;

        private Boolean playedPreLightsMessage;

        public Boolean playedFinished;

        private DateTime lastFinishMessageTime = DateTime.MinValue;

        private Boolean enableGreenLightMessages = UserSettings.GetUserSettings().getBoolean("enable_green_light_messages");

        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Countdown, SessionPhase.Formation, SessionPhase.Gridwalk, SessionPhase.Green, SessionPhase.Checkered, SessionPhase.Finished }; }
        }

        public LapCounter(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            playedGetReady = false;
            playedFinished = false;
            playedPreLightsMessage = false;
        }

        public override bool isMessageStillValid(String eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            return applicableSessionPhases.Contains(currentGameState.SessionData.SessionPhase);
        }


        // TODO: Randomise the order of these
        private void playPreLightsMessage(GameStateData currentGameState, Boolean hasWeatherData)
        {
            playedPreLightsMessage = true;
            CrewChiefV3.GameState.Conditions.ConditionsSample currentConditions = currentGameState.Conditions.getMostRecentConditions();
            Boolean playedMessage = false;
            if (hasWeatherData && currentConditions != null)
            {
                audioPlayer.queueClip(new QueuedMessage("trackTemp", MessageContents(ConditionsMonitor.folderTrackTempIsNow, 
                    QueuedMessage.folderNameNumbersStub + Math.Round(currentConditions.TrackTemperature), ConditionsMonitor.folderCelsius), 0, null));
                playedMessage = true;
            }
            if (!playedMessage && currentGameState.PitData.HasMandatoryPitStop)
            {
                if (currentGameState.SessionData.SessionHasFixedTime)
                {
                    audioPlayer.queueClip(new QueuedMessage("pit_window_time", MessageContents(MandatoryPitStops.folderMandatoryPitPitWindowsOpensAfter,
                        QueuedMessage.folderNameNumbersStub + currentGameState.PitData.PitWindowStart, MandatoryPitStops.folderMandatoryPitStopsMinutes), 0, this));
                    playedMessage = true;
                } 
                else
                {
                    audioPlayer.queueClip(new QueuedMessage("pit_window_time", MessageContents(MandatoryPitStops.folderMandatoryPitPitWindowsOpensOnLap,
                        QueuedMessage.folderNameNumbersStub + currentGameState.PitData.PitWindowStart), 0, this));
                    playedMessage = true;
                }
            }
            if (!playedMessage && currentGameState.SessionData.Position == 1)
            {
                audioPlayer.queueClip(new QueuedMessage(Position.folderPole, 0, this));
                playedMessage = true;
            }
            else if (!playedMessage) 
            {
                audioPlayer.queueClip(new QueuedMessage(Position.folderStub + currentGameState.SessionData.Position, 0, this));
                playedMessage = true;
            }
            // TODO: in the countdown / pre-lights phase, we don't know how long the race is going to be so we can't use the 'get on with it' messages :(

        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (!playedPreLightsMessage && currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.SessionPhase == SessionPhase.Gridwalk)
            {
                playPreLightsMessage(currentGameState, false);
                playedPreLightsMessage = true;
            }
            // TODO: in R3E online there's a GridWalk phase before the Countdown. In PCars they're combined. Add some messages to this phase
            if (!playedGetReady && currentGameState.SessionData.SessionType == SessionType.Race && (currentGameState.SessionData.SessionPhase == SessionPhase.Countdown ||
                (currentGameState.SessionData.SessionPhase == SessionPhase.Formation && CrewChief.gameDefinition.gameEnum == GameEnum.RACE_ROOM)))
            {
                if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_64BIT || CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_32BIT || CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK)
                {
                    // special case for PCars...
                    if (!playedPreLightsMessage)
                    {
                        playedPreLightsMessage = true;
                        playPreLightsMessage(currentGameState, true);
                    }
                }
                audioPlayer.queueClip(new QueuedMessage(folderGetReady, 0, this));
                playedGetReady = true;
            }
            /*if (!playedGreenGreenGreen && previousGameState != null && currentGameState.SessionData.SessionType == SessionType.Race &&
                (currentGameState.SessionData.SessionPhase == SessionPhase.Green &&
                    (previousGameState.SessionData.SessionPhase == SessionPhase.Formation ||
                     previousGameState.SessionData.SessionPhase == SessionPhase.Countdown)))
            {*/
            if (previousGameState != null && enableGreenLightMessages && 
                currentGameState.SessionData.SessionType == SessionType.Race &&
                currentGameState.SessionData.SessionPhase == SessionPhase.Green && 
                (previousGameState.SessionData.SessionPhase == SessionPhase.Formation ||
                 previousGameState.SessionData.SessionPhase == SessionPhase.Countdown))
            {
                audioPlayer.removeQueuedClip(folderGetReady);
                audioPlayer.playClipImmediately(new QueuedMessage(folderGreenGreenGreen, 0, this), false);
                audioPlayer.closeChannel();
                audioPlayer.disablePearlsOfWisdom = false;
            }
            if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.IsNewLap && currentGameState.SessionData.CompletedLaps > 0)
            {
                // a new lap has been started in race mode
                int position = currentGameState.SessionData.Position;
                if (currentGameState.SessionData.CompletedLaps == currentGameState.SessionData.SessionNumberOfLaps - 1)
                {
                    if (position == 1)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderLastLapLeading, 0, this));
                    }
                    else if (position < 4)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderLastLapTopThree, 0, this));
                    }
                    else if (position >= 4)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderLastLap, 0, this), PearlsOfWisdom.PearlType.NEUTRAL, 0.5);
                    }
                    else if (position >= 10)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderLastLap, 0, this), PearlsOfWisdom.PearlType.BAD, 0.5);
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
                        audioPlayer.queueClip(new QueuedMessage(folderTwoLeftLeading, 0, this));
                    }
                    else if (position < 4)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderTwoLeftTopThree, 0, this));
                    }
                    else if (position >= 4)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderTwoLeft, 0, this), PearlsOfWisdom.PearlType.NEUTRAL, 0.5);
                    }
                    else if (position >= 10)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderTwoLeft, 0, this), PearlsOfWisdom.PearlType.BAD, 0.5);
                    }
                    else
                    {
                        Console.WriteLine("2 laps left but position is < 1");
                    }
                    // 2 laps left, so prevent any further pearls of wisdom being added

                }
            }
        }
    }
}
