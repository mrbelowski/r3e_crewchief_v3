using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    class FlagsMonitor : AbstractEvent
    {
        private String folderBlueFlag = "flags/blue_flag";
        private String folderYellowFlag = "flags/yellow_flag";
        private String folderDoubleYellowFlag = "flags/double_yellow_flag";
        private String folderWhiteFlag = "flags/white_flag";
        private String folderBlackFlag = "flags/black_flag";

        private DateTime lastYellowFlagTime = DateTime.Now;
        private DateTime lastBlackFlagTime = DateTime.Now;
        private DateTime lastWhiteFlagTime = DateTime.Now;
        private DateTime lastBlueFlagTime = DateTime.Now;

        private TimeSpan timeBetweenYellowFlagMessages = TimeSpan.FromSeconds(20);
        private TimeSpan timeBetweenBlueFlagMessages = TimeSpan.FromSeconds(10);
        private TimeSpan timeBetweenBlackFlagMessages = TimeSpan.FromSeconds(20);
        private TimeSpan timeBetweenWhiteFlagMessages = TimeSpan.FromSeconds(20);

        public FlagsMonitor(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            lastYellowFlagTime = DateTime.MinValue;
            lastBlackFlagTime = DateTime.MinValue;
            lastWhiteFlagTime = DateTime.MinValue;
            lastBlueFlagTime = DateTime.MinValue;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            DateTime now = DateTime.Now;
            if (currentGameState.SessionData.Flag == FlagEnum.BLACK)
            {
                if (now > lastBlackFlagTime.Add(timeBetweenBlackFlagMessages))
                {
                    lastBlackFlagTime = now;
                    audioPlayer.queueClip(folderBlackFlag, 0, this);
                }
            }
            else if (currentGameState.SessionData.Flag == FlagEnum.BLUE)
            {
                if (now > lastBlueFlagTime.Add(timeBetweenBlueFlagMessages))
                {
                    lastBlueFlagTime = now;
                    audioPlayer.queueClip(folderBlueFlag, 0, this);
                }
            }
            else if (currentGameState.SessionData.Flag == FlagEnum.YELLOW)
            {
                if (now > lastYellowFlagTime.Add(timeBetweenYellowFlagMessages))
                {
                    lastYellowFlagTime = now;
                    audioPlayer.queueClip(folderYellowFlag, 0, this);
                }
            }
            else if (currentGameState.SessionData.Flag == FlagEnum.WHITE)
            {
                if (now > lastWhiteFlagTime.Add(timeBetweenWhiteFlagMessages))
                {
                    lastWhiteFlagTime = now;
                    audioPlayer.queueClip(folderWhiteFlag, 0, this);
                }
            }
            else if (currentGameState.SessionData.Flag == FlagEnum.DOUBLE_YELLOW)
            {
                if (now > lastYellowFlagTime.Add(timeBetweenYellowFlagMessages))
                {
                    lastYellowFlagTime = now;
                    audioPlayer.queueClip(folderDoubleYellowFlag, 0, this);
                }
            }
        }
    }
}
