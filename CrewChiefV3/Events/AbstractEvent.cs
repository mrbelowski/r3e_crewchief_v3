﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.GameState;

namespace CrewChiefV3.Events
{
    abstract class AbstractEvent
    {
        protected AudioPlayer audioPlayer;

        protected PearlsOfWisdom pearlsOfWisdom;


        // some convienence methods for building up compound messages
        public static List<MessageFragment> MessageContents(Object o1, Object o2, Object o3, Object o4, Object o5)
        {
            List<MessageFragment> messages = new List<MessageFragment>();
            addObjectToMessages(messages, o1);
            addObjectToMessages(messages, o2);
            addObjectToMessages(messages, o3);
            addObjectToMessages(messages, o4);
            addObjectToMessages(messages, o5);
            return messages;
        }
        public static List<MessageFragment> MessageContents(Object o1, Object o2, Object o3, Object o4)
        {
            List<MessageFragment> messages = new List<MessageFragment>();
            addObjectToMessages(messages, o1);
            addObjectToMessages(messages, o2);
            addObjectToMessages(messages, o3);
            addObjectToMessages(messages, o4);
            return messages;
        }
        public static List<MessageFragment> MessageContents(Object o1, Object o2, Object o3)
        {
            List<MessageFragment> messages = new List<MessageFragment>();
            addObjectToMessages(messages, o1);
            addObjectToMessages(messages, o2);
            addObjectToMessages(messages, o3);
            return messages;
        }
        public static List<MessageFragment> MessageContents(Object o1, Object o2)
        {
            List<MessageFragment> messages = new List<MessageFragment>();
            addObjectToMessages(messages, o1);
            addObjectToMessages(messages, o2);
            return messages;
        }
        public static List<MessageFragment> MessageContents(Object o1)
        {
            List<MessageFragment> messages = new List<MessageFragment>();
            addObjectToMessages(messages, o1);
            return messages;
        }

        private static void addObjectToMessages(List<MessageFragment> messageFragments, Object o) {
            if (o == null)
            {
                messageFragments.Add(null);
            }
            else if (o.GetType() == typeof(String)) {
                messageFragments.Add(MessageFragment.Text((String)o));
            }
            else if (o.GetType() == typeof(TimeSpan))
            {
                messageFragments.Add(MessageFragment.Time((TimeSpan)o));
            }
            else if (o.GetType() == typeof(OpponentData)) {
                messageFragments.Add(MessageFragment.Opponent((OpponentData)o));
            } 
        }

        public virtual List<SessionType> applicableSessionTypes 
        {
            get { return new List<SessionType> { SessionType.Practice, SessionType.Qualify, SessionType.Race }; }
        }

        public virtual List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Green }; }
        }

        // this is called on each 'tick' - the event subtype should
        // place its logic in here including calls to audioPlayer.queueClip
        abstract protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState);

        // reinitialise any state held by the event subtype
        public abstract void clearState();

        // generally the event subclass can just return true for this, but when a clip is played with
        // a non-zero delay it may be necessary to re-check that the clip is still valid against the current
        // state
        public virtual Boolean isMessageStillValid(String eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            return isApplicableForCurrentSessionAndPhase(currentGameState.SessionData.SessionType, currentGameState.SessionData.SessionPhase);
        }

        public Boolean isApplicableForCurrentSessionAndPhase(SessionType sessionType, SessionPhase sessionPhase)
        {
            return applicableSessionPhases.Contains(sessionPhase) && applicableSessionTypes.Contains(sessionType);
        }

        public virtual void respond(String voiceMessage)
        {
            // no-op, override in the subclasses
        }

        public void setPearlsOfWisdom(PearlsOfWisdom pearlsOfWisdom)
        {
            this.pearlsOfWisdom = pearlsOfWisdom;
        }

        public void trigger(GameStateData previousGameState, GameStateData currentGameState)
        {
            // common checks here?
            triggerInternal(previousGameState, currentGameState);
        }

        public Boolean messagesHaveSameContent(List<MessageFragment> messages1, List<MessageFragment> messages2)
        {
            if (messages1 == null && messages2 == null) 
            {
                return true;
            }
            if ((messages1 == null && messages2 != null) || (messages1 != null && messages2 == null) ||
                messages1.Count != messages2.Count)
            {
                return false;
            }
            foreach (MessageFragment m1Fragment in messages1)
            {
                Boolean foundMatch = false;
                foreach (MessageFragment m2Fragment in messages2)
                {
                    if (m1Fragment.type == MessageFragment.FragmentType.Text && m2Fragment.type == MessageFragment.FragmentType.Text && m1Fragment.text.Equals(m2Fragment.text))
                    {
                        foundMatch = true;
                        break;
                    }
                    else if (m1Fragment.type == MessageFragment.FragmentType.Time && m2Fragment.type == MessageFragment.FragmentType.Time && m1Fragment.time.Equals(m2Fragment.time))
                    {
                        foundMatch = true;
                        break;
                    }
                    else if (m1Fragment.type == MessageFragment.FragmentType.Opponent && m2Fragment.type == MessageFragment.FragmentType.Opponent && 
                        ((m1Fragment.opponent == null && m2Fragment.opponent == null) || 
                            (m1Fragment.opponent != null && m2Fragment.opponent != null && m1Fragment.opponent.DriverRawName.Equals(m2Fragment.opponent.DriverRawName))))
                    {
                        foundMatch = true;
                        break;
                    }
                }
                if (!foundMatch)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
