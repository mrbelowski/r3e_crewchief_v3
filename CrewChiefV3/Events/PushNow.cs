using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    class PushNow : AbstractEvent
    {
        private String folderPushToImprove = "push_now/push_to_improve";
        private String folderPushToGetWin = "push_now/push_to_get_win";
        private String folderPushToGetSecond = "push_now/push_to_get_second";
        private String folderPushToGetThird = "push_now/push_to_get_third";
        private String folderPushToHoldPosition = "push_now/push_to_hold_position";

        private String folderPushExitingPits = "push_now/pits_exit_clear";
        private String folderTrafficBehindExitingPits = "push_now/pits_exit_traffic_behind";

        private List<PushData> pushDataInFront;
        private List<PushData> pushDataBehind;
        private Boolean playedNearEndTimePush;
        private int previousDataWindowSizeToCheck = 2;
        private Boolean playedNearEndLapsPush;

        private Boolean isLast;

        public PushNow(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            pushDataInFront = new List<PushData>();
            pushDataBehind = new List<PushData>();
            playedNearEndTimePush = false;
            playedNearEndLapsPush = false;
            isLast = false;
        }
        
        private float getOpponentBestLapInWindow(Boolean ahead)
        {
            float bestLap = -1;
            if (ahead)
            {
                for (int i = pushDataInFront.Count - 1; i > pushDataInFront.Count - previousDataWindowSizeToCheck; i--)
                {
                    float thisLap = pushDataInFront[i].lapTime + (pushDataInFront[i - 1].gap - pushDataInFront[i].gap);
                    if (bestLap == -1 || bestLap > thisLap)
                    {
                        bestLap = thisLap;
                    }
                }
            }
            else
            {
                for (int i = pushDataBehind.Count - 1; i > pushDataBehind.Count - previousDataWindowSizeToCheck; i--)
                {
                    float thisLap = pushDataBehind[i].lapTime - (pushDataBehind[i - 1].gap - pushDataBehind[i].gap);
                    if (bestLap == -1 || bestLap > thisLap)
                    {
                        bestLap = thisLap;
                    }
                }
            }
            return bestLap;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (pushDataInFront == null || pushDataBehind == null)
            {
                clearState();
            }
            if (!currentGameState.SessionData.IsRacingSameCarInFront ||
                (currentGameState.SessionData.TimeDeltaFront > 0 && currentGameState.SessionData.TimeDeltaFront < 1))
            {
                pushDataInFront.Clear();
            }
            if (!currentGameState.SessionData.IsRacingSameCarBehind ||
                (currentGameState.SessionData.TimeDeltaBehind > 0 && currentGameState.SessionData.TimeDeltaBehind < 1))
            {
                pushDataBehind.Clear();
            }
            if (currentGameState.SessionData.IsNewLap)
            {
                pushDataInFront.Add(new PushData(currentGameState.SessionData.LapTimePrevious, currentGameState.SessionData.TimeDeltaFront));
                pushDataBehind.Add(new PushData(currentGameState.SessionData.LapTimePrevious, currentGameState.SessionData.TimeDeltaBehind));                    
            }
            if (currentGameState.SessionData.SessionNumberOfLaps <= 0 && !playedNearEndTimePush &&
                    currentGameState.SessionData.SessionTimeRemaining < 4 * 60 && currentGameState.SessionData.SessionTimeRemaining > 2 * 60)
            {
                // estimate the number of remaining laps - be optimistic...
                int numLapsLeft = (int)Math.Ceiling((double)currentGameState.SessionData.SessionTimeRemaining / (double)currentGameState.SessionData.LapTimeBest);
                playedNearEndTimePush = checkGaps(currentGameState, numLapsLeft);
            }
            else if (currentGameState.SessionData.SessionNumberOfLaps > 0 && currentGameState.SessionData.SessionNumberOfLaps - currentGameState.SessionData.CompletedLaps <= 4 &&
                !playedNearEndLapsPush)
            {
                playedNearEndLapsPush = checkGaps(currentGameState, currentGameState.SessionData.SessionNumberOfLaps - currentGameState.SessionData.CompletedLaps);
            }
            else if (currentGameState.PitData.IsAtPitExit)
            {
                // we've just been handed control back after a pitstop
                if (currentGameState.SessionData.TimeDeltaFront > 3 && currentGameState.SessionData.TimeDeltaBehind > 4)
                {
                    // we've exited into clean air
                    audioPlayer.queueClip(folderPushExitingPits, 0, this);
                }
                else if (currentGameState.SessionData.TimeDeltaBehind > 0 && currentGameState.SessionData.TimeDeltaBehind <= 4 &&
                    previousGameState != null && currentGameState.SessionData.TimeDeltaBehind < previousGameState.SessionData.TimeDeltaBehind)
                {
                    // we've exited the pits but there's traffic behind
                    audioPlayer.queueClip(folderTrafficBehindExitingPits, 0, this);
                }
            }            
        }

        private Boolean checkGaps(GameStateData currentGameState, int numLapsLeft)
        {
            Boolean playedMessage = false;
            if (currentGameState.SessionData.Position > 1 && pushDataInFront.Count >= previousDataWindowSizeToCheck &&
                (getOpponentBestLapInWindow(true) - currentGameState.SessionData.LapTimeBest) * numLapsLeft > currentGameState.SessionData.TimeDeltaFront)
            {
                // going flat out, we're going to catch the guy ahead us before the end
                playedMessage = true;
                if (currentGameState.SessionData.Position == 2)
                {
                    audioPlayer.queueClip(folderPushToGetWin, 0, this);
                }
                else if (currentGameState.SessionData.Position == 3)
                {
                    audioPlayer.queueClip(folderPushToGetSecond, 0, this);
                }
                else if (currentGameState.SessionData.Position == 4)
                {
                    audioPlayer.queueClip(folderPushToGetThird, 0, this);
                }
                else
                {
                    audioPlayer.queueClip(folderPushToImprove, 0, this);
                }
            }
            else if (!isLast && pushDataBehind.Count >= previousDataWindowSizeToCheck &&
                (currentGameState.SessionData.LapTimeBest - getOpponentBestLapInWindow(false)) * numLapsLeft > currentGameState.SessionData.TimeDeltaBehind)
            {
                // even with us going flat out, the guy behind is going to catch us before the end
                playedMessage = true;
                audioPlayer.queueClip(folderPushToHoldPosition, 0, this);
            }
            return playedMessage;
        }

        private class PushData
        {
            public float lapTime;
            public float gap;

            public PushData(float lapTime, float gap)
            {
                this.lapTime = lapTime;
                this.gap = gap;
            }
        }
    }
}
