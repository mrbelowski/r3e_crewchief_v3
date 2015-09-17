using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    class Penalties : AbstractEvent
    {
        // time (in seconds) to delay messages about penalty laps to go - 
        // we need this because the play might cross the start line while serving 
        // a penalty, so we should wait before telling them how many laps they have to serve it
        private int pitstopDelay = 20;

        private String folderNewPenaltyStopGo = "penalties/new_penalty_stopgo";

        private String folderNewPenaltyDriveThrough = "penalties/new_penalty_drivethrough";

        private String folderThreeLapsToServe = "penalties/penalty_three_laps_left";

        private String folderTwoLapsToServe = "penalties/penalty_two_laps_left";

        private String folderOneLapToServeStopGo = "penalties/penalty_one_lap_left_stopgo";

        private String folderOneLapToServeDriveThrough = "penalties/penalty_one_lap_left_drivethrough";

        private String folderDisqualified = "penalties/penalty_disqualified";

        private String folderPitNowStopGo = "penalties/pit_now_stop_go";

        private String folderPitNowDriveThrough = "penalties/pit_now_drive_through";

        private String folderTimePenalty = "penalties/time_penalty";

        private String folderCutTrackInRace = "penalties/cut_track_in_race";

        private String folderLapDeleted = "penalties/lap_deleted";

        private String folderCutTrackPracticeOrQual = "penalties/cut_track_in_prac_or_qual";

        private String folderPenaltyNotServed = "penalties/penalty_not_served";

        // for voice requests
        private String folderYouStillHavePenalty = "penalties/you_still_have_a_penalty";

        private String folderYouHavePenalty = "penalties/you_have_a_penalty";

        private String folderPenaltyServed = "penalties/penalty_served";

        private String folderYouDontHaveAPenalty = "penalties/you_dont_have_a_penalty";


        private Boolean hasHadAPenalty;

        private int penaltyLap;

        private int lapsCompleted;

        private Boolean playedPitNow;

        private Boolean hasOutstandingPenalty = false;

        private Boolean playedTimePenaltyMessage;

        private int cutTrackWarningsCount;

        private TimeSpan cutTrackWarningFrequency = TimeSpan.FromSeconds(30);

        private Boolean playCutTrackWarnings = UserSettings.GetUserSettings().getBoolean("play_cut_track_warnings");

        private DateTime lastCutTrackWarningTime;

        public Penalties(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            clearPenaltyState();
            lastCutTrackWarningTime = DateTime.Now;
            cutTrackWarningsCount = 0;
            hasHadAPenalty = false;
        }

        private void clearPenaltyState()
        {
            penaltyLap = -1;
            lapsCompleted = -1;
            hasOutstandingPenalty = false;
            // edge case here: if a penalty is given and immediately served (slow down penalty), then
            // the player gets another within the next 20 seconds, the 'you have 3 laps to come in to serve'
            // message would be in the queue and would be made valid again, so would play. So we explicity 
            // remove this message from the queue
            audioPlayer.removeQueuedClip(folderThreeLapsToServe);
            playedPitNow = false;
            playedTimePenaltyMessage = false;
        }

        public override bool isMessageStillValid(String eventSubType, GameStateData currentGameState)
        {
            // when a new penalty is given we queue a 'three laps left to serve' message for 20 seconds in the future.
            // If, 20 seconds later, the player has started a new lap, this message is no longer valid so shouldn't be played
            if (eventSubType == folderThreeLapsToServe)
            {
                Console.WriteLine("checking penalty validity, pen lap = " + penaltyLap + ", completed =" + lapsCompleted);
                return hasOutstandingPenalty && lapsCompleted == penaltyLap && currentGameState.SessionData.SessionPhase != SessionPhase.Finished;
            }
            else if (eventSubType == folderCutTrackInRace) 
            {
                return !hasOutstandingPenalty && currentGameState.SessionData.SessionPhase != SessionPhase.Finished;
            }
            else if(eventSubType == folderCutTrackPracticeOrQual || eventSubType == folderLapDeleted)
            {
                return currentGameState.SessionData.SessionPhase != SessionPhase.Finished;
            }
            else
            {
                return hasOutstandingPenalty && currentGameState.SessionData.SessionPhase != SessionPhase.Finished;
            }
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.SessionData.SessionType == SessionType.Race && previousGameState != null && 
                currentGameState.PenaltiesData.HasDriveThrough || currentGameState.PenaltiesData.HasStopAndGo || currentGameState.PenaltiesData.HasTimeDeduction)
            {
                if (currentGameState.PenaltiesData.HasDriveThrough && !previousGameState.PenaltiesData.HasDriveThrough)
                {
                    lapsCompleted = currentGameState.SessionData.CompletedLaps;
                    // this is a new penalty
                    audioPlayer.queueClip(folderNewPenaltyDriveThrough, 0, this);
                    // queue a '3 laps to serve penalty' message - this might not get played
                    audioPlayer.queueClip(folderThreeLapsToServe, 20, this);
                    // we don't already have a penalty
                    if (penaltyLap == -1 || !hasOutstandingPenalty)
                    {
                        penaltyLap = currentGameState.SessionData.CompletedLaps;
                    }
                    hasOutstandingPenalty = true;
                    hasHadAPenalty = true;
                }
                else if (currentGameState.PenaltiesData.HasStopAndGo && !previousGameState.PenaltiesData.HasStopAndGo)
                {
                    lapsCompleted = currentGameState.SessionData.CompletedLaps;
                    // this is a new penalty
                    audioPlayer.queueClip(folderNewPenaltyStopGo, 0, this);
                    // queue a '3 laps to serve penalty' message - this might not get played
                    audioPlayer.queueClip(folderThreeLapsToServe, 20, this);
                    // we don't already have a penalty
                    if (penaltyLap == -1 || !hasOutstandingPenalty)
                    {
                        penaltyLap = currentGameState.SessionData.CompletedLaps;
                    }
                    hasOutstandingPenalty = true;
                    hasHadAPenalty = true;
                }
                else if (currentGameState.PitData.InPitlane && currentGameState.PitData.OnOutLap && (currentGameState.PenaltiesData.HasStopAndGo || currentGameState.PenaltiesData.HasDriveThrough))
                {
                    // we've exited the pits but there's still an outstanding penalty
                    audioPlayer.queueClip(folderPenaltyNotServed, 3, this);
                } 
                else if (currentGameState.SessionData.IsNewLap && (currentGameState.PenaltiesData.HasStopAndGo || currentGameState.PenaltiesData.HasDriveThrough))
                {
                    // TODO: variable number of laps to serve penalty...

                    lapsCompleted = currentGameState.SessionData.CompletedLaps;
                    if (lapsCompleted - penaltyLap == 3 && !currentGameState.PitData.InPitlane)
                    {
                        // run out of laps, an not in the pitlane
                        audioPlayer.queueClip(folderDisqualified, 5, this);
                    }
                    else if (lapsCompleted - penaltyLap == 2 && currentGameState.PenaltiesData.HasDriveThrough)
                    {
                        audioPlayer.queueClip(folderOneLapToServeDriveThrough, pitstopDelay, this);
                    }
                    else if (lapsCompleted - penaltyLap == 2 && currentGameState.PenaltiesData.HasStopAndGo)
                    {
                        audioPlayer.queueClip(folderOneLapToServeStopGo, pitstopDelay, this);
                    }
                    else if (lapsCompleted - penaltyLap == 1)
                    {
                        audioPlayer.queueClip(folderTwoLapsToServe, pitstopDelay, this);
                    }
                }
                else if (!playedPitNow && currentGameState.SessionData.SectorNumber == 3 && currentGameState.PenaltiesData.HasStopAndGo && lapsCompleted - penaltyLap == 2)
                {
                    playedPitNow = true;
                    audioPlayer.queueClip(folderPitNowStopGo, 6, this);
                }
                else if (!playedPitNow && currentGameState.SessionData.SectorNumber == 3 && currentGameState.PenaltiesData.HasDriveThrough && lapsCompleted - penaltyLap == 2)
                {
                    playedPitNow = true;
                    audioPlayer.queueClip(folderPitNowDriveThrough, 6, this);
                }
                else if (!playedTimePenaltyMessage && currentGameState.PenaltiesData.HasTimeDeduction)
                {
                    playedTimePenaltyMessage = true;
                    audioPlayer.queueClip(folderTimePenalty, 0, this);
                }
            }
            else if (currentGameState.PositionAndMotionData.CarSpeed > 1 && playCutTrackWarnings && currentGameState.SessionData.SessionType != SessionType.Race &&
              !currentGameState.SessionData.CurrentLapIsValid && previousGameState != null && previousGameState.SessionData.CurrentLapIsValid)
            {
                cutTrackWarningsCount = currentGameState.PenaltiesData.CutTrackWarnings;
                DateTime now = DateTime.Now;
                // don't warn about cut track if the AI is driving
                if (currentGameState.ControlData.ControlType != ControlType.AI &&
                    lastCutTrackWarningTime.Add(cutTrackWarningFrequency) < now)
                {
                    lastCutTrackWarningTime = DateTime.Now;
                    audioPlayer.queueClip(folderLapDeleted, 2, this);
                    clearPenaltyState();
                }
            }
            else if (currentGameState.PositionAndMotionData.CarSpeed > 1 && playCutTrackWarnings &&
                currentGameState.PenaltiesData.CutTrackWarnings > cutTrackWarningsCount)
            {
                cutTrackWarningsCount = currentGameState.PenaltiesData.CutTrackWarnings;
                DateTime now = DateTime.Now;
                if (currentGameState.ControlData.ControlType != ControlType.AI &&
                    lastCutTrackWarningTime.Add(cutTrackWarningFrequency) < now)
                {
                    lastCutTrackWarningTime = now;
                    if (currentGameState.SessionData.SessionType == SessionType.Race)
                    {
                        audioPlayer.queueClip(folderCutTrackInRace, 2, this);
                    }
                    else
                    {
                        audioPlayer.queueClip(folderCutTrackPracticeOrQual, 2, this);
                    }
                    clearPenaltyState();
                }
            }                     
            else
            {
                clearPenaltyState();
            }
            if (currentGameState.SessionData.SessionType == SessionType.Race && previousGameState != null && 
                ((previousGameState.PenaltiesData.HasStopAndGo && !currentGameState.PenaltiesData.HasStopAndGo) ||
                (previousGameState.PenaltiesData.HasDriveThrough && !currentGameState.PenaltiesData.HasDriveThrough)))
            {
                audioPlayer.queueClip(folderPenaltyServed, 0, null);
            }            
        }

        public override void respond(string voiceMessage)
        {
            if (!hasHadAPenalty)
            {
                audioPlayer.openChannel();
                audioPlayer.playClipImmediately(folderYouDontHaveAPenalty, new QueuedMessage(0, null));
                audioPlayer.closeChannel();
                return;
            }
            if (voiceMessage.Contains(SpeechRecogniser.DO_I_HAVE_A_PENALTY))
            {
                if (hasOutstandingPenalty) {
                    if (lapsCompleted - penaltyLap == 2) {
                        List<String> messages = new List<String>();
                        messages.Add(folderYouHavePenalty);
                        messages.Add(MandatoryPitStops.folderMandatoryPitStopsPitThisLap);
                        audioPlayer.openChannel();
                        audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "_youHaveAPenaltyBoxThisLap",
                            new QueuedMessage(messages, 0, null));
                        audioPlayer.closeChannel();
                    } else
                    {
                        audioPlayer.openChannel();
                        audioPlayer.playClipImmediately(folderYouHavePenalty, new QueuedMessage(0, null));
                        audioPlayer.closeChannel();
                    }
                }
                else
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderYouDontHaveAPenalty, new QueuedMessage(0, null));
                    audioPlayer.closeChannel();
                }
            }
            else if (voiceMessage.Contains(SpeechRecogniser.HAVE_I_SERVED_MY_PENALTY))
            {
                if (hasOutstandingPenalty)
                {
                    List<String> messages = new List<String>();
                    messages.Add(AudioPlayer.folderNo);
                    messages.Add(folderYouStillHavePenalty);
                    if (lapsCompleted - penaltyLap == 2)
                    {
                        messages.Add(MandatoryPitStops.folderMandatoryPitStopsPitThisLap);
                    }
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "_noYouStillHaveAPenalty",
                        new QueuedMessage(messages, 0, null));
                    audioPlayer.closeChannel();
                }
                else
                {
                    List<String> messages = new List<String>();
                    messages.Add(AudioPlayer.folderYes);
                    messages.Add(folderPenaltyServed);
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "_yesYouServedYourPenalty",
                        new QueuedMessage(messages, 0, null));
                    audioPlayer.closeChannel();
                }
            } else if (voiceMessage.Contains(SpeechRecogniser.DO_I_STILL_HAVE_A_PENALTY))
            {
                if (hasOutstandingPenalty)
                {
                    List<String> messages = new List<String>();
                    messages.Add(AudioPlayer.folderYes);
                    messages.Add(folderYouStillHavePenalty);
                    if (lapsCompleted - penaltyLap == 2)
                    {
                        messages.Add(MandatoryPitStops.folderMandatoryPitStopsPitThisLap);
                    }
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "_yesYouStillHaveAPenalty",
                        new QueuedMessage(messages, 0, null));
                    audioPlayer.closeChannel();
                }
                else
                {
                    List<String> messages = new List<String>();
                    messages.Add(AudioPlayer.folderNo);
                    messages.Add(folderPenaltyServed);
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "_noYouServedYourPenalty",
                        new QueuedMessage(messages, 0, null));
                    audioPlayer.closeChannel();
                }                
            }
        }
    }
}
