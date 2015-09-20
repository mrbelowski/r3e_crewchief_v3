using System;
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
            if (o.GetType() == typeof(String)) {
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
    }
}
