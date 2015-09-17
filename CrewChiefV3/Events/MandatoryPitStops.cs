using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    class MandatoryPitStops : AbstractEvent
    {
        private String folderMandatoryPitStopsOptionsToPrimes = "mandatory_pit_stops/options_to_primes";

        private String folderMandatoryPitStopsPrimesToOptions = "mandatory_pit_stops/primes_to_options";

        private String folderMandatoryPitStopsPitWindowOpening = "mandatory_pit_stops/pit_window_opening";

        private String folderMandatoryPitStopsPitWindowOpen1Min = "mandatory_pit_stops/pit_window_opens_1_min";

        private String folderMandatoryPitStopsPitWindowOpen2Min = "mandatory_pit_stops/pit_window_opens_2_min";

        private String folderMandatoryPitStopsPitWindowOpen = "mandatory_pit_stops/pit_window_open";

        private String folderMandatoryPitStopsPitWindowCloses1min = "mandatory_pit_stops/pit_window_closes_1_min";

        private String folderMandatoryPitStopsPitWindowCloses2min = "mandatory_pit_stops/pit_window_closes_2_min";

        private String folderMandatoryPitStopsPitWindowClosing = "mandatory_pit_stops/pit_window_closing";

        private String folderMandatoryPitStopsPitWindowClosed = "mandatory_pit_stops/pit_window_closed";

        public static String folderMandatoryPitStopsPitThisLap = "mandatory_pit_stops/pit_this_lap";

        private String folderMandatoryPitStopsPitThisLapTooLate = "mandatory_pit_stops/pit_this_lap_too_late";

        private String folderMandatoryPitStopsPitNow = "mandatory_pit_stops/pit_now";

        // for voice responses
        private String folderMandatoryPitStopsYesStopOnLap = "mandatory_pit_stops/yes_stop_on_lap";
        private String folderMandatoryPitStopsYesStopAfter = "mandatory_pit_stops/yes_stop_after";
        private String folderMandatoryPitStopsMinutes = "mandatory_pit_stops/minutes";
        private String folderMandatoryPitStopsMissedStop = "mandatory_pit_stops/missed_stop";


        private int pitWindowOpenLap;

        private int pitWindowClosedLap;

        private int pitWindowOpenTime;

        private int pitWindowClosedTime;

        private Boolean pitDataInitialised;

        private Boolean onOptions;

        private Boolean onPrimes;

        private int tyreChangeLap;

        private Boolean playBoxNowMessage;

        private Boolean playOpenNow;

        private Boolean play1minOpenWarning;

        private Boolean play2minOpenWarning;

        private Boolean playClosedNow;

        private Boolean play1minCloseWarning;

        private Boolean play2minCloseWarning;

        private Boolean playPitThisLap;

        private Boolean mandatoryStopRequired;

        private Boolean mandatoryStopCompleted;

        private Boolean mandatoryStopBoxThisLap;

        private Boolean mandatoryStopMissed;
        
        public MandatoryPitStops(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            pitWindowOpenLap = 0;
            pitWindowClosedLap = 0;
            pitWindowOpenTime = 0;
            pitWindowClosedTime = 0;
            pitDataInitialised = false;
            onOptions = false;
            onPrimes = false;
            tyreChangeLap = 0;
            playBoxNowMessage = false;
            play2minOpenWarning = false;
            play2minCloseWarning = false;
            play1minOpenWarning = false;
            play1minCloseWarning = false;
            playClosedNow = false;
            playOpenNow = false;
            playPitThisLap = false;
            mandatoryStopRequired = false;
            mandatoryStopCompleted = false;
            mandatoryStopBoxThisLap = false;
            mandatoryStopMissed = false;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.HasMandatoryPitStop)
            {                
                if (!pitDataInitialised)
                {
                    mandatoryStopCompleted = false;
                    mandatoryStopRequired = true;
                    mandatoryStopBoxThisLap = false;
                    mandatoryStopMissed = false;
                    Console.WriteLine("pit start = " + currentGameState.SessionData.PitWindowStart + ", pit end = " + currentGameState.SessionData.PitWindowEnd);
                    if (currentGameState.SessionData.SessionNumberOfLaps > 0)
                    {
                        pitWindowOpenLap = currentGameState.SessionData.PitWindowStart;
                        pitWindowClosedLap = currentGameState.SessionData.PitWindowEnd;
                        // DTM specific stuff...
                        if (currentGameState.TyreData.HasMatchedTyreTypes && currentGameState.TyreData.FrontLeftTyreType == TyreType.Option)
                        {
                            onOptions = true;
                            // when we've completed half distance - 1 laps, we need to come in at the end of the current lap
                            tyreChangeLap = (int)Math.Floor((double)currentGameState.SessionData.SessionNumberOfLaps / 2d) - 1;
                        }
                        else if (currentGameState.TyreData.HasMatchedTyreTypes && currentGameState.TyreData.FrontLeftTyreType == TyreType.Prime)
                        {
                            onPrimes = true;
                            tyreChangeLap = (int)Math.Floor((double)currentGameState.SessionData.SessionNumberOfLaps / 2d);
                        }
                        playPitThisLap = true;
                    }
                    else if (currentGameState.SessionData.SessionTimeRemaining > 0)
                    {
                        pitWindowOpenTime = currentGameState.SessionData.PitWindowStart;
                        pitWindowClosedTime = currentGameState.SessionData.PitWindowEnd;
                        play2minOpenWarning = true;
                        play1minOpenWarning = true;
                        playOpenNow = true;
                        play2minCloseWarning = true;
                        play1minCloseWarning = true;
                        playClosedNow = true;
                        playPitThisLap = true;
                    }
                    else
                    {
                        Console.WriteLine("Error getting pit data");
                    }
                    pitDataInitialised = true;
                }
                else if (currentGameState.PitData.IsMakingMandatoryPitStop)
                {
                    playPitThisLap = false;
                    playBoxNowMessage = false;
                    mandatoryStopCompleted = true;
                    mandatoryStopBoxThisLap = false;
                    mandatoryStopMissed = false;
                }
                else
                {
                    if (currentGameState.SessionData.IsNewLap && currentGameState.SessionData.CompletedLaps > 0 && currentGameState.SessionData.SessionNumberOfLaps > 0)
                    {
                        if (currentGameState.PitData.PitWindow != PitWindow.StopInProgress &&
                            currentGameState.PitData.PitWindow != PitWindow.Completed &&
                            currentGameState.SessionData.CompletedLaps == tyreChangeLap && playPitThisLap)
                        {
                            playBoxNowMessage = true;
                            playPitThisLap = false;
                            mandatoryStopBoxThisLap = true;
                            if (onOptions)
                            {
                                audioPlayer.queueClip(folderMandatoryPitStopsOptionsToPrimes, 0, this);
                            }
                            else if (onPrimes)
                            {
                                audioPlayer.queueClip(folderMandatoryPitStopsPrimesToOptions, 0, this);
                            }
                            else
                            {
                                audioPlayer.queueClip(folderMandatoryPitStopsPitThisLap, 0, this);
                            }
                        }
                        else if (currentGameState.SessionData.CompletedLaps == pitWindowOpenLap - 1)
                        {
                            // note this is a 'pit window opens at the end of this lap' message, 
                            // so we play it 1 lap before the window opens
                            audioPlayer.queueClip(folderMandatoryPitStopsPitWindowOpening, 0, this);
                        }
                        else if (currentGameState.SessionData.CompletedLaps == pitWindowOpenLap)
                        {
                            audioPlayer.setBackgroundSound(AudioPlayer.dtmPitWindowOpenBackground);
                            audioPlayer.queueClip(folderMandatoryPitStopsPitWindowOpen, 0, this);
                        }
                        else if (currentGameState.SessionData.CompletedLaps == pitWindowClosedLap - 1)
                        {
                            audioPlayer.queueClip(folderMandatoryPitStopsPitWindowClosing, 0, this);
                            if (currentGameState.PitData.PitWindow != PitWindow.Completed &&
                                currentGameState.PitData.PitWindow != PitWindow.StopInProgress)
                            {
                                playBoxNowMessage = true;
                                audioPlayer.queueClip(folderMandatoryPitStopsPitThisLap, 0, this);
                            }
                        }
                        else if (currentGameState.SessionData.CompletedLaps == pitWindowClosedLap)
                        {
                            mandatoryStopBoxThisLap = false;
                            if (currentGameState.PitData.PitWindow != PitWindow.Completed)
                            {
                                mandatoryStopMissed = true;
                            }
                            audioPlayer.setBackgroundSound(AudioPlayer.dtmPitWindowClosedBackground);
                            audioPlayer.queueClip(folderMandatoryPitStopsPitWindowClosed, 0, this);                            
                        }
                    }
                    else if (currentGameState.SessionData.IsNewLap && currentGameState.SessionData.CompletedLaps > 0 && currentGameState.SessionData.SessionTimeRemaining > 0)
                    {
                        if (currentGameState.PitData.PitWindow != PitWindow.StopInProgress &&
                            currentGameState.PitData.PitWindow != PitWindow.Completed &&
                            currentGameState.SessionData.SessionRunTime - currentGameState.SessionData.SessionTimeRemaining > pitWindowOpenTime * 60 &&
                            currentGameState.SessionData.SessionRunTime - currentGameState.SessionData.SessionTimeRemaining < pitWindowClosedTime * 60)
                        {
                            double timeLeftToPit = pitWindowClosedTime * 60 - (currentGameState.SessionData.SessionRunTime - currentGameState.SessionData.SessionTimeRemaining);
                            if (playPitThisLap && currentGameState.SessionData.LapTimeBest + 10 > timeLeftToPit)
                            {
                                // oh dear, we might have missed the pit window.
                                playBoxNowMessage = true;
                                playPitThisLap = false;
                                mandatoryStopBoxThisLap = true;
                                audioPlayer.queueClip(folderMandatoryPitStopsPitThisLapTooLate, 0, this);
                            }
                            else if (playPitThisLap && currentGameState.SessionData.LapTimeBest + 10 < timeLeftToPit &&
                                (currentGameState.SessionData.LapTimeBest * 2) + 10 > timeLeftToPit)
                            {
                                // we probably won't make it round twice - pit at the end of this lap
                                playBoxNowMessage = true;
                                playPitThisLap = false;
                                mandatoryStopBoxThisLap = true;
                                audioPlayer.queueClip(folderMandatoryPitStopsPitThisLap, 0, this);
                            }
                        }
                    }
                    if (playOpenNow && currentGameState.SessionData.SessionTimeRemaining > 0 &&
                        (currentGameState.SessionData.SessionRunTime - currentGameState.SessionData.SessionTimeRemaining > (pitWindowOpenTime * 60) ||
                        currentGameState.PitData.PitWindow == PitWindow.Open))
                    {
                        playOpenNow = false;
                        play1minOpenWarning = false;
                        play2minOpenWarning = false;
                        audioPlayer.queueClip(folderMandatoryPitStopsPitWindowOpen, 0, this);
                    }
                    else if (play1minOpenWarning && currentGameState.SessionData.SessionTimeRemaining > 0 &&
                        currentGameState.SessionData.SessionRunTime - currentGameState.SessionData.SessionTimeRemaining > ((pitWindowOpenTime - 1) * 60))
                    {
                        play1minOpenWarning = false;
                        play2minOpenWarning = false;
                        audioPlayer.queueClip(folderMandatoryPitStopsPitWindowOpen1Min, 0, this);
                    }
                    else if (play2minOpenWarning && currentGameState.SessionData.SessionTimeRemaining > 0 &&
                        currentGameState.SessionData.SessionRunTime - currentGameState.SessionData.SessionTimeRemaining > ((pitWindowOpenTime - 2) * 60))
                    {
                        play2minOpenWarning = false;
                        audioPlayer.queueClip(folderMandatoryPitStopsPitWindowOpen2Min, 0, this);
                    }
                    else if (playClosedNow && currentGameState.SessionData.SessionTimeRemaining > 0 &&
                        currentGameState.SessionData.SessionRunTime - currentGameState.SessionData.SessionTimeRemaining > (pitWindowClosedTime * 60))
                    {
                        playClosedNow = false;
                        playBoxNowMessage = false;
                        play1minCloseWarning = false;
                        play2minCloseWarning = false;
                        playPitThisLap = false;
                        mandatoryStopBoxThisLap = false;
                        if (currentGameState.PitData.PitWindow != PitWindow.Completed)
                        {
                            mandatoryStopMissed = true;
                        }
                        audioPlayer.queueClip(folderMandatoryPitStopsPitWindowClosed, 0, this);
                    }
                    else if (play1minCloseWarning && currentGameState.SessionData.SessionTimeRemaining > 0 &&
                        currentGameState.SessionData.SessionRunTime - currentGameState.SessionData.SessionTimeRemaining > ((pitWindowClosedTime - 1) * 60))
                    {
                        play1minCloseWarning = false;
                        play2minCloseWarning = false;
                        audioPlayer.queueClip(folderMandatoryPitStopsPitWindowCloses1min, 0, this);
                    }
                    else if (play2minCloseWarning && currentGameState.SessionData.SessionTimeRemaining > 0 &&
                        currentGameState.SessionData.SessionRunTime - currentGameState.SessionData.SessionTimeRemaining > ((pitWindowClosedTime - 2) * 60))
                    {
                        play2minCloseWarning = false;
                        audioPlayer.queueClip(folderMandatoryPitStopsPitWindowCloses2min, 0, this);
                    }

                    if (playBoxNowMessage && currentGameState.SessionData.SectorNumber == 3)
                    {
                        playBoxNowMessage = false;
                        audioPlayer.queueClip(folderMandatoryPitStopsPitNow, 3, this);
                    }
                }
            }
        }

        public override void respond(String voiceMessage)
        {
            if (!mandatoryStopRequired || mandatoryStopCompleted)
            {
                audioPlayer.openChannel();
                audioPlayer.playClipImmediately(AudioPlayer.folderNo, new QueuedMessage(0, null));
                audioPlayer.closeChannel();
            }
            else if (mandatoryStopMissed)
            {
                audioPlayer.openChannel();
                audioPlayer.playClipImmediately(folderMandatoryPitStopsMissedStop, new QueuedMessage(0, null));
                audioPlayer.closeChannel();
            }
            else if (mandatoryStopBoxThisLap)
            {
                List<String> messages = new List<String>();
                messages.Add(AudioPlayer.folderYes);
                messages.Add(folderMandatoryPitStopsPitThisLap);
                audioPlayer.openChannel();
                audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "_yesBoxThisLap", new QueuedMessage(messages, 0, null));
                audioPlayer.closeChannel();
            }
            else if (pitWindowOpenLap > 0)
            {
                List<String> messages = new List<String>();
                messages.Add(folderMandatoryPitStopsYesStopOnLap);
                messages.Add(QueuedMessage.folderNameNumbersStub + pitWindowOpenLap);
                audioPlayer.openChannel();
                audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "_yesBoxOnLap", new QueuedMessage(messages, 0, null));
                audioPlayer.closeChannel();
            }
            else if (pitWindowOpenTime > 0)
            {
                List<String> messages = new List<String>();
                messages.Add(folderMandatoryPitStopsYesStopAfter);
                messages.Add(QueuedMessage.folderNameNumbersStub + pitWindowOpenTime);
                messages.Add(folderMandatoryPitStopsMinutes);
                audioPlayer.openChannel();
                audioPlayer.playClipImmediately(QueuedMessage.compoundMessageIdentifier + "_yesBoxAfter", new QueuedMessage(messages, 0, null));
                audioPlayer.closeChannel();
            }
        }
    }
}
