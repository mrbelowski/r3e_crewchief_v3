﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV3.Events;

namespace CrewChiefV3.PCars
{
    class PCarsSpotter : Spotter
    {
        private Boolean paused = false;

        // if the audio player is in the middle of another message, this 'immediate' message will have to wait.
        // If it's older than 1000 milliseconds by the time the player's got round to playing it, it's expired
        private int clearMessageExpiresAfter = 2000;
        private int clearAllRoundMessageExpiresAfter = 2000;
        private int holdMessageExpiresAfter = 1000;
        private int inTheMiddleMessageExpiresAfter = 1000;

        // how long is a car? we use 3.5 meters by default here. Too long and we'll get 'hold your line' messages
        // when we're clearly directly behind the car
        private float carLength = UserSettings.GetUserSettings().getFloat("pcars_spotter_car_length");

        // before saying 'clear', we need to be carLength + this value from the other car
        private float gapNeededForClear = UserSettings.GetUserSettings().getFloat("spotter_gap_for_clear");

        private float longCarLength;
        
        // don't play spotter messages if we're going < 10ms
        private float minSpeedForSpotterToOperate = UserSettings.GetUserSettings().getFloat("min_speed_for_spotter");

        // if the closing speed is > 5ms (about 12mph) then don't trigger spotter messages - 
        // this prevents them being triggered when passing stationary cars
        private float maxClosingSpeed = UserSettings.GetUserSettings().getFloat("max_closing_speed_for_spotter");

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        //private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");
        private int timeAfterRaceStartToActivate = 2;

        // say "still there" every 3 seconds
        private TimeSpan repeatHoldFrequency = TimeSpan.FromSeconds(UserSettings.GetUserSettings().getInt("spotter_hold_repeat_frequency"));

        private Boolean spotterOnlyWhenBeingPassed = UserSettings.GetUserSettings().getBoolean("spotter_only_when_being_passed");

        private Boolean hasCarLeft;
        private Boolean hasCarRight;

        private float trackWidth = 10f;

        private float carWidth = 1.7f;

        private String folderStillThere = "spotter/still_there";
        private String folderInTheMiddle = "spotter/in_the_middle";
        private String folderCarLeft = "spotter/car_left";
        private String folderCarRight = "spotter/car_right"; 
        private String folderClearLeft = "spotter/clear_left";
        private String folderClearRight = "spotter/clear_right";
        private String folderClearAllRound = "spotter/clear_all_round";

        // don't play 'clear' or 'hold' messages unless we've actually been clear or overlapping for some time
        private TimeSpan clearMessageDelay = TimeSpan.FromMilliseconds(UserSettings.GetUserSettings().getInt("spotter_clear_delay"));
        private TimeSpan overlapMessageDelay = TimeSpan.FromMilliseconds(UserSettings.GetUserSettings().getInt("spotter_overlap_delay"));

        private DateTime nextMessageDue = DateTime.Now;
        
        private Boolean enabled;

        private Boolean initialEnabledState;

        private DateTime timeWhenChannelShouldBeClosed;

        private TimeSpan timeToWaitBeforeClosingChannelLeftOpen = TimeSpan.FromMilliseconds(500);

        private Boolean channelLeftOpenTimerStarted = false;

        private TimeSpan maxTimeToKeepChannelOpenWhileReceivingUnusableData = TimeSpan.FromSeconds(2);
        
        private AudioPlayer audioPlayer;

        private NextMessageType nextMessageType;

        private DateTime timeToStartSpotting = DateTime.Now;

        private Boolean reportedOverlapLeft = false;

        private Boolean reportedOverlapRight = false;

        private enum Side {
            right, left, none
        }

        private enum NextMessageType
        {
            none, clearLeft, clearRight, clearAllRound, carLeft, carRight, threeWide, stillThere
        }

        public PCarsSpotter(AudioPlayer audioPlayer, Boolean initialEnabledState)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            this.longCarLength = carLength + gapNeededForClear;
        }

        public void clearState()
        {
            hasCarLeft = false;
            hasCarRight = false;
            timeWhenChannelShouldBeClosed = DateTime.Now;
            channelLeftOpenTimerStarted = false;
            nextMessageType = NextMessageType.none;
            timeToStartSpotting = DateTime.Now;
            this.reportedOverlapLeft = false;
            this.reportedOverlapRight = false;
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
                return;
            }
            pCarsAPIStruct lastState = ((CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)lastStateObj).data;
            pCarsAPIStruct currentState = ((CrewChiefV3.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)currentStateObj).data;
            DateTime now = DateTime.Now;

