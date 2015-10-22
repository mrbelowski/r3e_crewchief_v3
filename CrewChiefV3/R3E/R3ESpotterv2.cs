using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV3.Events;

namespace CrewChiefV3.RaceRoom
{
    class R3ESpotterv2 : Spotter
    {
        private NoisyCartesianCoordinateSpotter internalSpotter;

        private Boolean paused = false;
        
        // how long is a car? we use 3.5 meters by default here. Too long and we'll get 'hold your line' messages
        // when we're clearly directly behind the car
        private float carLength = UserSettings.GetUserSettings().getFloat("r3e_spotter_car_length");

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");

        private Boolean enabled;

        private Boolean initialEnabledState;

        private AudioPlayer audioPlayer;

        private float intervalSeconds;

        public R3ESpotterv2(AudioPlayer audioPlayer, Boolean initialEnabledState, float intervalSeconds)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            this.intervalSeconds = intervalSeconds;
            this.internalSpotter = new NoisyCartesianCoordinateSpotter(audioPlayer, initialEnabledState, carLength);
        }

        public void clearState()
        {
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

        private DriverData getDriverData(RaceRoomShared shared, int slot_id)
        {
            foreach (DriverData driverData in shared.all_drivers_data)
            {
                if (driverData.driver_info.slot_id == slot_id)
                {
                    return driverData;
                }
            }
            throw new Exception("no driver data for slotID " + slot_id);
        }

        public void trigger(Object lastStateObj, Object currentStateObj)
        {
            if (paused)
            {
                return;
            }

            RaceRoomShared lastState = ((CrewChiefV3.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper)lastStateObj).data;
            RaceRoomShared currentState = ((CrewChiefV3.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper)currentStateObj).data;
            
            if (R3EGameStateMapper.firstViewedDriverSlotId != -1 && currentState.slot_id != -1 && R3EGameStateMapper.firstViewedDriverSlotId != currentState.slot_id)
            {
                audioPlayer.closeChannel();
                return;
            }

            if (!enabled || currentState.Player.GameSimulationTime < timeAfterRaceStartToActivate ||
                currentState.ControlType != (int)RaceRoomConstant.Control.Player || currentState.all_drivers_data.Count() <= 1)
            {
                return;
            }

            DateTime now = DateTime.Now;
           
            DriverData currentPlayerData = getDriverData(currentState, currentState.slot_id);

            DriverData previousPlayerData = getDriverData(lastState, currentState.slot_id);
            float[] currentPlayerPosition = new float[] { currentPlayerData.position.X, currentPlayerData.position.Z };
            float[] previousPlayerPosition = new float[] { previousPlayerData.position.X, previousPlayerData.position.Z };

            if (currentPlayerData.in_pitlane == 0)
            {
                List<float[]> currentOpponentPositions = new List<float[]>();
                List<float[]> previousOpponentPositions = new List<float[]>();

                foreach (DriverData driverData in currentState.all_drivers_data)
                {
                    if (driverData.driver_info.slot_id == currentState.slot_id || driverData.driver_info.slot_id == -1)
                    {
                        continue;
                    }
                    currentOpponentPositions.Add(new float[] { driverData.position.X, driverData.position.Z });
                    try
                    {
                        DriverData previousOpponentData = getDriverData(lastState, driverData.driver_info.slot_id);
                        previousOpponentPositions.Add(new float[] { previousOpponentData.position.X, driverData.position.Z });
                    }
                    catch (Exception e)
                    {
                        // ignore - the mParticipantData array is frequently full of crap
                        previousOpponentPositions.Add(new float[] { 0, 0 });
                    }
                    
                }
                float playerRotation = currentState.CarOrientation.Yaw;                
                if (playerRotation < 0)
                {
                    playerRotation = (float)(2 * Math.PI) + playerRotation;
                }
                playerRotation = (float)(2 * Math.PI) - playerRotation;
                internalSpotter.triggerInternal(playerRotation, currentPlayerPosition, previousPlayerPosition, currentOpponentPositions,
                    previousOpponentPositions, this.intervalSeconds);
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
