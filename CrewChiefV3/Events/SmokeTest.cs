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

        private OpponentData makeTempDriver(String driverName, List<String> names)
        {
            OpponentData opponent = new OpponentData();
            opponent.DriverRawName = driverName;
            AudioPlayer.availableDriverNames.Add(driverName);
            names.Add(driverName);
            return opponent;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            audioPlayer.queueClip(new QueuedMessage(folderTest, 0, this));
            
            /*List<String> driverNames = new List<string>();
            OpponentData opponent1 = makeTempDriver("bithrey", driverNames);
            OpponentData opponent2 = makeTempDriver("bendy bayer", driverNames);
            OpponentData opponent3 = makeTempDriver("feldsieper", driverNames);
            OpponentData opponent4 = makeTempDriver("milli banter", driverNames);
            OpponentData opponent5 = makeTempDriver("sanque", driverNames);
            audioPlayer.cacheDriverNames(driverNames);
            
            audioPlayer.queueClip(new QueuedMessage("Timings/gap_in_front",
                                    MessageContents(Timings.folderTheGapTo, opponent1, Timings.folderAheadIsIncreasing,
                                    TimeSpan.FromSeconds(1.3), Timings.folderSeconds),
                                    MessageContents(Timings.folderGapInFrontIncreasing, TimeSpan.FromSeconds(1.3), Timings.folderSeconds), 0, this));

            audioPlayer.queueClip(new QueuedMessage("Timings/gap_in_front2",
                                    MessageContents(Timings.folderYoureReeling, opponent5,
                                    Timings.folderInTheGapIsNow, TimeSpan.FromSeconds(0.4), Timings.folderSeconds),
                                    MessageContents(Timings.folderGapInFrontDecreasing, TimeSpan.FromSeconds(0.4), Timings.folderSeconds), 0, this));

            audioPlayer.queueClip(new QueuedMessage("Timings/gap_behind",
                                   MessageContents(Timings.folderTheGapTo, opponent3,
                                   Timings.folderBehindIsIncreasing, TimeSpan.FromSeconds(1.3), Timings.folderSeconds),
                                   MessageContents(Timings.folderGapBehindIncreasing, TimeSpan.FromSeconds(2.9), Timings.folderSeconds), 0, this));

            audioPlayer.queueClip(new QueuedMessage("Timings/gap_behind2",
                                    MessageContents(opponent4, Timings.folderIsReelingYouIn, TimeSpan.FromSeconds(7), Timings.folderSeconds),
                                    MessageContents(Timings.folderGapBehindDecreasing, TimeSpan.FromSeconds(7), Timings.folderSeconds), 0, this));
      
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
                                    MessageContents(opponent3, Opponents.folderIsNowLeading), 0, this));

            audioPlayer.queueClip(new QueuedMessage("Fuel/estimate", MessageContents(
                                        Fuel.folderWeEstimate, QueuedMessage.folderNameNumbersStub + 12, Fuel.folderMinutesRemaining), 0, this));
            
            audioPlayer.queueClip(new QueuedMessage("laptime", MessageContents(LapTimes.folderLapTimeIntro, 
                TimeSpan.FromSeconds(currentGameState.SessionData.LapTimePrevious)), 0, this));
            audioPlayer.queueClip(new QueuedMessage("yesBoxAfter", MessageContents(MandatoryPitStops.folderMandatoryPitStopsYesStopAfter,
                QueuedMessage.folderNameNumbersStub + 10, MandatoryPitStops.folderMandatoryPitStopsMinutes), 0, null));
            audioPlayer.queueClip(new QueuedMessage("laps_on_current_tyres", MessageContents(TyreMonitor.folderLapsOnCurrentTyresIntro,
                QueuedMessage.folderNameNumbersStub + 5, TyreMonitor.folderLapsOnCurrentTyresOutro), 0, this));*/
        }
    }
}
