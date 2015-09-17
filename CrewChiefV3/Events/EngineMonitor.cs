using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    class EngineMonitor : AbstractEvent
    {
        private String folderAllClear = "engine_monitor/all_clear";
        private String folderHotWater = "engine_monitor/hot_water";
        private String folderHotOil = "engine_monitor/hot_oil";
        private String folderHotOilAndWater = "engine_monitor/hot_oil_and_water";

        private static float maxSafeWaterTemp = UserSettings.GetUserSettings().getFloat("max_safe_water_temp");
        private static float maxSafeOilTemp = UserSettings.GetUserSettings().getFloat("max_safe_oil_temp");

        EngineStatus lastStatusMessage;

        EngineData engineData;

        // record engine data for 60 seconds then report changes
        double statusMonitorWindowLength = 60;

        double gameTimeAtLastStatusCheck;

        public EngineMonitor(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            lastStatusMessage = EngineStatus.ALL_CLEAR;
            engineData = new EngineData();
            gameTimeAtLastStatusCheck = 0;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (engineData == null)
            {
                clearState();
            }
            if (currentGameState.SessionData.SessionRunningTime > 60 * currentGameState.EngineData.MinutesIntoSessionBeforeMonitoring)
            {
                engineData.addSample(currentGameState.EngineData.EngineOilTemp, currentGameState.EngineData.EngineWaterTemp, 
                    currentGameState.EngineData.EngineOilPressure);

                if (currentGameState.SessionData.SessionRunningTime > gameTimeAtLastStatusCheck + statusMonitorWindowLength)
                {
                    EngineStatus currentEngineStatus = engineData.getEngineStatusFromAverage();
                    if (currentEngineStatus != lastStatusMessage)
                    {
                        switch (currentEngineStatus)
                        {
                            case EngineStatus.ALL_CLEAR:
                                lastStatusMessage = currentEngineStatus;
                                audioPlayer.queueClip(folderAllClear, 0, this);
                                break;
                            case EngineStatus.HOT_OIL:
                                // don't play this if the last message was about hot oil *and* water - wait for 'all clear'
                                if (lastStatusMessage != EngineStatus.HOT_OIL_AND_WATER)
                                {
                                    lastStatusMessage = currentEngineStatus;
                                    audioPlayer.queueClip(folderHotOil, 0, this);
                                }
                                break;
                            case EngineStatus.HOT_WATER:
                                // don't play this if the last message was about hot oil *and* water - wait for 'all clear'
                                if (lastStatusMessage != EngineStatus.HOT_OIL_AND_WATER)
                                {
                                    lastStatusMessage = currentEngineStatus;
                                    audioPlayer.queueClip(folderHotWater, 0, this);
                                }
                                break;
                            case EngineStatus.HOT_OIL_AND_WATER:
                                lastStatusMessage = currentEngineStatus;
                                audioPlayer.queueClip(folderHotOilAndWater, 0, this);
                                break;
                        }
                    }
                    gameTimeAtLastStatusCheck = currentGameState.SessionData.SessionRunningTime;
                    engineData = new EngineData();
                }
            }
        }

        public override void respond(string voiceMessage)
        {
            Boolean gotData = false;
            if (engineData != null)
            {
                gotData = true;
                EngineStatus currentEngineStatus = engineData.getEngineStatusFromCurrent();
                audioPlayer.openChannel();
                switch (currentEngineStatus)
                {
                    case EngineStatus.ALL_CLEAR:
                        lastStatusMessage = currentEngineStatus;
                        audioPlayer.playClipImmediately(folderAllClear, new QueuedMessage(0, null));
                        break;
                    case EngineStatus.HOT_OIL:
                        // don't play this if the last message was about hot oil *and* water - wait for 'all clear'
                        if (lastStatusMessage != EngineStatus.HOT_OIL_AND_WATER)
                        {
                            lastStatusMessage = currentEngineStatus;
                            audioPlayer.playClipImmediately(folderHotOil, new QueuedMessage(0, null));
                        }
                        break;
                    case EngineStatus.HOT_WATER:
                        // don't play this if the last message was about hot oil *and* water - wait for 'all clear'
                        if (lastStatusMessage != EngineStatus.HOT_OIL_AND_WATER)
                        {
                            lastStatusMessage = currentEngineStatus;
                            audioPlayer.playClipImmediately(folderHotWater, new QueuedMessage(0, null));
                        }
                        break;
                    case EngineStatus.HOT_OIL_AND_WATER:
                        lastStatusMessage = currentEngineStatus;
                        audioPlayer.playClipImmediately(folderHotOilAndWater, new QueuedMessage(0, null));
                        break;
                }
                audioPlayer.closeChannel();
            }
            if (!gotData)
            {
                audioPlayer.openChannel();
                audioPlayer.playClipImmediately(AudioPlayer.folderNoData, new QueuedMessage(0, this));
                audioPlayer.closeChannel();
            }
        }

        private class EngineData
        {
            private int samples;
            private float cumulativeOilTemp;
            private float cumulativeWaterTemp;
            private float cumulativeOilPressure;
            private float currentOilTemp;
            private float currentWaterTemp;
            private float currentOilPressure;
            public EngineData()
            {
                this.samples = 0;
                this.cumulativeOilTemp = 0;
                this.cumulativeWaterTemp = 0;
                this.cumulativeOilPressure = 0;
                this.currentOilTemp = 0;
                this.currentWaterTemp = 0;
                this.currentOilPressure = 0;
            }
            public void addSample(float engineOilTemp, float engineWaterTemp, float engineOilPressure)
            {
                this.samples++;
                this.cumulativeOilTemp += engineOilTemp;
                this.cumulativeWaterTemp += engineWaterTemp;
                this.cumulativeOilPressure += engineOilPressure;
                this.currentOilTemp = engineOilTemp;
                this.currentWaterTemp = engineWaterTemp;
                this.currentOilPressure = engineOilPressure;
            }
            public EngineStatus getEngineStatusFromAverage()
            {
                // TODO: detect a sudden drop in oil pressure without triggering false positives caused by stalling the engine
                if (samples > 10)
                {
                    float averageOilTemp = cumulativeOilTemp / samples;
                    float averageWaterTemp = cumulativeWaterTemp / samples;
                    float averageOilPressure = cumulativeOilPressure / samples;
                    if (averageOilTemp > maxSafeOilTemp && averageWaterTemp > maxSafeWaterTemp)
                    {
                        return EngineStatus.HOT_OIL_AND_WATER;
                    }
                    else if (averageWaterTemp > maxSafeWaterTemp)
                    {
                        return EngineStatus.HOT_WATER;
                    }
                    else if (averageOilTemp > maxSafeOilTemp)
                    {
                        return EngineStatus.HOT_OIL;
                    }
                }                
                return EngineStatus.ALL_CLEAR;
                // low oil pressure not implemented
            }
            public EngineStatus getEngineStatusFromCurrent()
            {
                if (currentOilTemp > maxSafeOilTemp && currentWaterTemp > maxSafeWaterTemp)
                {
                    return EngineStatus.HOT_OIL_AND_WATER;
                }
                else if (currentWaterTemp > maxSafeWaterTemp)
                {
                    return EngineStatus.HOT_WATER;
                }
                else if (currentOilTemp > maxSafeOilTemp)
                {
                    return EngineStatus.HOT_OIL;
                }
                return EngineStatus.ALL_CLEAR;
            }
        }

        private enum EngineStatus
        {
            ALL_CLEAR, HOT_OIL, HOT_WATER, HOT_OIL_AND_WATER
        }
    }
}
