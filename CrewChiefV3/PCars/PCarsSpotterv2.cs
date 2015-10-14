using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV3.Events;

namespace CrewChiefV3.PCars
{
    class PCarsSpotterv2 : Spotter
    {
        private NoisyCartesianCoordinateSpotter internalSpotter;

        private Boolean paused = false;

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");
        
        private Boolean enabled;

        private Boolean initialEnabledState;
        
        private AudioPlayer audioPlayer;

        private DateTime timeToStartSpotting = DateTime.Now;

        private float intervalSeconds = 50 / 1000;
        
        public PCarsSpotterv2(AudioPlayer audioPlayer, Boolean initialEnabledState, float intervalSeconds)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            this.intervalSeconds = intervalSeconds;
            this.internalSpotter = new NoisyCartesianCoordinateSpotter(audioPlayer, initialEnabledState);
        }

        public void clearState()
        {
            timeToStartSpotting = DateTime.Now;
            audioPlayer.closeChannel();
        }

        public void pause()
        {
            paused = true;
        }

        public void unpause()
        {
            paused = false;
        }

        public void trigger(Object lastStateObj, Object currentStateObj)
        {
            if (paused)
            {
                audioPlayer.closeChannel();
                return;
            }
            pCarsAPIStruct currentState = ((CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)currentStateObj).data;

            // game state is 3 for paused, 5 for replay. No idea what 4 is...
            if (currentState.mGameState == (uint)eGameState.GAME_FRONT_END ||
                (currentState.mGameState == (uint)eGameState.GAME_INGAME_PAUSED && !System.Diagnostics.Debugger.IsAttached) ||
                currentState.mGameState == (uint)eGameState.GAME_VIEWING_REPLAY || currentState.mGameState == (uint)eGameState.GAME_EXITED)
            {
                // don't ignore the paused game updates if we're in debug mode
                audioPlayer.closeChannel();
                return;
            }
            pCarsAPIStruct lastState = ((CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)lastStateObj).data;
            DateTime now = DateTime.Now;

            if (currentState.mRaceState == (int)eRaceState.RACESTATE_RACING &&
                lastState.mRaceState != (int)eRaceState.RACESTATE_RACING)
            {
                timeToStartSpotting = now.Add(TimeSpan.FromSeconds(timeAfterRaceStartToActivate));
            }
            // this check looks a bit funky... whe we start a practice session, the raceState is not_started
            // until we cross the line for the first time. Which is retarded really.
            if (currentState.mRaceState == (int)eRaceState.RACESTATE_INVALID || now < timeToStartSpotting ||
                (currentState.mSessionState == (int)eSessionState.SESSION_RACE && currentState.mRaceState == (int) eRaceState.RACESTATE_NOT_STARTED))
            {
                return;
            }

            if (enabled && currentState.mParticipantData.Count() > 1)
            {
                Tuple<int, pCarsAPIParticipantStruct> playerDataWithIndex = PCarsGameStateMapper.getPlayerDataStruct(currentState.mParticipantData, currentState.mViewedParticipantIndex);
                int playerIndex = playerDataWithIndex.Item1;
                pCarsAPIParticipantStruct playerData = playerDataWithIndex.Item2;
                float[] currentPlayerPosition = new float[] { playerData.mWorldPosition[0], playerData.mWorldPosition[2] };
                Tuple<int, pCarsAPIParticipantStruct> previousPlayerDataWithIndex = PCarsGameStateMapper.getPlayerDataStruct(lastState.mParticipantData, lastState.mViewedParticipantIndex);
                pCarsAPIParticipantStruct previousPlayerData = previousPlayerDataWithIndex.Item2;
                float[] previousPlayerPosition = new float[] { previousPlayerData.mWorldPosition[0], previousPlayerData.mWorldPosition[2] };

                if (currentState.mPitMode == (uint)ePitMode.PIT_MODE_NONE)
                {
                    List<float[]> currentOpponentPositions = new List<float[]>();
                    List<float[]> previousOpponentPositions = new List<float[]>();

                    for (int i = 0; i < currentState.mParticipantData.Count(); i++)
                    {
                        if (i == playerIndex)
                        {
                            continue;
                        }
                        pCarsAPIParticipantStruct opponentData = currentState.mParticipantData[i];
                        if (opponentData.mIsActive)
                        {
                            currentOpponentPositions.Add(new float[] { opponentData.mWorldPosition[0], opponentData.mWorldPosition[2] });
                            try
                            {
                                pCarsAPIParticipantStruct previousOpponentData = PCarsGameStateMapper.getParticipantDataForName(lastState.mParticipantData, opponentData.mName, i);
                                previousOpponentPositions.Add(new float[] { previousOpponentData.mWorldPosition[0], previousOpponentData.mWorldPosition[2] });
                            }
                            catch (Exception e)
                            {
                                // ignore - the mParticipantData array is frequently full of crap
                                previousOpponentPositions.Add(new float[] { 0, 0 });
                            }
                        }
                    }
                    float playerRotation = currentState.mOrientation[1];
                    if (playerRotation < 0)
                    {
                        playerRotation = (float)(2 * Math.PI) + playerRotation;
                    }
                    playerRotation = (float)(2 * Math.PI) - playerRotation;
                    internalSpotter.triggerInternal(playerRotation, currentPlayerPosition, previousPlayerPosition, currentOpponentPositions,
                        previousOpponentPositions, this.intervalSeconds);
                }
            }
        }

        public void enableSpotter()
        {
            enabled = true;
            audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderEnableSpotter, 0, null), false);
            audioPlayer.closeChannel();
        }

        public void disableSpotter()
        {
            enabled = false;
            audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderDisableSpotter, 0, null), false);
            audioPlayer.closeChannel();
        }
    }
}
