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

        public override List<SessionType> applicableSessionTypes
        {
            get { return new List<SessionType> { SessionType.Race }; }
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
                            if (currentGameState.PitData.IsRefuellingAllowed) 
                            {
                                audioPlayer.queueClip(new QueuedMessage(RaceTime.folderHalfWayHome, 0, this));
                                if (estimatedFuelLapsLeft < 60)
                                {
                                    audioPlayer.queueClip(new QueuedMessage("Fuel/estimate", MessageContents(folderWeEstimate, 
                                        QueuedMessage.folderNameNumbersStub + estimatedFuelLapsLeft, folderLapsRemaining), 0, this));
                                }
                            }
                            else
                            {
                                audioPlayer.queueClip(new QueuedMessage(folderHalfDistanceLowFuel, 0, this));
                            }
                        }
                        else
                        {
                            audioPlayer.queueClip(new QueuedMessage(folderHalfDistanceGoodFuel, 0, this));
                        }
                    }
                    else if (enableFuelMessages && estimatedFuelLapsLeft == 4)
                    {
                        Console.WriteLine("4 laps fuel left, starting fuel = " + fuelAfter15Seconds +
                                ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.queueClip(new QueuedMessage(folderFourLapsEstimate, 0, this));
                    }
                    else if (enableFuelMessages && estimatedFuelLapsLeft == 3)
                    {
                        Console.WriteLine("3 laps fuel left, starting fuel = " + fuelAfter15Seconds +
                            ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.queueClip(new QueuedMessage(folderThreeLapsEstimate, 0, this));
                    }
                    else if (enableFuelMessages && estimatedFuelLapsLeft == 2)
                    {
                        Console.WriteLine("2 laps fuel left, starting fuel = " + fuelAfter15Seconds +
                            ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.queueClip(new QueuedMessage(folderTwoLapsEstimate, 0, this));
                    }
                    else if (enableFuelMessages && estimatedFuelLapsLeft == 1)
                    {
                        Console.WriteLine("1 lap fuel left, starting fuel = " + fuelAfter15Seconds +
                            ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.queueClip(new QueuedMessage(folderOneLapEstimate, 0, this));
                    }
                }
                else if (initialised && currentGameState.SessionData.SessionNumberOfLaps <= 0 && !playedHalfTimeFuelEstimate &&
                    currentGameState.SessionData.SessionTimeRemaining <= halfTime && currentGameState.SessionData.SessionTimeRemaining > halfTime - 30 && averageUsagePerMinute > 0)
                {
                    Console.WriteLine("Half race distance. Fuel in tank = " + currentGameState.FuelData.FuelLeft + ", average usage per minute = " + averageUsagePerMinute);
                    playedHalfTimeFuelEstimate = true;
                    if (enableFuelMessages)
                    {
                        if (averageUsagePerMinute * halfTime / 60 > currentGameState.FuelData.FuelLeft && currentGameState.FuelData.FuelLeft / fuelAfter15Seconds < 0.6)
                        {
                            if (currentGameState.PitData.IsRefuellingAllowed)
                            {
                                int minutesLeft = (int)Math.Floor(currentGameState.FuelData.FuelLeft / averageUsagePerMinute);
                                audioPlayer.queueClip(new QueuedMessage(RaceTime.folderHalfWayHome, 0, this));
                                if (minutesLeft < 60)
                                {
                                    audioPlayer.queueClip(new QueuedMessage("Fuel/estimate", MessageContents(
                                        folderWeEstimate, QueuedMessage.folderNameNumbersStub + minutesLeft, folderMinutesRemaining), 0, this));
                                }
                            }
                            else
                            {
                                audioPlayer.queueClip(new QueuedMessage(folderHalfDistanceLowFuel, 0, this));
                            }
                        }
                        else
                        {
                            audioPlayer.queueClip(new QueuedMessage(folderHalfDistanceGoodFuel, 0, this));
                        }
                    }
                }
                else if (initialised && currentGameState.SessionData.SessionNumberOfLaps <= 0 && 
                    currentGameState.SessionData.SessionRunningTime > gameTimeAtLastFuelWindowUpdate + (60 * fuelUseSampleTime))
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
                        audioPlayer.queueClip(new QueuedMessage(folderTwoMinutesFuel, 0, this));
                    }
                    else if (enableFuelMessages && currentGameState.FuelData.FuelLeft / averageUsagePerMinute < 5 && !playedFiveMinutesRemaining)
                    {
                        playedFiveMinutesRemaining = true;
                        playedTenMinutesRemaining = true;
                        audioPlayer.queueClip(new QueuedMessage(folderFiveMinutesFuel, 0, this));
                    }
                    else if (enableFuelMessages && currentGameState.FuelData.FuelLeft / averageUsagePerMinute < 10 && !playedTenMinutesRemaining)
                    {
                        playedTenMinutesRemaining = true;
                        audioPlayer.queueClip(new QueuedMessage(folderTenMinutesFuel, 0, this));
                    }
                }
                else if (enableFuelMessages && initialised && !playedHalfTankWarning && currentGameState.FuelData.FuelLeft / fuelAfter15Seconds <= 0.50)
                {
                    // warning message for fuel left - these play as soon as the fuel reaches 1/2 tank left
                    playedHalfTankWarning = true;
                    audioPlayer.queueClip(new QueuedMessage(folderHalfTankWarning, 0, this));
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
                        audioPlayer.playClipImmediately(new QueuedMessage(folderPlentyOfFuel, 0, this));
                        audioPlayer.closeChannel();
                    }
                    else
                    {
                        audioPlayer.openChannel();
                        audioPlayer.playClipImmediately(new QueuedMessage("Fuel/estimate",
                            MessageContents(folderWeEstimate, QueuedMessage.folderNameNumbersStub + lapsOfFuelLeft, folderLapsRemaining), 0, this));
                        audioPlayer.closeChannel();
                    }                    
                    haveData = true;
                }
                else if (averageUsagePerMinute > 0)
                {
                    int minutesOfFuelLeft = (int)Math.Floor(currentFuel / averageUsagePerMinute);
                    if (minutesOfFuelLeft > 60) {
                        audioPlayer.openChannel();
                        audioPlayer.playClipImmediately(new QueuedMessage(folderPlentyOfFuel, 0, this));
                        audioPlayer.closeChannel();
                    }
                    else 
                    {
                        audioPlayer.openChannel();
                        audioPlayer.playClipImmediately(new QueuedMessage("Fuel/estimate",
                            MessageContents(folderWeEstimate, QueuedMessage.folderNameNumbersStub + minutesOfFuelLeft, folderMinutesRemaining), 0, this));
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
                    audioPlayer.playClipImmediately(new QueuedMessage(folderPlentyOfFuel, 0, this));
                }
                else
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, this));
                }
                audioPlayer.closeChannel();
            }
        }
    }
}