            if (currentState.mRaceState == (int)eRaceState.RACESTATE_RACING &&
                lastState.mRaceState != (int)eRaceState.RACESTATE_RACING)
            {
                timeToStartSpotting = now.Add(TimeSpan.FromSeconds(timeAfterRaceStartToActivate));
            }
            // this check looks a bit funky... whe we start a practice session, the raceState is no_started
            // until we cross the line for the first time. Which is retarded really.
            if (currentState.mRaceState == (int)eRaceState.RACESTATE_INVALID || now < timeToStartSpotting)
            {
                return;
            }

            float currentSpeed = currentState.mSpeed;
            float previousSpeed = lastState.mSpeed;
            // knock 10 metres off the track length to avoid farting about with positions wrapping relative to each other - 
            // we only use the track length here for belt n braces checking
            float trackLengthToUse = currentState.mTrackLength - 10;
            if (enabled && currentState.mParticipantData.Count() > 1 && currentState.mViewedParticipantIndex >= 0)
            {
                pCarsAPIParticipantStruct playerData = currentState.mParticipantData[currentState.mViewedParticipantIndex];
                Boolean playerIsInQuickNDirtyCheckZone = playerData.mCurrentLapDistance < trackLengthToUse;

                if (currentSpeed > minSpeedForSpotterToOperate && currentState.mPitMode == (uint) ePitMode.PIT_MODE_NONE)
                {
                    int carsOnLeft = 0;
                    int carsOnRight = 0;
                    for (int i = 0; i < currentState.mParticipantData.Count(); i++)
                    {
                        if (carsOnLeft >= 1 && carsOnRight >= 1)
                        {
                            // stop processing - we already know there's a car on both sides
                            break;
                        }
                        if (i != currentState.mViewedParticipantIndex)
                        {
                            //Console.WriteLine("speed = "+ currentState.mSpeed + " time ahead = " + currentState.mSplitTimeAhead + " time behind = " + currentState.mSplitTimeBehind);
                            pCarsAPIParticipantStruct opponentData = currentState.mParticipantData[i];
                            pCarsAPIParticipantStruct opponentDataLastState = lastState.mParticipantData[i];
                            if (opponentData.mIsActive && opponentData.mWorldPosition[0] != 0 && opponentData.mWorldPosition[2] != 0)
                            {
                                // if we think this opponent car isn't even on the same 10 metre chunk of track to us, ignore it
                                Boolean opponentIsInQuickNDirtyCheckZone = opponentData.mCurrentLapDistance < trackLengthToUse &&
                                    opponentDataLastState.mCurrentLapDistance < trackLengthToUse;
                                if (opponentIsInQuickNDirtyCheckZone) 
                                {
                                    if (playerIsInQuickNDirtyCheckZone &&
                                        Math.Abs(playerData.mCurrentLapDistance - opponentData.mCurrentLapDistance) > 10)
                                    {
                                        continue;
                                    }
                                    // this car is close to use, so check its speed relative to ours
                                    // this check looks sane enough, but the speed often comes out as zero, not sure why yet...
                                    /*float approxOpponentSpeed = 1000 * (opponentData.mCurrentLapDistance - opponentDataLastState.mCurrentLapDistance) / (float)CrewChief.spotterInterval.TotalMilliseconds;
                                    if (Math.Abs(currentSpeed - approxOpponentSpeed) > maxClosingSpeed)
                                    {
                                        continue;
                                    }*/
                                }
                                Side side = getSide(currentState.mOrientation[1], playerData.mWorldPosition, opponentData.mWorldPosition);
                                if (side == Side.left)
                                {
                                    carsOnLeft++;
                                }
                                else if (side == Side.right)
                                {
                                    carsOnRight++;
                                }
                            }                            
                        }                        
                    }
                    getNextMessage(carsOnLeft, carsOnRight, now);
                    playNextMessage(carsOnLeft, carsOnRight, now);
                    hasCarLeft = carsOnLeft > 0;
                    hasCarRight = carsOnRight > 0;
                }
                else if (hasCarLeft || hasCarRight)
                {
                    if (!channelLeftOpenTimerStarted)
                    {
                        timeWhenChannelShouldBeClosed = now.Add(timeToWaitBeforeClosingChannelLeftOpen);
                        channelLeftOpenTimerStarted = true;
                    }
                    if (now > timeWhenChannelShouldBeClosed)
                    {
                        Console.WriteLine("Closing channel left open in spotter");
                        timeWhenChannelShouldBeClosed = DateTime.MaxValue;
                        hasCarLeft = false;
                        hasCarRight = false;
                        audioPlayer.closeChannel();
                        channelLeftOpenTimerStarted = false;
                    }
                }
            }
        }
        
        // TODO: "clear all round" will never play here unless both sides go clear during the same update, which is really unlikely
        private void getNextMessage(int carsOnLeftCount, int carsOnRightCount, DateTime now)
        {
            if (carsOnLeftCount == 0 && carsOnRightCount == 0 && hasCarLeft && hasCarRight)
            {
                Console.WriteLine("clear all round");
                nextMessageType = NextMessageType.clearAllRound;
                nextMessageDue = now.Add(clearMessageDelay);
            }
            else if (carsOnLeftCount == 0 && hasCarLeft && ((carsOnRightCount == 0 && !hasCarRight) || (carsOnRightCount > 0 && hasCarRight)))
            {
                Console.WriteLine("clear left, carsOnLeftCount " + carsOnLeftCount + " carsOnRightCount " + carsOnRightCount + " hasCarLeft " + hasCarLeft + " hasCarRight " + hasCarRight);
                // just gone clear on the left - might still be a car right
                nextMessageType = NextMessageType.clearLeft;
                nextMessageDue = now.Add(clearMessageDelay);
            }
            else if (carsOnRightCount == 0 && hasCarRight && ((carsOnLeftCount == 0 && !hasCarLeft) || (carsOnLeftCount > 0 && hasCarLeft)))
            {
                Console.WriteLine("clear right, carsOnLeftCount " + carsOnLeftCount + " carsOnRightCount " + carsOnRightCount + " hasCarLeft " + hasCarLeft + " hasCarRight " + hasCarRight);
                // just gone clear on the right - might still be a car left
                nextMessageType = NextMessageType.clearRight;
                nextMessageDue = now.Add(clearMessageDelay);
            }
            else if (carsOnLeftCount > 0 && carsOnRightCount > 0 && (!hasCarLeft || !hasCarRight))
            {
                // new 'in the middle'
                Console.WriteLine("3 wide, carsOnLeftCount " + carsOnLeftCount + " carsOnRightCount " + carsOnRightCount + " hasCarLeft " + hasCarLeft + " hasCarRight " + hasCarRight);
                nextMessageType = NextMessageType.threeWide;
                nextMessageDue = now;
            }
            else if (carsOnLeftCount > 0 && carsOnRightCount == 0 && !hasCarLeft && !hasCarRight)
            {
                Console.WriteLine("car left, carsOnLeftCount " + carsOnLeftCount + " carsOnRightCount " + carsOnRightCount + " hasCarLeft " + hasCarLeft + " hasCarRight " + hasCarRight);
                nextMessageType = NextMessageType.carLeft;
                nextMessageDue = now;
            }
            else if (carsOnLeftCount == 0 && carsOnRightCount > 0 && !hasCarLeft && !hasCarRight)
            {
                Console.WriteLine("car right, carsOnLeftCount " + carsOnLeftCount + " carsOnRightCount " + carsOnRightCount + " hasCarLeft " + hasCarLeft + " hasCarRight " + hasCarRight);
                nextMessageType = NextMessageType.carRight;
                nextMessageDue = now;
            }
        }

        private Boolean messageIsValid(NextMessageType nextMessageType, int carsOnLeftCount, int carsOnRightCount)
        {
            if (nextMessageType == NextMessageType.carLeft && carsOnLeftCount == 0)
            {
                return false;
            }
            if (nextMessageType == NextMessageType.carRight && carsOnRightCount == 0)
            {
                return false;
            } 
            if (nextMessageType == NextMessageType.threeWide && (carsOnRightCount == 0 || carsOnLeftCount == 0))
            {
                return false;
            } 
            if (nextMessageType == NextMessageType.stillThere && carsOnRightCount == 0 && carsOnLeftCount == 0)
            {
                return false;
            }
            return true;
        }

        private void playNextMessage(int carsOnLeftCount, int carsOnRightCount, DateTime now) 
        {
            if (nextMessageType != NextMessageType.none && now > nextMessageDue)
            {
                if (messageIsValid(nextMessageType, carsOnLeftCount, carsOnRightCount))
                {
                    switch (nextMessageType)
                    {
                        case NextMessageType.threeWide:
                            audioPlayer.holdOpenChannel(true);
                            audioPlayer.removeImmediateClip(folderStillThere);
                            audioPlayer.removeImmediateClip(folderCarLeft);
                            audioPlayer.removeImmediateClip(folderCarRight);
                            audioPlayer.removeImmediateClip(folderClearAllRound);
                            audioPlayer.removeImmediateClip(folderClearLeft);
                            audioPlayer.removeImmediateClip(folderClearRight);
                            QueuedMessage inTheMiddleMessage = new QueuedMessage(folderInTheMiddle, 0, null);
                            inTheMiddleMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + inTheMiddleMessageExpiresAfter;
                            audioPlayer.playClipImmediately(inTheMiddleMessage);
                            nextMessageType = NextMessageType.stillThere;
                            nextMessageDue = now.Add(repeatHoldFrequency);
                            reportedOverlapLeft = true;
                            reportedOverlapRight = true;
                            break;
                        case NextMessageType.carLeft:
                            audioPlayer.holdOpenChannel(true);
                            audioPlayer.removeImmediateClip(folderStillThere);
                            audioPlayer.removeImmediateClip(folderInTheMiddle);
                            audioPlayer.removeImmediateClip(folderCarRight);
                            audioPlayer.removeImmediateClip(folderClearAllRound);
                            audioPlayer.removeImmediateClip(folderClearLeft);
                            audioPlayer.removeImmediateClip(folderClearRight);
                            QueuedMessage carLeftMessage = new QueuedMessage(folderCarLeft, 0, null);
                            carLeftMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                            audioPlayer.playClipImmediately(carLeftMessage);
                            nextMessageType = NextMessageType.stillThere;
                            nextMessageDue = now.Add(repeatHoldFrequency);
                            reportedOverlapLeft = true;
                            break;
                        case NextMessageType.carRight:
                            audioPlayer.holdOpenChannel(true);
                            audioPlayer.removeImmediateClip(folderStillThere);
                            audioPlayer.removeImmediateClip(folderCarLeft);
                            audioPlayer.removeImmediateClip(folderInTheMiddle);
                            audioPlayer.removeImmediateClip(folderClearAllRound);
                            audioPlayer.removeImmediateClip(folderClearLeft);
                            audioPlayer.removeImmediateClip(folderClearRight);
                            QueuedMessage carRightMessage = new QueuedMessage(folderCarRight, 0, null);
                            carRightMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                            audioPlayer.playClipImmediately(carRightMessage);
                            nextMessageType = NextMessageType.stillThere;
                            nextMessageDue = now.Add(repeatHoldFrequency);
                            reportedOverlapRight = true;
                            break;
                        case NextMessageType.clearAllRound:
                            if (reportedOverlapLeft || reportedOverlapRight)
                            {
                                QueuedMessage clearAllRoundMessage = new QueuedMessage(folderClearAllRound, 0, null);
                                clearAllRoundMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearAllRoundMessageExpiresAfter;
                                audioPlayer.removeImmediateClip(folderCarLeft);
                                audioPlayer.removeImmediateClip(folderStillThere);
                                audioPlayer.removeImmediateClip(folderCarRight);
                                audioPlayer.removeImmediateClip(folderInTheMiddle);
                                audioPlayer.removeImmediateClip(folderClearLeft);
                                audioPlayer.removeImmediateClip(folderClearRight);
                                audioPlayer.playClipImmediately(clearAllRoundMessage);                                
                                nextMessageType = NextMessageType.none;
                            }
                            audioPlayer.closeChannel();
                            reportedOverlapLeft = false;
                            reportedOverlapRight = false;
                            break;
                        case NextMessageType.clearLeft:
                            if (reportedOverlapLeft)
                            {
                                QueuedMessage clearLeftMessage = new QueuedMessage(folderClearLeft, 0, null);
                                clearLeftMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearMessageExpiresAfter;
                                audioPlayer.removeImmediateClip(folderCarLeft);
                                audioPlayer.removeImmediateClip(folderStillThere);
                                audioPlayer.removeImmediateClip(folderCarRight);
                                audioPlayer.removeImmediateClip(folderInTheMiddle);
                                audioPlayer.removeImmediateClip(folderClearRight);
                                audioPlayer.removeImmediateClip(folderClearAllRound);
                                audioPlayer.playClipImmediately(clearLeftMessage);                                
                                nextMessageType = NextMessageType.none;                                
                            }
                            if (carsOnRightCount == 0)
                            {
                                audioPlayer.closeChannel();
                            }
                            reportedOverlapLeft = false;
                            break;
                        case NextMessageType.clearRight:
                            if (reportedOverlapRight)
                            {
                                QueuedMessage clearRightMessage = new QueuedMessage(folderClearRight, 0, null);
                                clearRightMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearMessageExpiresAfter;
                                audioPlayer.removeImmediateClip(folderCarLeft);
                                audioPlayer.removeImmediateClip(folderStillThere);
                                audioPlayer.removeImmediateClip(folderCarRight);
                                audioPlayer.removeImmediateClip(folderInTheMiddle);
                                audioPlayer.removeImmediateClip(folderClearLeft);
                                audioPlayer.removeImmediateClip(folderClearAllRound);
                                audioPlayer.playClipImmediately(clearRightMessage);                                
                                nextMessageType = NextMessageType.none;
                            }
                            if (carsOnLeftCount == 0)
                            {
                                audioPlayer.closeChannel();
                            }
                            reportedOverlapRight = false;
                            break;
                        case NextMessageType.stillThere:
                            if (reportedOverlapLeft || reportedOverlapRight)
                            {
                                QueuedMessage holdYourLineMessage = new QueuedMessage(folderStillThere, 0, null);
                                holdYourLineMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                                audioPlayer.removeImmediateClip(folderCarLeft);
                                audioPlayer.removeImmediateClip(folderCarRight);
                                audioPlayer.removeImmediateClip(folderInTheMiddle);
                                audioPlayer.removeImmediateClip(folderClearRight);
                                audioPlayer.removeImmediateClip(folderClearLeft);
                                audioPlayer.removeImmediateClip(folderClearAllRound);
                                audioPlayer.playClipImmediately(holdYourLineMessage);
                                nextMessageType = NextMessageType.stillThere;
                                nextMessageDue = now.Add(repeatHoldFrequency);
                            }
                            break;
                        case NextMessageType.none:
                            break;
                    }
                }
                else
                {
                    audioPlayer.closeChannel();
                    reportedOverlapLeft = false;
                    reportedOverlapRight = false;
                }
            }
        }

        private Side getSide(float playerRotation, float[] playerWorldPosition, float[] opponentWorldPosition)
        {
            float rawXCoordinate = opponentWorldPosition[0] - playerWorldPosition[0];
            float rawYCoordinate = opponentWorldPosition[2] - playerWorldPosition[2];
            if (rawXCoordinate > carLength && rawYCoordinate > longCarLength)
            {
                return Side.none;
            }

            if (playerRotation < 0)
            {
                playerRotation = (float)(2 * Math.PI) + playerRotation;
            }
            playerRotation = (float)(2 * Math.PI) - playerRotation;
            
            // now transform the position by rotating the frame of reference to align it north-south. The player's car is at the origin pointing north.
            // We assume that both cars have similar orientations (or at least, any orientation difference isn't going to be relevant)
            float alignedXCoordinate = ((float)Math.Cos(playerRotation) * rawXCoordinate) + ((float)Math.Sin(playerRotation) * rawYCoordinate);
            float alignedYCoordinate = ((float)Math.Cos(playerRotation) * rawYCoordinate) - ((float)Math.Sin(playerRotation) * rawXCoordinate);


            // when checking for an overlap, use the 'short' (actual) car length if we're not already overlapping on that side.
            // If we're already overlapping, use the 'long' car length - this means we don't call 'clear' till there's a small gap
            if (Math.Abs(alignedXCoordinate) < trackWidth && Math.Abs(alignedXCoordinate) > carWidth)
            {
                // we're not directly behind / ahead, but are within a track width of this car
                if (alignedXCoordinate < 0)
                {
                    if (hasCarRight)
                    {
                        if (Math.Abs(alignedYCoordinate) < longCarLength)
                        {
                            return Side.right;
                        }
                    }
                    else if (Math.Abs(alignedYCoordinate) < carLength)
                    {
                        return Side.right;
                    }
                }
                else
                {
                    if (hasCarLeft)
                    {
                        if (Math.Abs(alignedYCoordinate) < longCarLength)
                        {
                            return Side.left;
                        }
                    }
                    else if (Math.Abs(alignedYCoordinate) < carLength)
                    {
                        return Side.left;
                    }
                }
            }
            return Side.none;
        }

        public void enableSpotter()
        {
            enabled = true;
            audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderEnableSpotter, 0, null));
            audioPlayer.closeChannel();
        }

        public void disableSpotter()
        {
            enabled = false;
            audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderDisableSpotter, 0, null));
            audioPlayer.closeChannel();
        }
    }
}
