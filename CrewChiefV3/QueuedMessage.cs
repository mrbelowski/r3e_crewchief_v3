using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV3.Events;
using CrewChiefV3.GameState;

namespace CrewChiefV3
{
    class QueuedMessage
    {
        public static String compoundMessageIdentifier = "COMPOUND_";

        public static String driverNameIdentifier = "DRIVERNAME_";

        public static String folderNameOh = "numbers/oh";
        public static String folderNamePoint = "numbers/point";
        public static String folderNameNumbersStub = "numbers/";
        public static String folderZeroZero = "numbers/zerozero";

        // if a queued message is a gap filler, it's only played if the queue only contains 1 other message
        public Boolean gapFiller = false;
        public long dueTime;
        public AbstractEvent abstractEvent;
        public TimeSpan timeSpan;
        private Boolean timeSpanSet = false;
        public List<String> messagesBeforeTimeSpan = new List<String>();
        public List<String> messagesAfterTimeSpan = new List<String>();

        // TODO: the queued message should contain some snapshot of pertentent data at the point of creation, 
        // which can be validated before it actually gets played. Perhaps a Dictionary of property names and value - 
        // e.g. {SessionData.Position = 1}

        public long expiryTime = 0;

        // Note that even when queuing a message with 0 delay, we always wait 1 complete update interval. This is to 
        // (hopefully...) address issues where some data in the block get updated (like the lap count), but other data haven't 
        // get been updated (like the session phase)
        private int updateInterval = UserSettings.GetUserSettings().getInt("update_interval");

        public QueuedMessage(int secondsDelay, AbstractEvent abstractEvent)
        {
            this.dueTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + (secondsDelay * 1000) + updateInterval;
            this.abstractEvent = abstractEvent;
        }

        // used for creating a pearl of wisdom message where we need to copy the dueTime from the original
        public QueuedMessage(AbstractEvent abstractEvent)
        {
            this.abstractEvent = abstractEvent;
        }

        public QueuedMessage(List<String> messages, int secondsDelay, AbstractEvent abstractEvent)
        {
            if (messages != null && messages.Count > 0)
            {
                this.messagesBeforeTimeSpan.AddRange(messages);
            }
            this.dueTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + (secondsDelay * 1000) + updateInterval;
            this.abstractEvent = abstractEvent;
        }

        public QueuedMessage(String messageBeforeTimeSpan, String messageAfterTimeSpan, TimeSpan timeSpan, int secondsDelay, AbstractEvent abstractEvent)
        {
            if (messageBeforeTimeSpan != null)
            {
                this.messagesBeforeTimeSpan.Add(messageBeforeTimeSpan);
            }
            if (messageAfterTimeSpan != null)
            {
                this.messagesAfterTimeSpan.Add(messageAfterTimeSpan);
            }
            this.timeSpan = timeSpan;
            timeSpanSet = true;
            this.dueTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + (secondsDelay * 1000) + updateInterval;
            this.abstractEvent = abstractEvent;
        }

        public QueuedMessage(List<String> messagesBeforeTimeSpan, List<String> messagesAfterTimeSpan, TimeSpan timeSpan, int secondsDelay, AbstractEvent abstractEvent)
        {
            if (messagesBeforeTimeSpan != null && messagesBeforeTimeSpan.Count > 0)
            {
                this.messagesBeforeTimeSpan.AddRange(messagesBeforeTimeSpan);
            }
            if (messagesAfterTimeSpan != null && messagesAfterTimeSpan.Count > 0)
            {
                this.messagesAfterTimeSpan.AddRange(messagesAfterTimeSpan);
            }
            this.timeSpan = timeSpan;
            timeSpanSet = true;
            this.dueTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + (secondsDelay * 1000) + updateInterval;
            this.abstractEvent = abstractEvent;
        }

        public Boolean isMessageStillValid(String eventSubType, GameStateData currentGameState)
        {
            return this.abstractEvent == null || 
                this.abstractEvent.isMessageStillValid(eventSubType, currentGameState);
        }

        public List<String> getMessageFolders()
        {
            List<String> messages = new List<String>();
            messages.AddRange(messagesBeforeTimeSpan);
            if (timeSpanSet == true && timeSpan != null)
            {
                messages.AddRange(getTimeMessageFolders(timeSpan));
            }
            messages.AddRange(messagesAfterTimeSpan);
            return messages;
        }

        private List<String> getTimeMessageFolders(TimeSpan timeSpan)
        {
            List<String> messages = new List<String>();
            if (timeSpan != null)
            {
                // if the milliseconds would is > 949, when we turn this into tenths it'll get rounded up to 
                // ten tenths, which we can't have. So move the timespan on so this rounding doesn't happen
                if (timeSpan.Milliseconds > 949)
                {
                    timeSpan = timeSpan.Add(TimeSpan.FromMilliseconds(1000 - timeSpan.Milliseconds));
                }
                if (timeSpan.Minutes > 0)
                {
                    messages.AddRange(getFolderNames(timeSpan.Minutes, ZeroType.NONE));
                    if (timeSpan.Seconds == 0)
                    {
                        // add "zero-zero" for messages with minutes in them
                        messages.Add(folderZeroZero);
                    }
                    else
                    {
                        messages.AddRange(getFolderNames(timeSpan.Seconds, ZeroType.OH));
                    }
                }
                else
                {
                    messages.AddRange(getFolderNames(timeSpan.Seconds, ZeroType.NONE));
                }
                int tenths = (int)Math.Round(((double)timeSpan.Milliseconds / 100));
                messages.Add(folderNamePoint);
                if (tenths == 0)
                {
                    messages.Add(folderNameNumbersStub + 0);
                }
                else
                {
                    messages.AddRange(getFolderNames(tenths, ZeroType.NONE));
                }
            }
            return messages;
        }

        private List<String> getFolderNames(int number, ZeroType zeroType)
        {
            List<String> names = new List<String>();
            if (number < 60)
            {
                // only numbers < 60 are supported
                if (number < 10)
                {
                    // if the number is < 10, use the "oh two" files if we've asked for "oh" instead of "zero"
                    if (zeroType == ZeroType.OH)
                    {
                        if (number == 0)
                        {
                            // will this block ever be reached?
                            names.Add(folderNameOh);
                        }
                        else
                        {
                            names.Add(folderNameNumbersStub + "0" + number);
                        }
                    }
                    else if (zeroType == ZeroType.ZERO)
                    {
                        names.Add(folderNameNumbersStub + 0);
                        if (number > 0)
                        {
                            names.Add(folderNameNumbersStub + number);
                        }
                    }
                    else
                    {
                        names.Add(folderNameNumbersStub + number);
                    }
                }
                else
                {
                    // > 10 so use the actual number
                    names.Add(folderNameNumbersStub + number);
                }
            }
            return names;
        }

        private enum ZeroType
        {
            NONE, OH, ZERO
        }
    }
}
