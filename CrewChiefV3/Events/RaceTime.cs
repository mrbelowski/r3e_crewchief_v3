using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.Events;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    class RaceTime : AbstractEvent
    {
        // TODO: separate position & time remaining from "push push push" and "ease off and bring it home safely" messages
        private String folder5mins = "race_time/five_minutes_left";
        private String folder5minsLeading = "race_time/five_minutes_left_leading";
        private String folder5minsPodium = "race_time/five_minutes_left_podium";
        private String folder2mins = "race_time/two_minutes_left";
        // TODO: 2 minutes remaining messages
        //TODO: separate messages depending on the gap
        private String folder10mins = "race_time/ten_minutes_left";
        private String folder15mins = "race_time/fifteen_minutes_left";
        private String folder20mins = "race_time/twenty_minutes_left";
        private String folderHalfWayHome = "race_time/half_way";
        private String folderLastLap = "race_time/last_lap";
        private String folderLastLapLeading = "race_time/last_lap_leading";
        private String folderLastLapPodium = "race_time/last_lap_top_three";

        private String folderMinutesLeft = "race_time/minutes_remaining";
        private String folderLapsLeft = "race_time/laps_remaining";

        private String folderLessThanOneMinute = "race_time/less_than_one_minute";

        private String folderThisIsTheLastLap = "race_time/this_is_the_last_lap";

        private String folderOneMinuteRemaining = "race_time/one_minute_remaining";

        private String folderOneLapAfterThisOne = "race_time/one_more_lap_after_this_one";

        private Boolean played2mins, played5mins, played10mins, played15mins, played20mins, playedHalfWayHome, playedLastLap;

        private float halfTime;

        private Boolean gotHalfTime;

        private int lapsLeft;
        private float timeLeft;

        private Boolean leaderHasFinishedRace;

        private Boolean sessionLengthIsTime;

        public RaceTime(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            played2mins = false; played5mins = false; played10mins = false; played15mins = false;
            played20mins = false; playedHalfWayHome = false; playedLastLap = false;
            halfTime = 0;
            gotHalfTime = false;
            lapsLeft = -1;
            timeLeft = 0;
            sessionLengthIsTime = false;
            leaderHasFinishedRace = false;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            leaderHasFinishedRace = currentGameState.SessionData.LeaderHasFinishedRace;
            timeLeft = currentGameState.SessionData.SessionTimeRemaining;
            if (currentGameState.SessionData.SessionNumberOfLaps > 0)
            {
                lapsLeft = currentGameState.SessionData.SessionNumberOfLaps - currentGameState.SessionData.CompletedLaps;
                sessionLengthIsTime = false;
            }
            else
            {
                sessionLengthIsTime = true;
            }
            if (sessionLengthIsTime)
            {
                if (!gotHalfTime)
                {
                    Console.WriteLine("Session time remaining = " + timeLeft);
                    halfTime = timeLeft / 2;
                    gotHalfTime = true;
                    if (currentGameState.FuelData.FuelUseActive)
                    {
                        // don't allow the half way message to play if fuel use is active - there's already one in there
                        playedHalfWayHome = true;
                    }
                }
                PearlsOfWisdom.PearlType pearlType = PearlsOfWisdom.PearlType.NONE;
                if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.CompletedLaps > 1)
                {
                    pearlType = PearlsOfWisdom.PearlType.NEUTRAL;
                    if (currentGameState.SessionData.Position < 4)
                    {
                        pearlType = PearlsOfWisdom.PearlType.GOOD;
                    }
                    else if (currentGameState.SessionData.Position > 10)
                    {
                        pearlType = PearlsOfWisdom.PearlType.BAD;
                    }
                }

                // this event only works if we're leading because we don't know when the leader 
                // crosses the line :(
                if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.IsNewLap &&
                    currentGameState.SessionData.SessionRunningTime > 60 && !playedLastLap &&
                    currentGameState.SessionData.Position == 1 && timeLeft > 0 && timeLeft < currentGameState.SessionData.LapTimeBest)
                {
                    playedLastLap = true;
                    played2mins = true;
                    played5mins = true;
                    played10mins = true;
                    played15mins = true;
                    played20mins = true;
                    playedHalfWayHome = true;
                    if (currentGameState.SessionData.Position == 1)
                    {
                        // don't add a pearl here - the audio clip already contains encouragement
                        audioPlayer.queueClip(folderLastLapLeading, 0, this, pearlType, 0);
                    }
                    else if (currentGameState.SessionData.Position < 4)
                    {
                        // don't add a pearl here - the audio clip already contains encouragement
                        audioPlayer.queueClip(folderLastLapPodium, 0, this, pearlType, 0);
                    }
                    else
                    {
                        audioPlayer.queueClip(folderLastLap, 0, this, pearlType, 0.7);
                    }
                }
                if (currentGameState.SessionData.SessionRunningTime > 60 && !played2mins && timeLeft / 60 < 2 && timeLeft / 60 > 1.9)
                {
                    played2mins = true;
                    played5mins = true;
                    played10mins = true;
                    played15mins = true;
                    played20mins = true;
                    playedHalfWayHome = true;
                    audioPlayer.queueClip(folder2mins, 0, this, PearlsOfWisdom.PearlType.NONE, 0);
                } if (currentGameState.SessionData.SessionRunningTime > 60 && !played5mins && timeLeft / 60 < 5 && timeLeft / 60 > 4.9)
                {
                    played5mins = true;
                    played10mins = true;
                    played15mins = true;
                    played20mins = true;
                    playedHalfWayHome = true;
                    if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.Position == 1)
                    {
                        // don't add a pearl here - the audio clip already contains encouragement
                        audioPlayer.queueClip(folder5minsLeading, 0, this, pearlType, 0);
                    }
                    else if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.Position < 4)
                    {
                        // don't add a pearl here - the audio clip already contains encouragement
                        audioPlayer.queueClip(folder5minsPodium, 0, this, pearlType, 0);
                    }
                    else
                    {
                        audioPlayer.queueClip(folder5mins, 0, this, pearlType, 0.7);
                    }
                }
                if (currentGameState.SessionData.SessionRunningTime > 60 && !played10mins && timeLeft / 60 < 10 && timeLeft / 60 > 9.9)
                {
                    played10mins = true;
                    played15mins = true;
                    played20mins = true;
                    audioPlayer.queueClip(folder10mins, 0, this, pearlType, 0.7);
                }
                if (currentGameState.SessionData.SessionRunningTime > 60 && !played15mins && timeLeft / 60 < 15 && timeLeft / 60 > 14.9)
                {
                    played15mins = true;
                    played20mins = true;
                    audioPlayer.queueClip(folder15mins, 0, this, pearlType, 0.7);
                }
                if (currentGameState.SessionData.SessionRunningTime > 60 && !played20mins && timeLeft / 60 < 20 && timeLeft / 60 > 19.9)
                {
                    played20mins = true;
                    audioPlayer.queueClip(folder20mins, 0, this, pearlType, 0.7);
                }
                else if (currentGameState.SessionData.SessionType == SessionType.Race &&
                    currentGameState.SessionData.SessionRunningTime > 60 && !playedHalfWayHome && timeLeft > 0 && timeLeft < halfTime)
                {
                    // this one sounds weird in practice and qual sessions, so skip it
                    playedHalfWayHome = true;
                    audioPlayer.queueClip(folderHalfWayHome, 0, this, pearlType, 0.7);
                }
            }
        }

        public override void respond(string voiceMessage)
        {
            // TODO: handle times and laps > 60 - maybe just use "lots" and "ages"...
            if (sessionLengthIsTime)
            {
                if (leaderHasFinishedRace)
                {
                    Console.WriteLine("Playing last lap message, timeleft = " + timeLeft);
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderThisIsTheLastLap, new QueuedMessage(0, this));
                    audioPlayer.closeChannel();
                }
                else if (timeLeft >= 3600)
                {
                    Console.WriteLine("Unable to process times higher than 59 minutes in this version...");
                }
                else if (timeLeft >= 120)
                {
                    TimeSpan timeLeftTimeSpan = TimeSpan.FromSeconds(timeLeft);
                    List<String> messages = new List<String>();
                    messages.Add(QueuedMessage.folderNameNumbersStub + timeLeftTimeSpan.Minutes);
                    messages.Add(folderMinutesLeft);
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "RaceTime/time_remaining",
                        new QueuedMessage(messages, 0, this));
                    audioPlayer.closeChannel();
                }
                else if (timeLeft >= 60)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderOneMinuteRemaining, new QueuedMessage(0, this));
                    audioPlayer.closeChannel();
                }
                else if (timeLeft <= 0)
                {
                    // TODO: check these - if the timeLeft value contains -1 for some reason this message will be wrong
                    Console.WriteLine("Playing last lap message, timeleft = " + timeLeft);
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderThisIsTheLastLap, new QueuedMessage(0, this));
                    audioPlayer.closeChannel();
                }
                else if (timeLeft < 60)
                {
                    // TODO: check these - if the timeLeft value contains -1 for some reason this message will be wrong
                    Console.WriteLine("Playing less than a minute message, timeleft = " + timeLeft);
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderLessThanOneMinute, new QueuedMessage(0, this));
                    audioPlayer.closeChannel();
                }
            }
            else
            {
                if (lapsLeft > 59)
                {
                    Console.WriteLine("Unable to process numbers higher than 59 in this version...");
                }
                else if (lapsLeft > 1)
                {
                    List<String> messages = new List<String>();
                    messages.Add(QueuedMessage.folderNameNumbersStub + lapsLeft);
                    messages.Add(folderLapsLeft);
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "RaceTime/laps_remaining",
                        new QueuedMessage(messages, 0, this));

                    audioPlayer.closeChannel();
                }
                else if (lapsLeft == 1)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderOneLapAfterThisOne, new QueuedMessage(0, this));
                    audioPlayer.closeChannel();
                }
                else if (lapsLeft == 0)
                {
                    audioPlayer.openChannel();
                    audioPlayer.playClipImmediately(folderThisIsTheLastLap, new QueuedMessage(0, this));
                    audioPlayer.closeChannel();
                }
            }     
        }
    }
}
