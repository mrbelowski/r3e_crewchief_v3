using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    class Fuel : AbstractEvent
    {
        private String folderOneLapEstimate = "fuel/one_lap_fuel";

        private String folderTwoLapsEstimate = "fuel/two_laps_fuel";

        private String folderThreeLapsEstimate = "fuel/three_laps_fuel";

        private String folderFourLapsEstimate = "fuel/four_laps_fuel";

        private String folderHalfDistanceGoodFuel = "fuel/half_distance_good_fuel";

        private String folderHalfDistanceLowFuel = "fuel/half_distance_low_fuel";

        private String folderHalfTankWarning = "fuel/half_tank_warning";

        private String folderTenMinutesFuel = "fuel/ten_minutes_fuel";

        private String folderTwoMinutesFuel = "fuel/two_minutes_fuel";

        private String folderFiveMinutesFuel = "fuel/five_minutes_fuel";

        private String folderMinutesRemaining = "fuel/minutes_remaining";

        private String folderLapsRemaining = "fuel/laps_remaining";

        private String folderWeEstimate = "fuel/we_estimate";

        private String folderPlentyOfFuel = "fuel/plenty_of_fuel";

        private float averageUsagePerLap;

        private float averageUsagePerMinute;

        // fuel in tank 15 seconds after game start
        private float fuelAfter15Seconds;

        private int halfDistance;

        private float halfTime;

        private Boolean playedHalfTankWarning;

        private Boolean initialised;

        private Boolean playedHalfTimeFuelEstimate;

        private int fuelUseWindowLength = 3;

        private List<float> fuelUseWindow;

        private float gameTimeAtLastFuelWindowUpdate;

        private Boolean playedTwoMinutesRemaining;

        private Boolean playedFiveMinutesRemaining;

        private Boolean playedTenMinutesRemaining;

        private Boolean fuelUseActive;

        // check fuel use every 2 minutes
        private int fuelUseSampleTime = 2;

        private float currentFuel;

        private Boolean enableFuelMessages = UserSettings.GetUserSettings().getBoolean("enable_fuel_messages");

        public Fuel(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            fuelAfter15Seconds = 0;
            averageUsagePerLap = 0;
            halfDistance = 0;
            playedHalfTankWarning = false;
            initialised = false;
            halfTime = 0;
            playedHalfTimeFuelEstimate = false;
            fuelUseWindow = new List<float>();
            gameTimeAtLastFuelWindowUpdate = 0;
            averageUsagePerMinute = 0;
            playedFiveMinutesRemaining = false;
            playedTenMinutesRemaining = false;
            playedTwoMinutesRemaining = false;
            currentFuel = -1;
            fuelUseActive = false;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.FuelData.FuelUseActive)
            {
                fuelUseActive = true;
                currentFuel = currentGameState.FuelData.FuelLeft;
                // To get the initial fuel, wait for 15 seconds
                if (!initialised && currentGameState.SessionData.SessionRunningTime > 15)
                {
                    fuelUseWindow = new List<float>();
                    fuelAfter15Seconds = currentGameState.FuelData.FuelLeft;
                    fuelUseWindow.Add(fuelAfter15Seconds);
                    gameTimeAtLastFuelWindowUpdate = currentGameState.SessionData.SessionRunningTime;
                    Console.WriteLine("Fuel after 15s = " + fuelAfter15Seconds);
                    initialised = true;
                    if (currentGameState.SessionData.SessionNumberOfLaps > 0)
                    {
                        halfDistance = currentGameState.SessionData.SessionNumberOfLaps / 2;
                    }
                    else
                    {
                        halfTime = currentGameState.SessionData.SessionRunTime / 2;
                        Console.WriteLine("Half time = " + halfTime);
                    }
                }
                if (currentGameState.SessionData.IsNewLap && initialised && currentGameState.SessionData.CompletedLaps > 0 && currentGameState.SessionData.SessionNumberOfLaps > 0)
                {
                    // completed a lap, so store the fuel left at this point:
                    fuelUseWindow.Insert(0, currentGameState.FuelData.FuelLeft);
                    // if we've got fuelUseWindowLength + 1 samples (note we initialise the window data with fuelAt15Seconds so we always
                    // have one extra), get the average difference between each pair of values

                    // only do this if we have a full window of data + one extra start point
                    if (fuelUseWindow.Count > fuelUseWindowLength)
                    {
                        averageUsagePerLap = 0;
                        for (int i = 0; i < fuelUseWindowLength - 1; i++)
                        {
                            averageUsagePerLap += (fuelUseWindow[i + 1] - fuelUseWindow[i]);
                        }
                        averageUsagePerLap = averageUsagePerLap / fuelUseWindowLength;
                    }
                    else
                    {
                        averageUsagePerLap = (fuelAfter15Seconds - currentGameState.FuelData.FuelLeft) / currentGameState.SessionData.CompletedLaps;
                    }
                    int estimatedFuelLapsLeft = (int)Math.Floor(currentGameState.FuelData.FuelLeft / averageUsagePerLap);
                    if (enableFuelMessages && currentGameState.SessionData.CompletedLaps == halfDistance)
                    {
                        if (estimatedFuelLapsLeft < halfDistance && currentGameState.FuelData.FuelLeft / fuelAfter15Seconds < 0.6)
                        {
                            audioPlayer.queueClip(folderHalfDistanceLowFuel, 0, this);
                        }
                        else
                        {
                            audioPlayer.queueClip(folderHalfDistanceGoodFuel, 0, this);
                        }
                    }
                    else if (enableFuelMessages && estimatedFuelLapsLeft == 4)
                    {
                        Console.WriteLine("4 laps fuel left, starting fuel = " + fuelAfter15Seconds +
                                ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.queueClip(folderFourLapsEstimate, 0, this);
                    }
                    else if (enableFuelMessages && estimatedFuelLapsLeft == 3)
                    {
                        Console.WriteLine("3 laps fuel left, starting fuel = " + fuelAfter15Seconds +
                            ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.queueClip(folderThreeLapsEstimate, 0, this);
                    }
                    else if (enableFuelMessages && estimatedFuelLapsLeft == 2)
                    {
                        Console.WriteLine("2 laps fuel left, starting fuel = " + fuelAfter15Seconds +
                            ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.queueClip(folderTwoLapsEstimate, 0, this);
                    }
                    else if (enableFuelMessages && estimatedFuelLapsLeft == 1)
                    {
                        Console.WriteLine("1 lap fuel left, starting fuel = " + fuelAfter15Seconds +
                            ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.queueClip(folderOneLapEstimate, 0, this);
                    }
                }
                else if (initialised && currentGameState.SessionData.SessionNumberOfLaps <= 0 && !playedHalfTimeFuelEstimate && currentGameState.SessionData.SessionTimeRemaining <= halfTime &&
                    averageUsagePerMinute > 0)
                {
                    Console.WriteLine("Half race distance. Fuel in tank = " + currentGameState.FuelData.FuelLeft + ", average usage per minute = " + averageUsagePerMinute);
                    playedHalfTimeFuelEstimate = true;
                    if (enableFuelMessages && averageUsagePerMinute * halfTime / 60 > currentGameState.FuelData.FuelLeft
                        && currentGameState.FuelData.FuelLeft / fuelAfter15Seconds < 0.6)
                    {
                        audioPlayer.queueClip(folderHalfDistanceLowFuel, 0, this);
                    }
                    else
                    {
                        audioPlayer.queueClip(folderHalfDistanceGoodFuel, 0, this);
                    }
                }
                else if (initialised && currentGameState.SessionData.SessionNumberOfLaps <= 0 && currentGameState.SessionData.SessionRunningTime > gameTimeAtLastFuelWindowUpdate + (60 * fuelUseSampleTime))
                {
                    // it's 2 minutes since the last fuel window check
                    gameTimeAtLastFuelWindowUpdate = currentGameState.SessionData.SessionRunningTime;
                    fuelUseWindow.Insert(0, currentGameState.FuelData.FuelLeft);
                    // if we've got fuelUseWindowLength + 1 samples (note we initialise the window data with fuelAt15Seconds so we always
                    // have one extra), get the average difference between each pair of values

                    // only do this if we have a full window of data + one extra start point
                    if (fuelUseWindow.Count > fuelUseWindowLength)
                    {
                        averageUsagePerMinute = 0;
                        for (int i = 0; i < fuelUseWindowLength - 1; i++)
                        {
                            averageUsagePerMinute += (fuelUseWindow[i + 1] - fuelUseWindow[i]);
                        }
                        averageUsagePerMinute = averageUsagePerMinute / (fuelUseWindowLength * fuelUseSampleTime);
                    }
                    else
                    {
                        averageUsagePerMinute = 60 * (fuelAfter15Seconds - currentGameState.FuelData.FuelLeft) / (float)gameTimeAtLastFuelWindowUpdate;
                    }
                    int estimatedFuelMinutesLeft = (int)Math.Floor(currentGameState.FuelData.FuelLeft / averageUsagePerMinute);

                    if (enableFuelMessages && currentGameState.FuelData.FuelLeft / averageUsagePerMinute < 2 && !playedTwoMinutesRemaining)
                    {
                        playedTwoMinutesRemaining = true;
                        playedFiveMinutesRemaining = true;
                        playedTenMinutesRemaining = true;
                        audioPlayer.queueClip(folderTwoMinutesFuel, 0, this);
                    }
                    else if (enableFuelMessages && currentGameState.FuelData.FuelLeft / averageUsagePerMinute < 5 && !playedFiveMinutesRemaining)
                    {
                        playedFiveMinutesRemaining = true;
                        playedTenMinutesRemaining = true;
                        audioPlayer.queueClip(folderFiveMinutesFuel, 0, this);
                    }
                    else if (enableFuelMessages && currentGameState.FuelData.FuelLeft / averageUsagePerMinute < 10 && !playedTenMinutesRemaining)
                    {
                        playedTenMinutesRemaining = true;
                        audioPlayer.queueClip(folderTenMinutesFuel, 0, this);
                    }
                }
                else if (enableFuelMessages && initialised && !playedHalfTankWarning && currentGameState.FuelData.FuelLeft / fuelAfter15Seconds <= 0.50)
                {
                    // warning message for fuel left - these play as soon as the fuel reaches 1/2 tank left
                    playedHalfTankWarning = true;
                    audioPlayer.queueClip(folderHalfTankWarning, 0, this);
                }
            }
        }

        public override void respond(String voiceMessage)
        {
            Boolean haveData = false;
            if (initialised && currentFuel > -1)
            {
                if (averageUsagePerLap > 0)
                {
                    int lapsOfFuelLeft = (int)Math.Floor(currentFuel / averageUsagePerLap);
                    if (lapsOfFuelLeft > 60)
                    {
                        audioPlayer.openChannel();
                        audioPlayer.playClipImmediately(folderPlentyOfFuel, new QueuedMessage(0, this));
                        audioPlayer.closeChannel();
                    }
                    else
                    {
                        List<String> messages = new List<String>();
                        messages.Add(folderWeEstimate);
                        messages.Add(QueuedMessage.folderNameNumbersStub + lapsOfFuelLeft);
                        messages.Add(folderLapsRemaining);
                        audioPlayer.openChannel();
                        audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "Fuel/estimate",
                            new QueuedMessage(messages, 0, this));
                        audioPlayer.closeChannel();
                    }                    
                    haveData = true;
                }
                else if (averageUsagePerMinute > 0)
                {
                    int minutesOfFuelLeft = (int)Math.Floor(currentFuel / averageUsagePerMinute);
                    if (minutesOfFuelLeft > 60) {
                        audioPlayer.openChannel();
                        audioPlayer.playClipImmediately(folderPlentyOfFuel, new QueuedMessage(0, this));
                        audioPlayer.closeChannel();
                    }
                    else 
                    {
                        List<String> messages = new List<String>();
                        messages.Add(folderWeEstimate);
                        messages.Add(QueuedMessage.folderNameNumbersStub + minutesOfFuelLeft);
                        messages.Add(folderMinutesRemaining);
                        audioPlayer.openChannel();
                        audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "Fuel/estimate",
                            new QueuedMessage(messages, 0, this));
                        audioPlayer.closeChannel();
                    }                    
                    haveData = true;
                }
            }
            if (!haveData)
            {
                audioPlayer.openChannel();
                if (!fuelUseActive)
                {
                    audioPlayer.playClipImmediately(folderPlentyOfFuel, new QueuedMessage(0, this));
                }
                else
                {
                    audioPlayer.playClipImmediately(AudioPlayer.folderNoData, new QueuedMessage(0, this));
                }
                audioPlayer.closeChannel();
            }
        }
    }
}
