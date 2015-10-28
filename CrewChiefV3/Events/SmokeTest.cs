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

        private OpponentData makeTempDriver(String driverName, List<String> rawDriverNames)
        {
            OpponentData opponent = new OpponentData();
            opponent.DriverRawName = driverName;
            rawDriverNames.Add(driverName);
            return opponent;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            audioPlayer.queueClip(new QueuedMessage(folderTest, 0, this));

            /*List<String> rawDriverNames = new List<string>();
            OpponentData opponent1 = makeTempDriver("haLLunKen henki", rawDriverNames);
            OpponentData opponent2 = makeTempDriver("haLLunKen hipe", rawDriverNames);
            OpponentData opponent3 = makeTempDriver("[PRL] Alex Kingston", rawDriverNames);
            OpponentData opponent4 = makeTempDriver("[PRL] David Bornkessel", rawDriverNames);
            OpponentData opponent5 = makeTempDriver("[PRL] Brad Roller", rawDriverNames);

            List<String> usableDriverNames = DriverNameHelper.getUsableDriverNames(rawDriverNames, audioPlayer.soundFilesPath);
            foreach (String usableDriverName in usableDriverNames)
            {
                AudioPlayer.availableDriverNames.Add(usableDriverName);
            }
            audioPlayer.cacheDriverNames(usableDriverNames);
            Random random = new Random();

            audioPlayer.queueClip(new QueuedMessage("Timings/gap_in_front",
                                    MessageContents(Timings.folderTheGapTo, opponent1, Timings.folderAheadIsIncreasing,
                                    TimeSpanWrapper.FromSeconds((float) random.NextDouble() * 10, true)),
                                    MessageContents(Timings.folderGapInFrontIncreasing, TimeSpanWrapper.FromSeconds((float)random.NextDouble() * 10, true)), 0, this));

            audioPlayer.queueClip(new QueuedMessage("Timings/gap_in_front2",
                                    MessageContents(Timings.folderYoureReeling, opponent5,
                                    Timings.folderInTheGapIsNow, TimeSpanWrapper.FromSeconds((float) random.NextDouble() * 10, true)),
                                    MessageContents(Timings.folderGapInFrontDecreasing, TimeSpanWrapper.FromSeconds((float)random.NextDouble() * 10, true)), 0, this));

            audioPlayer.queueClip(new QueuedMessage("Timings/gap_in_front3",
                                    MessageContents(Timings.folderYoureReeling, opponent5,
                                    Timings.folderInTheGapIsNow, TimeSpanWrapper.FromSeconds((float)random.NextDouble() * 10, true)),
                                    MessageContents(Timings.folderGapInFrontDecreasing, TimeSpanWrapper.FromSeconds((float)random.NextDouble() * 10, true)), 0, this));

            audioPlayer.queueClip(new QueuedMessage("Timings/gap_in_front4",
                                    MessageContents(Timings.folderYoureReeling, opponent5,
                                    Timings.folderInTheGapIsNow, TimeSpanWrapper.FromSeconds(0.04f, true)),
                                    MessageContents(Timings.folderGapInFrontDecreasing, TimeSpanWrapper.FromSeconds((float)random.NextDouble() * 10, true)), 0, this));
            
            audioPlayer.queueClip(new QueuedMessage("Timings/gap_behind",
                                   MessageContents(Timings.folderTheGapTo, opponent3,
                                   Timings.folderBehindIsIncreasing, TimeSpanWrapper.FromSeconds((float)random.NextDouble() * 10, true)),
                                   MessageContents(Timings.folderGapBehindIncreasing, TimeSpanWrapper.FromSeconds((float)random.NextDouble() * 10, true)), 0, this));

            audioPlayer.queueClip(new QueuedMessage("Timings/gap_behind2",
                                    MessageContents(opponent4, Timings.folderIsReelingYouIn, TimeSpanWrapper.FromSeconds((float)random.NextDouble() * 10, true)),
                                    MessageContents(Timings.folderGapBehindDecreasing, TimeSpanWrapper.FromSeconds((float)random.NextDouble() * 10, true)), 0, this));

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
                TimeSpan.FromSeconds(60 + (random.NextDouble() * 60))), 0, this));
            audioPlayer.queueClip(new QueuedMessage("yesBoxAfter", MessageContents(MandatoryPitStops.folderMandatoryPitStopsYesStopAfter,
                QueuedMessage.folderNameNumbersStub + 10, MandatoryPitStops.folderMandatoryPitStopsMinutes), 0, null));
            audioPlayer.queueClip(new QueuedMessage("laps_on_current_tyres", MessageContents(TyreMonitor.folderLapsOnCurrentTyresIntro,
                QueuedMessage.folderNameNumbersStub + 5, TyreMonitor.folderLapsOnCurrentTyresOutro), 0, this));*/
            /*audioPlayer.queueClip(new QueuedMessage("sectors1", MessageContents(LapTimes.folderSector1Is, LapTimes.folderSectorFastest,
                LapTimes.folderSectors2and3Are, LapTimes.folderSectorAFewTenthsOffPace), 0, this));
            audioPlayer.queueClip(new QueuedMessage("sectors2", MessageContents(LapTimes.folderSector2Is, LapTimes.folderSectorASecondOffPace,
                            LapTimes.folderSectors1and3Are, LapTimes.folderSectorATenthOffPace), 0, this));
            audioPlayer.queueClip(new QueuedMessage("sectors3", MessageContents(LapTimes.folderSectorsAllThreeAre, LapTimes.folderSectorMoreThanASecondOffPace), 0, this));
            audioPlayer.queueClip(new QueuedMessage("sectors4", MessageContents(LapTimes.folderSectors1and2Are, LapTimes.folderSectorFast,
                LapTimes.folderSector3Is, LapTimes.folderSectorAFewTenthsOffPace), 0, this));
            audioPlayer.queueClip(new QueuedMessage("sectors5", MessageContents(LapTimes.folderSector2Is, LapTimes.folderSectorASecondOffPace,
                            LapTimes.folderSectors1and3Are, LapTimes.folderSectorATenthOffPace), 0, this));
            audioPlayer.queueClip(new QueuedMessage("sectors6", MessageContents(LapTimes.folderSectorsAllThreeAre, LapTimes.folderSectorMoreThanASecondOffPace), 0, this));*/
            
        }
    }
}
