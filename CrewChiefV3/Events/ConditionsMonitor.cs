using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    class ConditionsMonitor : AbstractEvent
    {
        private Boolean enableTrackAndAirTempReports = UserSettings.GetUserSettings().getBoolean("enable_track_and_air_temp_reports");
        public static TimeSpan ConditionsSampleFrequency = TimeSpan.FromSeconds(20);

        // 3 minutes of data
        private int shortSampleWindow = 3 * 60 / ConditionsSampleFrequency.Seconds;
        // 8 minutes of data
        private int longSampleWindow = 8 * 60 / ConditionsSampleFrequency.Seconds;

        private float ambientTempChangeShortWindowReportThreshold = 1f;
        private float trackTempChangeShortWindowReportThreshold = 1.3f;

        private float ambientTempChangeLongWindowReportThreshold = 1.5f;
        private float trackTempChangeLongWindowReportThreshold = 2.3f;

        public static String folderTrackTempIsNow = "conditions/track_temperature_is_now";
        public static String folderAmbientTempIsNow = "conditions/ambient_temperature_is_now";
        public static String folderCelcius = "conditions/celcius";

        private int sampleCount;

        public ConditionsMonitor(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            sampleCount = 0;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            // 6 samples => 3 minutes of data
            if (!enableTrackAndAirTempReports || currentGameState.Conditions.samples.Count() < shortSampleWindow || sampleCount == currentGameState.Conditions.samples.Count())
            {
                return;
            }
            sampleCount = currentGameState.Conditions.samples.Count();
            CrewChiefV3.GameState.Conditions.ConditionsSample endSample = currentGameState.Conditions.samples[currentGameState.Conditions.samples.Count() - 1];
            CrewChiefV3.GameState.Conditions.ConditionsSample startSample = currentGameState.Conditions.samples[currentGameState.Conditions.samples.Count() - shortSampleWindow];
            float shortWindowAmbientDelta = endSample.AmbientTemperature - startSample.AmbientTemperature;
            if (Math.Abs(shortWindowAmbientDelta) > ambientTempChangeShortWindowReportThreshold)
            {
                Boolean increasing = shortWindowAmbientDelta > 0;
                // now check this trend is consistent
                int numberNotFollowingTrend = getNumberNotFollowingTrend(increasing, currentGameState.Conditions.samples, shortSampleWindow, true);
                if (numberNotFollowingTrend < 2)
                {
                    // report it
                    Console.WriteLine("short threshold ambient temp change: delta = " + shortWindowAmbientDelta);
                    audioPlayer.queueClip(new QueuedMessage("ambientTemp", MessageContents
                        (folderAmbientTempIsNow, QueuedMessage.folderNameNumbersStub + Math.Round(endSample.AmbientTemperature), folderCelcius), 0, this)); 
                }
            }

            float shortWindowTrackDelta = endSample.TrackTemperature - startSample.TrackTemperature;
            if (Math.Abs(shortWindowTrackDelta) > trackTempChangeShortWindowReportThreshold)
            {
                Boolean increasing = shortWindowTrackDelta > 0;
                // now check this trend is consistent
                int numberNotFollowingTrend = getNumberNotFollowingTrend(increasing, currentGameState.Conditions.samples, shortSampleWindow, false);
                if (numberNotFollowingTrend < 2)
                {
                    // report it
                    Console.WriteLine("short threshold track temp change: delta = " + shortWindowTrackDelta);
                    audioPlayer.queueClip(new QueuedMessage("trackTemp", MessageContents
                        (folderTrackTempIsNow, QueuedMessage.folderNameNumbersStub + Math.Round(endSample.TrackTemperature), folderCelcius), 0, this)); 
                }
            }

            if (currentGameState.Conditions.samples.Count() < longSampleWindow)
            {
                return;
            }
            startSample = currentGameState.Conditions.samples[currentGameState.Conditions.samples.Count() - longSampleWindow];

            float longWindowAmbientDelta = endSample.AmbientTemperature - startSample.AmbientTemperature;
            if (Math.Abs(longWindowAmbientDelta) > ambientTempChangeLongWindowReportThreshold)
            {
                Boolean increasing = longWindowAmbientDelta > 0;
                // now check this trend is consistent
                int numberNotFollowingTrend = getNumberNotFollowingTrend(increasing, currentGameState.Conditions.samples, longSampleWindow, true);
                if (numberNotFollowingTrend < 2)
                {
                    // report it
                    Console.WriteLine("long threshold ambient temp change: delta = " + longWindowAmbientDelta);
                    audioPlayer.queueClip(new QueuedMessage("ambientTemp", MessageContents
                        (folderAmbientTempIsNow, QueuedMessage.folderNameNumbersStub + Math.Round(endSample.AmbientTemperature), folderCelcius), 0, this)); 
                }
            }

            float longWindowTrackDelta = endSample.TrackTemperature - startSample.TrackTemperature;
            if (Math.Abs(longWindowTrackDelta) > trackTempChangeLongWindowReportThreshold)
            {
                Boolean increasing = longWindowTrackDelta > 0;
                // now check this trend is consistent
                int numberNotFollowingTrend = getNumberNotFollowingTrend(increasing, currentGameState.Conditions.samples, longSampleWindow, false);
                if (numberNotFollowingTrend < 2)
                {
                    // report it
                    Console.WriteLine("long threshold track temp change: delta = " + longWindowTrackDelta);
                    audioPlayer.queueClip(new QueuedMessage("trackTemp", MessageContents
                        (folderTrackTempIsNow, QueuedMessage.folderNameNumbersStub + Math.Round(endSample.TrackTemperature), folderCelcius), 0, this)); 
                }
            }     
        }

        private int getNumberNotFollowingTrend(Boolean increasing, List<GameState.Conditions.ConditionsSample> samples, int window, Boolean isAmbient) 
        {
            int numberOfPairsNotFollowingTrend = 0;
            // now check this trend is consistent
            for (int i = samples.Count() - 1; i > samples.Count() - window; i--)
            {
                float delta;
                if (isAmbient)
                {
                    delta = samples[i].AmbientTemperature - samples[i - 1].AmbientTemperature;
                }
                else
                {
                    delta = samples[i].TrackTemperature - samples[i - 1].TrackTemperature;
                }
                if ((increasing && delta < 0) | (!increasing && delta > 0))
                {
                    numberOfPairsNotFollowingTrend++;
                }
            }
            return numberOfPairsNotFollowingTrend;
        }
    }
}
