﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    class OvertakingAidsMonitor : AbstractEvent
    {        
        public static String folderAFewTenthsOffDRSRange = "overtaking_aids/a_few_tenths_off_drs_range";
        public static String folderASecondOffDRSRange = "overtaking_aids/a_second_off_drs_range";
        public static String folderActivationsRemaining = "overtaking_aids/activations_remaining";
        public static String folderNoActivationsRemaining = "overtaking_aids/no_activations_remaining";
        public static String folderOneActivationRemaining = "overtaking_aids/one_activation_remaining";
        public static String folderDontForgetDRS = "overtaking_aids/dont_forget_drs"; 
        public static String folderGuyBehindHasDRS = "overtaking_aids/guy_behind_has_drs";
        public static String folderPushToPassNowAvailable = "overtaking_aids/push_to_pass_now_available";

        private Boolean hasUsedDrsOnThisLap = false;    // TODO: is this sufficient for DTM 2015? Don't they have 3 activations per lap?
        private Boolean drsAvailableOnThisLap = false;
        private float trackDistanceToCheckDRSGapFrontAt = -1;

        private Boolean playedGetCloserForDRSOnThisLap = false;
        private Boolean playedOpponentHasDRSOnThisLap = false;

        private int pushToPassActivationsRemaining = 0;

        private Boolean drsMessagesEnabled = UserSettings.GetUserSettings().getBoolean("enable_drs_messages");
        private Boolean ptpMessagesEnabled = UserSettings.GetUserSettings().getBoolean("enable_push_to_pass_messages");

        public OvertakingAidsMonitor(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            this.hasUsedDrsOnThisLap = false;
            this.drsAvailableOnThisLap = false;
            this.trackDistanceToCheckDRSGapFrontAt = -1;
            this.playedOpponentHasDRSOnThisLap = false;
            this.playedGetCloserForDRSOnThisLap = false;
            this.pushToPassActivationsRemaining = 0;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            // DRS:
            if (drsMessagesEnabled && currentGameState.OvertakingAids.DrsEnabled)
            {
                if (trackDistanceToCheckDRSGapFrontAt == -1 && currentGameState.SessionData.TrackDefinition != null)
                {
                    trackDistanceToCheckDRSGapFrontAt = currentGameState.SessionData.TrackDefinition.trackLength / 2;
                }
                if (currentGameState.SessionData.IsNewLap)
                {
                    if (drsAvailableOnThisLap && !hasUsedDrsOnThisLap)
                    {
                        audioPlayer.queueClip(new QueuedMessage("missed_available_drs", MessageContents(folderDontForgetDRS), 0, this));
                    }
                    drsAvailableOnThisLap = currentGameState.OvertakingAids.DrsAvailable;
                    hasUsedDrsOnThisLap = false;
                    playedGetCloserForDRSOnThisLap = false;
                    playedOpponentHasDRSOnThisLap = false;
                }
                if (currentGameState.OvertakingAids.DrsEngaged)
                {
                    hasUsedDrsOnThisLap = true;
                }
                if (!hasUsedDrsOnThisLap && !drsAvailableOnThisLap && !playedGetCloserForDRSOnThisLap &&
                    currentGameState.PositionAndMotionData.DistanceRoundTrack > trackDistanceToCheckDRSGapFrontAt)
                {
                    if (currentGameState.SessionData.TimeDeltaFront < 1.3 + currentGameState.OvertakingAids.DrsRange &&
                        currentGameState.SessionData.TimeDeltaFront >= 0.6 + currentGameState.OvertakingAids.DrsRange)
                    {
                        audioPlayer.queueClip(new QueuedMessage("drs_a_second_out_of_range", MessageContents(folderASecondOffDRSRange), 0, this));
                        playedGetCloserForDRSOnThisLap = true;
                    }
                    else if (currentGameState.SessionData.TimeDeltaFront < 0.6 + currentGameState.OvertakingAids.DrsRange &&
                        currentGameState.SessionData.TimeDeltaFront >= 0.1 + currentGameState.OvertakingAids.DrsRange)
                    {
                        audioPlayer.queueClip(new QueuedMessage("drs_a_few_tenths_out_of_range", MessageContents(folderAFewTenthsOffDRSRange), 0, this));
                        playedGetCloserForDRSOnThisLap = true;
                    }
                }
                if (!playedOpponentHasDRSOnThisLap && currentGameState.SessionData.TimeDeltaBehind <= currentGameState.OvertakingAids.DrsRange && 
                    currentGameState.SessionData.LapTimeCurrent > currentGameState.SessionData.TimeDeltaBehind &&
                    currentGameState.SessionData.LapTimeCurrent < currentGameState.SessionData.TimeDeltaBehind + 1)
                {
                    audioPlayer.queueClip(new QueuedMessage("opponent_has_drs", MessageContents(folderGuyBehindHasDRS), 0, this));
                    playedOpponentHasDRSOnThisLap = true;
                }
            }

            // push to pass
            if (ptpMessagesEnabled && previousGameState != null)
            {
                if (previousGameState.OvertakingAids.PushToPassEngaged && !currentGameState.OvertakingAids.PushToPassEngaged &&
                    currentGameState.OvertakingAids.PushToPassActivationsRemaining == 0)
                {
                    audioPlayer.queueClip(new QueuedMessage("no_push_to_pass_remaining", MessageContents(folderNoActivationsRemaining), 0, this));
                    pushToPassActivationsRemaining = 0;
                }
                else if (previousGameState.OvertakingAids.PushToPassWaitTimeLeft > 0 && currentGameState.OvertakingAids.PushToPassWaitTimeLeft == 0)
                {
                    if (currentGameState.OvertakingAids.PushToPassActivationsRemaining == 1)
                    {
                        audioPlayer.queueClip(new QueuedMessage("one_push_to_pass_remaining", MessageContents(
                            folderPushToPassNowAvailable, folderOneActivationRemaining), 0, this));
                        pushToPassActivationsRemaining = 1;
                    }
                    else
                    {
                        audioPlayer.queueClip(new QueuedMessage("one_push_to_pass_remaining", MessageContents(folderPushToPassNowAvailable,
                            QueuedMessage.folderNameNumbersStub + currentGameState.OvertakingAids.PushToPassActivationsRemaining, folderActivationsRemaining), 0, this));
                        pushToPassActivationsRemaining = currentGameState.OvertakingAids.PushToPassActivationsRemaining;
                    }
                }
            }
        }

        public override void respond(string voiceMessage)
        {
            // TODO - "how many activations left?" response? Is this needed?
        }
    }
}
