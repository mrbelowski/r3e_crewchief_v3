using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    class SmokeTest : AbstractEvent
    {
        private String folderTest = "radio_check/test";

        public SmokeTest(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
        }

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            return true;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            audioPlayer.queueClip(new QueuedMessage(folderTest, 0, this));
            /*OpponentData opponent = new OpponentData();
            opponent.DriverRawName = "carr";
            AudioPlayer.availableDriverNames.Add("carr");
            List<String> names = new List<string>();
            names.Add("carr");
            audioPlayer.cacheDriverNames(names);
            audioPlayer.queueClip(new QueuedMessage("Timings/gap_in_front",
                                    MessageContents(Timings.folderTheGapTo, opponent, Timings.folderAheadIsIncreasing,
                                    TimeSpan.FromSeconds(1.3), Timings.folderSeconds),
                                    MessageContents(Timings.folderGapInFrontIncreasing, TimeSpan.FromSeconds(1.3), Timings.folderSeconds), 0, this));

            audioPlayer.queueClip(new QueuedMessage("Timings/gap_in_front2",
                                    MessageContents(Timings.folderYoureReeling, opponent,
                                    Timings.folderInTheGapIsNow, TimeSpan.FromSeconds(1.3), Timings.folderSeconds),
                                    MessageContents(Timings.folderGapInFrontDecreasing, TimeSpan.FromSeconds(1.3), Timings.folderSeconds), 0, this));

            audioPlayer.queueClip(new QueuedMessage("Timings/gap_behind",
                                   MessageContents(Timings.folderTheGapTo, opponent,
                                   Timings.folderBehindIsIncreasing, TimeSpan.FromSeconds(1.3), Timings.folderSeconds),
                                   MessageContents(Timings.folderGapBehindIncreasing, TimeSpan.FromSeconds(1.3), Timings.folderSeconds), 0, this));

            audioPlayer.queueClip(new QueuedMessage("Timings/gap_behind2",
                                    MessageContents(opponent, Timings.folderIsReelingYouIn, TimeSpan.FromSeconds(1.3), Timings.folderSeconds),
                                    MessageContents(Timings.folderGapBehindDecreasing, TimeSpan.FromSeconds(1.3), Timings.folderSeconds), 0, this));
        
          */
        }
    }
}
