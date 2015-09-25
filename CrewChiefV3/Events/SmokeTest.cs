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
            /*OpponentData opponent1 = new OpponentData();
            opponent1.DriverRawName = "mitrovic";
            AudioPlayer.availableDriverNames.Add("mitrovic");
            List<String> names = new List<string>();
            names.Add("mitrovic");

            OpponentData opponent2 = new OpponentData();
            opponent2.DriverRawName = "brito";
            AudioPlayer.availableDriverNames.Add("brito");
            names.Add("brito");

            OpponentData opponent3 = new OpponentData();
            opponent3.DriverRawName = "cossette";
            AudioPlayer.availableDriverNames.Add("cossette");
            names.Add("cossette");

            OpponentData opponent4 = new OpponentData();
            opponent4.DriverRawName = "marshall";
            AudioPlayer.availableDriverNames.Add("marshall");
            names.Add("marshall");

            OpponentData opponent5 = new OpponentData();
            opponent5.DriverRawName = "simmonds";
            AudioPlayer.availableDriverNames.Add("simmonds");
            names.Add("simmonds");

            audioPlayer.cacheDriverNames(names);
            audioPlayer.queueClip(new QueuedMessage("Timings/gap_in_front",
                                    MessageContents(Timings.folderTheGapTo, opponent1, Timings.folderAheadIsIncreasing,
                                    TimeSpan.FromSeconds(1.3), Timings.folderSeconds),
                                    MessageContents(Timings.folderGapInFrontIncreasing, TimeSpan.FromSeconds(1.3), Timings.folderSeconds), 0, this));

            audioPlayer.queueClip(new QueuedMessage("Timings/gap_in_front2",
                                    MessageContents(Timings.folderYoureReeling, opponent5,
                                    Timings.folderInTheGapIsNow, TimeSpan.FromSeconds(1.3), Timings.folderSeconds),
                                    MessageContents(Timings.folderGapInFrontDecreasing, TimeSpan.FromSeconds(1.3), Timings.folderSeconds), 0, this));

            audioPlayer.queueClip(new QueuedMessage("Timings/gap_behind",
                                   MessageContents(Timings.folderTheGapTo, opponent3,
                                   Timings.folderBehindIsIncreasing, TimeSpan.FromSeconds(1.3), Timings.folderSeconds),
                                   MessageContents(Timings.folderGapBehindIncreasing, TimeSpan.FromSeconds(1.3), Timings.folderSeconds), 0, this));

            audioPlayer.queueClip(new QueuedMessage("Timings/gap_behind2",
                                    MessageContents(opponent4, Timings.folderIsReelingYouIn, TimeSpan.FromSeconds(1.3), Timings.folderSeconds),
                                    MessageContents(Timings.folderGapBehindDecreasing, TimeSpan.FromSeconds(1.3), Timings.folderSeconds), 0, this));
      
            audioPlayer.queueClip(new QueuedMessage("opponents_1",
                                    MessageContents(opponent1, Opponents.folderAheadIsPitting), 0, this));
            audioPlayer.queueClip(new QueuedMessage("opponents_2",
                                    MessageContents(opponent2, Opponents.folderBehindIsPitting), 0, this));
            audioPlayer.queueClip(new QueuedMessage("opponents_3",
                                    MessageContents(opponent3, Opponents.folderIsPitting), 0, this));
            audioPlayer.queueClip(new QueuedMessage("opponents_4",
                                    MessageContents(Opponents.folderNextCarIs, opponent5), 0, this));
            audioPlayer.queueClip(new QueuedMessage("opponents_5",
                                    MessageContents(Opponents.folderTheLeader, opponent4, Opponents.folderIsPitting), 0, this));
            audioPlayer.queueClip(new QueuedMessage("opponents_1",
                                    MessageContents(opponent3, Opponents.folderIsNowLeading), 0, this));*/
        }
    }
}
