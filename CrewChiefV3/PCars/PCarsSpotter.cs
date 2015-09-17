using System;
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
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");

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
            pCarsAPIStruct lastState = (pCarsAPIStruct)lastStateObj;
            pCarsAPIStruct currentState = (pCarsAPIStruct)currentStateObj;
            DateTime now = DateTime.Now;

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
                            if (opponentData.mIsActive && opponentData.mWorldPosition[0] != 0 && opponentData.mWorldPosition[2] != 0)
                            {
                                // if we think this opponent car isn't even on the same 10 metre chunk of track to us, ignore it
                                Boolean opponentIsInQuickNDirtyCheckZone = playerData.mCurrentLapDistance < trackLengthToUse;
                                if (playerIsInQuickNDirtyCheckZone && opponentIsInQuickNDirtyCheckZone &&
                                    Math.Abs(playerData.mCurrentLapDistance - opponentData.mCurrentLapDistance) > 10)
                                {
                                    continue;
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

        private void playNextMessage(int carsOnLeftCount, int carsOnRightCount, DateTime now) 
        {
            if (nextMessageType != NextMessageType.none && now > nextMessageDue)
            {
                switch (nextMessageType)
                {
                    case NextMessageType.threeWide:
                        audioPlayer.holdOpenChannel(true);
                        QueuedMessage inTheMiddleMessage = new QueuedMessage(0, null);
                        inTheMiddleMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + inTheMiddleMessageExpiresAfter;
                        audioPlayer.playClipImmediately(folderInTheMiddle, inTheMiddleMessage);
                        nextMessageType = NextMessageType.stillThere;
                        nextMessageDue = now.Add(repeatHoldFrequency);
                        break;
                    case NextMessageType.carLeft:
                        audioPlayer.holdOpenChannel(true);
                        QueuedMessage carLeftMessage = new QueuedMessage(0, null);
                        carLeftMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                        audioPlayer.playClipImmediately(folderCarLeft, carLeftMessage);
                        nextMessageType = NextMessageType.stillThere;
                        nextMessageDue = now.Add(repeatHoldFrequency);
                        break;
                    case NextMessageType.carRight:
                        audioPlayer.holdOpenChannel(true);
                        QueuedMessage carRightMessage = new QueuedMessage(0, null);
                        carRightMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                        audioPlayer.playClipImmediately(folderCarRight, carRightMessage);
                        nextMessageType = NextMessageType.stillThere;
                        nextMessageDue = now.Add(repeatHoldFrequency);
                        break;
                    case NextMessageType.clearAllRound:
                        QueuedMessage clearAllRoundMessage = new QueuedMessage(0, null);
                        clearAllRoundMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearAllRoundMessageExpiresAfter;
                        audioPlayer.playClipImmediately(folderClearAllRound, clearAllRoundMessage);
                        audioPlayer.closeChannel();
                        nextMessageType = NextMessageType.none;
                        break;
                    case NextMessageType.clearLeft:
                        QueuedMessage clearLeftMessage = new QueuedMessage(0, null);
                        clearLeftMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearMessageExpiresAfter;
                        audioPlayer.playClipImmediately(folderClearLeft, clearLeftMessage);
                        if (carsOnRightCount == 0)
                        {
                            audioPlayer.closeChannel();
                        }
                        nextMessageType = NextMessageType.none;
                        break;
                    case NextMessageType.clearRight:
                        QueuedMessage clearRightMessage = new QueuedMessage(0, null);
                        clearRightMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearMessageExpiresAfter;
                        audioPlayer.playClipImmediately(folderClearRight, clearRightMessage);
                        if (carsOnLeftCount == 0)
                        {
                            audioPlayer.closeChannel();
                        }
                        nextMessageType = NextMessageType.none;
                        break;
                    case NextMessageType.stillThere:
                        QueuedMessage holdYourLineMessage = new QueuedMessage(0, null);
                        holdYourLineMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                        audioPlayer.playClipImmediately(folderStillThere, holdYourLineMessage);
                        nextMessageType = NextMessageType.stillThere;
                        nextMessageDue = now.Add(repeatHoldFrequency);
                        break;
                    case NextMessageType.none:
                        break;
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
            audioPlayer.playClipImmediately(AudioPlayer.folderEnableSpotter, new QueuedMessage(0, null));
            audioPlayer.closeChannel();
        }

        public void disableSpotter()
        {
            enabled = false;
            audioPlayer.playClipImmediately(AudioPlayer.folderDisableSpotter, new QueuedMessage(0, null));
            audioPlayer.closeChannel();
        }
    }
}
