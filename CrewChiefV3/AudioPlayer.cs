using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Media;
using CrewChiefV3.Events;
using System.Windows.Media;
using System.Collections.Specialized;
using CrewChiefV3.GameState;

namespace CrewChiefV3
{
    class AudioPlayer
    {
        public static float minimumSoundPackVersion = 3.0f;

        private CrewChief crewChief;

        public static String folderAcknowlegeOK = "acknowledge/OK";
        public static String folderAcknowlegeEnableKeepQuiet = "acknowledge/keepQuietEnabled";
        public static String folderEnableSpotter = "acknowledge/spotterEnabled";
        public static String folderDisableSpotter = "acknowledge/spotterDisabled";
        public static String folderAcknowlegeDisableKeepQuiet = "acknowledge/keepQuietDisabled";
        public static String folderDidntUnderstand = "acknowledge/didnt_understand";
        public static String folderNoData = "acknowledge/no_data";
        public static String folderYes = "acknowledge/yes";
        public static String folderNo = "acknowledge/no";

        private Dictionary<String, int> playedMessagesCount = new Dictionary<String, int>();

        private Boolean monitorRunning = false;

        private Boolean keepQuiet = false;
        private Boolean channelOpen = false;

        private Boolean requestChannelOpen = false;
        private Boolean requestChannelClose = false;
        private Boolean holdChannelOpen = false;
        private Boolean useShortBeepWhenOpeningChannel = false;

        private readonly TimeSpan queueMonitorInterval = TimeSpan.FromMilliseconds(1000);

        private readonly int immediateMessagesMonitorInterval = 20;

        private Dictionary<String, List<SoundPlayer>> clips = new Dictionary<String, List<SoundPlayer>>();

        private Dictionary<String, SoundPlayer> driverNameClips = new Dictionary<String, SoundPlayer>();

        private String soundFolderName = UserSettings.GetUserSettings().getString("sound_files_path");

        private String voiceFolderPath;

        private String fxFolderPath;

        private String driverNamesFolderPath;

        private readonly TimeSpan minTimeBetweenPearlsOfWisdom = TimeSpan.FromSeconds(UserSettings.GetUserSettings().getInt("minimum_time_between_pearls_of_wisdom"));

        private Boolean sweary = UserSettings.GetUserSettings().getBoolean("use_sweary_messages");

        // if this is true, no 'green green green', 'get ready', or spotter messages are played
        private Boolean disableImmediateMessages = UserSettings.GetUserSettings().getBoolean("disable_immediate_messages");

        private Random random = new Random();

        private OrderedDictionary queuedClips = new OrderedDictionary();

        private OrderedDictionary immediateClips = new OrderedDictionary();

        List<String> enabledSounds = new List<String>();

        Boolean enableStartBleep = false;

        Boolean enableEndBleep = false;

        MediaPlayer backgroundPlayer;

        public String soundFilesPath;

        private String backgroundFilesPath;

        // TODO: sort looping callback out so we don't need this...
        private int backgroundLeadout = 30;

        public static String dtmPitWindowOpenBackground = "dtm_pit_window_open.wav";

        public static String dtmPitWindowClosedBackground = "dtm_pit_window_closed.wav";

        // only the monitor Thread can request a reload of the background wav file, so
        // the events thread will have to set these variables to ask for a reload
        private Boolean loadNewBackground = false;
        private String backgroundToLoad;

        private PearlsOfWisdom pearlsOfWisdom;

        DateTime timeLastPearlOfWisdomPlayed = DateTime.UtcNow;

        private Boolean backgroundPlayerInitialised = false;

        public Boolean initialised = false;

        public AudioPlayer(CrewChief crewChief)
        {
            this.crewChief = crewChief;
        }
        
        public void initialise()
        {
            if (soundFolderName.Length > 3 && (soundFolderName.Substring(1, 2) == @":\" || soundFolderName.Substring(1, 2) == @":/"))
            {
                soundFilesPath = soundFolderName;
            } else {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    soundFilesPath = Path.Combine(Path.GetDirectoryName(
                                            System.Reflection.Assembly.GetEntryAssembly().Location), @"..\", @"..\", soundFolderName);
                }
                else
                {
                    soundFilesPath = Path.Combine(Path.GetDirectoryName(
                                            System.Reflection.Assembly.GetEntryAssembly().Location), soundFolderName);
                }
            }
            
            voiceFolderPath = Path.Combine(soundFilesPath, "voice");
            fxFolderPath = Path.Combine(soundFilesPath , "fx");
            driverNamesFolderPath = Path.Combine(soundFilesPath, "driver_names");
            backgroundFilesPath = Path.Combine(soundFilesPath, "background_sounds");
            Console.WriteLine("Voice dir full path = " + voiceFolderPath);
            Console.WriteLine("FX dir full path = " + fxFolderPath);
            Console.WriteLine("driver names full path = " + driverNamesFolderPath);
            Console.WriteLine("Background sound dir full path = " + backgroundFilesPath);
            DirectoryInfo soundDirectory = new DirectoryInfo(soundFilesPath);
            if (!soundDirectory.Exists)
            {
                Console.WriteLine("Unable to find sound directory " + soundDirectory.FullName);
                return;
            }
            float soundPackVersion = getSoundPackVersion(soundDirectory);
            if (soundPackVersion == -1 || soundPackVersion == 0)
            {
                Console.WriteLine("Unable to get sound pack version - expected a file called version_info with a single line containing a version number, e.g. 2.0");
            }
            else if (soundPackVersion < minimumSoundPackVersion)
            {
                Console.WriteLine("The sound pack version in use is " + soundPackVersion + " but this version of the app requires version " 
                    + minimumSoundPackVersion + " or greater.");
                Console.WriteLine("You must update your sound pack to run this application");
                return;
            }
            else
            {
                Console.WriteLine("Minimum sound pack version = " + minimumSoundPackVersion + " using sound pack version " + soundPackVersion);
            }
            pearlsOfWisdom = new PearlsOfWisdom();
            int soundsCount = 0;
            try
            {
                DirectoryInfo fxSoundDirectory = new DirectoryInfo(fxFolderPath);
                if (!fxSoundDirectory.Exists)
                {
                    Console.WriteLine("Unable to find fx directory " + fxSoundDirectory.FullName);
                    return;
                }
                FileInfo[] bleepFiles = fxSoundDirectory.GetFiles();
                foreach (FileInfo bleepFile in bleepFiles)
                {
                    if (bleepFile.Name.EndsWith(".wav"))
                    {
                        if (bleepFile.Name.StartsWith("start"))
                        {
                            enableStartBleep = true;
                            openAndCacheClip("start_bleep", bleepFile.FullName);
                        }
                        else if (bleepFile.Name.StartsWith("end"))
                        {
                            enableEndBleep = true;
                            openAndCacheClip("end_bleep", bleepFile.FullName);
                        }
                        else if (bleepFile.Name.StartsWith("short"))
                        {
                            enableEndBleep = true;
                            openAndCacheClip("short_bleep", bleepFile.FullName);
                        }
                    }
                }
                DirectoryInfo voiceSoundDirectory = new DirectoryInfo(voiceFolderPath);
                if (!voiceSoundDirectory.Exists)
                {
                    Console.WriteLine("Unable to find voice directory " + voiceSoundDirectory.FullName);
                    return;
                }
                DirectoryInfo[] eventFolders = voiceSoundDirectory.GetDirectories();
                foreach (DirectoryInfo eventFolder in eventFolders)
                {
                    try
                    {
                        //Console.WriteLine("Got event folder " + eventFolder.Name);
                        DirectoryInfo[] eventDetailFolders = eventFolder.GetDirectories();
                        foreach (DirectoryInfo eventDetailFolder in eventDetailFolders)
                        {
                            //Console.WriteLine("Got event detail subfolder " + eventDetailFolder.Name);
                            String fullEventName = eventFolder + "/" + eventDetailFolder;
                            try
                            {
                                FileInfo[] soundFiles = eventDetailFolder.GetFiles();
                                foreach (FileInfo soundFile in soundFiles)
                                {
                                    if (soundFile.Name.EndsWith(".wav") && (sweary || !soundFile.Name.StartsWith("sweary")))
                                    {
                                        //Console.WriteLine("Got sound file " + soundFile.FullName);
                                        soundsCount++;
                                        openAndCacheClip(eventFolder + "/" + eventDetailFolder, soundFile.FullName);
                                        if (!enabledSounds.Contains(fullEventName))
                                        {
                                            enabledSounds.Add(fullEventName);
                                        }
                                    }
                                }
                                if (!enabledSounds.Contains(fullEventName))
                                {
                                    Console.WriteLine("Event " + fullEventName + " has no sound files");
                                }
                            }
                            catch (DirectoryNotFoundException)
                            {
                                Console.WriteLine("Event subfolder " + fullEventName + " not found");
                            }
                        }
                    }
                    catch (DirectoryNotFoundException)
                    {
                        Console.WriteLine("Unable to find events folder");
                    }
                }
                Console.WriteLine("Cached " + soundsCount + " clips");
                initialised = true;
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("Unable to find sounds directory - path: " + soundFolderName);
            }
        }

        public void cacheDriverNames(List<String> driverNames)
        {
            foreach (KeyValuePair<String, SoundPlayer> driverNameClip in driverNameClips) 
            {
                driverNameClip.Value.Stop();
                driverNameClip.Value.Dispose();
            }
            driverNameClips.Clear();
            try
            {
                DirectoryInfo driverNamesSoundDirectory = new DirectoryInfo(driverNamesFolderPath);
                if (!driverNamesSoundDirectory.Exists)
                {
                    Console.WriteLine("Unable to find driver names directory " + driverNamesSoundDirectory.FullName);
                    return;
                }
                FileInfo[] driverNamesFiles = driverNamesSoundDirectory.GetFiles();
                foreach (FileInfo driverNameFile in driverNamesFiles)
                {
                    if (driverNameFile.Name.EndsWith(".wav"))
                    {
                        foreach (String driverName in driverNames)
                        {
                            if (driverNameFile.Name.Equals(driverName + ".wav") && !driverNameClips.ContainsKey(driverName))
                            {
                                Console.WriteLine("Cached driver name sound file for " + driverName);
                                SoundPlayer clip = new SoundPlayer(driverNameFile.FullName);
                                clip.Load();
                                driverNameClips.Add(driverName, clip);
                            }
                        }
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("Unable to find driver names directory - path: " + driverNamesFolderPath);
            }
        }

        public Boolean hasDriverName(String driverName)
        {
            return driverNameClips.ContainsKey(driverName);
        }
        
        public void startMonitor()
        {
            if (monitorRunning)
            {
                Console.WriteLine("Monitor is already running");
            }
            else
            {
                Console.WriteLine("Starting queue monitor");
                monitorRunning = true;
                // spawn a Thread to monitor the queue
                ThreadStart work;
                if (disableImmediateMessages)
                {
                    Console.WriteLine("Interupting and immediate messages are disabled - no spotter or 'green green green'");
                    work = monitorQueueNoImmediateMessages;
                }
                else
                {
                    work = monitorQueue;
                }
                Thread thread = new Thread(work);
                thread.Start();
            }
            new SmokeTest(this).trigger(new GameStateData(), new GameStateData());
        }

        public void stopMonitor()
        {
            Console.WriteLine("Stopping queue monitor");
            monitorRunning = false;
        }

        private float getBackgroundVolume()
        {
            float volume = UserSettings.GetUserSettings().getFloat("background_volume");
            if (volume > 1)
            {
                volume = 1;
            }
            if (volume < 0)
            {
                volume = 0;
            }
            return volume;
        }

        public float getSoundPackVersion(DirectoryInfo soundDirectory)
        {
            FileInfo[] filesInSoundDirectory = soundDirectory.GetFiles();
            
            float soundfilesVersion = -1f;
            foreach (FileInfo fileInSoundDirectory in filesInSoundDirectory)
            {
                if (fileInSoundDirectory.Name == "version_info")
                {
                    String[] lines = File.ReadAllLines(Path.Combine(soundFilesPath, fileInSoundDirectory.Name));
                    foreach (String line in lines) {
                        if (float.TryParse(line, out soundfilesVersion))
                        {
                            return soundfilesVersion;
                        }
                    }
                }
            }
            return soundfilesVersion;
        }

        public void setBackgroundSound(String backgroundSoundName)
        {
            backgroundToLoad = backgroundSoundName;
            loadNewBackground = true;
        }

        private void initialiseBackgroundPlayer()
        {
            if (!backgroundPlayerInitialised && getBackgroundVolume() > 0)
            {
                backgroundPlayer = new MediaPlayer();
                backgroundPlayer.MediaEnded += new EventHandler(backgroundPlayer_MediaEnded);
                backgroundPlayer.Volume = getBackgroundVolume();
                setBackgroundSound(dtmPitWindowClosedBackground);
                backgroundPlayerInitialised = true;
            }
        }

        private void stopBackgroundPlayer()
        {
            if (backgroundPlayer != null && backgroundPlayerInitialised)
            {
                backgroundPlayer.Stop();
                backgroundPlayerInitialised = false;
                backgroundPlayer = null;
            }
        }

        private void monitorQueue()
        {
            Console.WriteLine("Monitor starting");
            initialiseBackgroundPlayer();
            DateTime nextQueueCheck = DateTime.Now;
            while (monitorRunning)
            {                
                if (requestChannelOpen)
                {
                    openRadioChannelInternal();
                    requestChannelOpen = false;
                    holdChannelOpen = true;
                }
                if (!holdChannelOpen && channelOpen)
                {
                    closeRadioInternalChannel();
                }
                if (immediateClips.Count > 0)
                {
                    try
                    {
                        playQueueContents(immediateClips, true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception processing immediate clips: " + e.Message);
                        lock (immediateClips)
                        {
                            immediateClips.Clear();
                        }
                    }
                }
                if (requestChannelClose)
                {
                    if (channelOpen)
                    {
                        if (!queueHasDueMessages(queuedClips, false) && !queueHasDueMessages(immediateClips, true))
                        {
                            requestChannelClose = false;
                            holdChannelOpen = false;
                            closeRadioInternalChannel();                            
                        }
                    }
                    else
                    {
                        requestChannelClose = false;
                        holdChannelOpen = false;
                    }
                }                
                if (DateTime.Now > nextQueueCheck)
                {
                    nextQueueCheck = nextQueueCheck.Add(queueMonitorInterval);
                    try
                    {
                        playQueueContents(queuedClips, false);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception processing queued clips: " + e.Message);
                        lock (queuedClips)
                        {
                            queuedClips.Clear();
                        }
                    }
                } 
                else
                {
                    Thread.Sleep(immediateMessagesMonitorInterval);
                    continue;
                }                
            }
            //writeMessagePlayedStats();
            playedMessagesCount.Clear();
            stopBackgroundPlayer();
        }

        private void writeMessagePlayedStats()
        {
            Console.WriteLine("Count, event name");
            foreach (KeyValuePair<String, int> entry in playedMessagesCount)
            {
                Console.WriteLine(entry.Value + " instances of event " + entry.Key);
            }
        }

        public void enableKeepQuietMode()
        {
            playClipImmediately(folderAcknowlegeEnableKeepQuiet, new QueuedMessage(0, null));
            closeChannel();
            keepQuiet = true;
        }

        public void disableKeepQuietMode()
        {
            playClipImmediately(folderAcknowlegeDisableKeepQuiet, new QueuedMessage(0, null));
            closeChannel();
            keepQuiet = false;
        }

        private void monitorQueueNoImmediateMessages()
        {
            initialiseBackgroundPlayer();
            while (monitorRunning)
            {
                Thread.Sleep(queueMonitorInterval);
                try
                {
                    playQueueContents(queuedClips, false);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception processing queued clips: " + e.Message);
                }
                if (!holdChannelOpen && channelOpen)
                {
                    closeRadioInternalChannel();
                }
            }
            writeMessagePlayedStats();
            playedMessagesCount.Clear();
            stopBackgroundPlayer();
        }

        private void playQueueContents(OrderedDictionary queueToPlay, Boolean isImmediateMessages)
        {
            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            List<String> keysToPlay = new List<String>();
            List<String> soundsProcessed = new List<String>();

            Boolean oneOrMoreEventsEnabled = false;
            if (queueToPlay.Count > 0)
            {
                Console.WriteLine("Processing queue of " + queueToPlay.Count + " event(s)");
            }
            lock (queueToPlay)
            {
                foreach (String key in queueToPlay.Keys)
                {
                    QueuedMessage queuedMessage = (QueuedMessage)queueToPlay[key];
                    if (isImmediateMessages || queuedMessage.dueTime <= milliseconds)
                    {
                        if ((isImmediateMessages || !keepQuiet) && 
                            queuedMessage.isMessageStillValid(key, crewChief.currentGameState) &&
                            !keysToPlay.Contains(key) && (!queuedMessage.gapFiller || playGapFillerMessage(queueToPlay)) &&
                            (queuedMessage.expiryTime == 0 || queuedMessage.expiryTime > milliseconds))
                        {
                            keysToPlay.Add(key);
                        }
                        else
                        {
                            Console.WriteLine("Clip " + key + " is not valid");
                            soundsProcessed.Add(key);
                        }
                    }
                }
                if (keysToPlay.Count > 0)
                {
                    if (keysToPlay.Count == 1 && clipIsPearlOfWisdom(keysToPlay[0]) && hasPearlJustBeenPlayed())
                    {
                        Console.WriteLine("Rejecting pearl of wisdom " + keysToPlay[0] +
                            " because one has been played in the last " + minTimeBetweenPearlsOfWisdom + " seconds");
                        soundsProcessed.Add(keysToPlay[0]);
                    }
                    else
                    {
                        foreach (String eventName in keysToPlay)
                        {
                            if (eventName.StartsWith(QueuedMessage.compoundMessageIdentifier) || eventName.StartsWith(QueuedMessage.driverNameIdentifier)
                                || enabledSounds.Contains(eventName))
                            {
                                oneOrMoreEventsEnabled = true;
                            }
                        }
                    }
                }
                if (queueToPlay.Count > 0 && keysToPlay.Count == 0)
                {
                    Console.WriteLine("None of the " + queueToPlay.Count + " message(s) in this queue is due or valid");
                }
            }            
            Boolean wasInterrupted = false;
            if (oneOrMoreEventsEnabled)
            {
                // block for immediate messages...
                if (isImmediateMessages)
                {
                    lock (queueToPlay)
                    {
                        openRadioChannelInternal();
                        soundsProcessed.AddRange(playSounds(keysToPlay, isImmediateMessages, out wasInterrupted));
                    }
                }
                else
                {
                    // for queued messages, allow other messages to be inserted into the queue while these are being read
                    openRadioChannelInternal();
                    soundsProcessed.AddRange(playSounds(keysToPlay, isImmediateMessages, out wasInterrupted));
                }                
            }
            else
            {
                soundsProcessed.AddRange(keysToPlay);
            }
            if (soundsProcessed.Count > 0)
            {
                lock (queueToPlay)
                {
                    foreach (String key in soundsProcessed)
                    {
                        if (queueToPlay.Contains(key))
                        {
                            queueToPlay.Remove(key);
                        }
                    }
                }
            }            
            if (queueHasDueMessages(queueToPlay, isImmediateMessages) && !wasInterrupted && !isImmediateMessages)
            {
                Console.WriteLine("There are " + queueToPlay.Count + " more events in the queue, playing them...");
                playQueueContents(queueToPlay, isImmediateMessages);
            }
        }

        private Boolean queueHasDueMessages(OrderedDictionary queueToCheck, Boolean isImmediateMessages)
        {
            if (isImmediateMessages)
            {
                // immediate messages can't be delayed so no point in checking their due times
                return queueToCheck.Count > 0;
            }
            else if (queueToCheck.Count == 0)
            {
                return false;
            }            
            else
            {
                long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                lock (queueToCheck)
                {
                    foreach (String key in queueToCheck.Keys)
                    {
                        if (((QueuedMessage)queueToCheck[key]).dueTime <= milliseconds)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }            
        }

        private List<String> playSounds(List<String> eventNames, Boolean isImmediateMessages, out Boolean wasInterrupted)
        {
            Console.WriteLine("Playing sounds, events: " + String.Join(", ", eventNames));
            List<String> soundsProcessed = new List<String>();
            OrderedDictionary thisQueue = isImmediateMessages ? immediateClips : queuedClips;
            wasInterrupted = false;
            int playedEventCount = 0;
            foreach (String eventName in eventNames)
            {
                // if there's anything in the immediateClips queue, stop processing
                if (isImmediateMessages || immediateClips.Count == 0)
                {
                    if (thisQueue.Contains(eventName))
                    {
                        QueuedMessage thisMessage = (QueuedMessage)thisQueue[eventName];
                        if (eventName.StartsWith(QueuedMessage.compoundMessageIdentifier) || eventName.StartsWith(QueuedMessage.driverNameIdentifier)
                            || enabledSounds.Contains(eventName))
                        {
                            if (clipIsPearlOfWisdom(eventName))
                            {
                                if (hasPearlJustBeenPlayed())
                                {
                                    Console.WriteLine("Rejecting pearl of wisdom " + eventName +
                                        " because one has been played in the last " + minTimeBetweenPearlsOfWisdom + " seconds");
                                    soundsProcessed.Add(eventName);
                                    continue;
                                }
                                else
                                {
                                    timeLastPearlOfWisdomPlayed = DateTime.UtcNow;
                                }
                            }
                            if (eventName.StartsWith(QueuedMessage.compoundMessageIdentifier))
                            {                                
                                foreach (String message in thisMessage.getMessageFolders())
                                {
                                    if (message.StartsWith(QueuedMessage.driverNameIdentifier)) 
                                    {
                                        String driverName = message.Substring(QueuedMessage.driverNameIdentifier.Length);
                                        Console.WriteLine("Found a driver name!");
                                        if (driverNameClips.ContainsKey(driverName))
                                        {
                                            driverNameClips[driverName].PlaySync();
                                        }
                                    }
                                    else
                                    {
                                        List<SoundPlayer> clipsList = clips[message];
                                        int index = random.Next(0, clipsList.Count);
                                        SoundPlayer clip = clipsList[index];
                                        clip.PlaySync();
                                    }                                   
                                }
                            }
                            else if (eventName.StartsWith(QueuedMessage.driverNameIdentifier))
                            {
                                String driverName = eventName.Substring(QueuedMessage.driverNameIdentifier.Length);
                                Console.WriteLine("Found a driver name!");
                                if (driverNameClips.ContainsKey(driverName))
                                {
                                    driverNameClips[driverName].PlaySync();
                                }
                            }
                            else
                            {
                                List<SoundPlayer> clipsList = clips[eventName];
                                int index = random.Next(0, clipsList.Count);
                                SoundPlayer clip = clipsList[index];
                                clip.PlaySync();
                            }
                            if (playedMessagesCount.ContainsKey(eventName))
                            {
                                int count = playedMessagesCount[eventName] + 1;
                                playedMessagesCount[eventName] = count;
                            }
                            else
                            {
                                playedMessagesCount.Add(eventName, 1);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Event " + eventName + " is disabled");
                        }
                        soundsProcessed.Add(eventName);
                        playedEventCount++;
                    }                    
                }
                else
                {
                    Console.WriteLine("we've been interrupted after playing " + playedEventCount + " events");
                    wasInterrupted = true;
                    break;
                }
            }
            if (soundsProcessed.Count == 0)
            {
                Console.WriteLine("Processed no messages in this queue");
                holdChannelOpen = true;
            }
            else
            {
                Console.WriteLine("Processed " + String.Join(", ", soundsProcessed.ToArray()));
            }
            return soundsProcessed;
        }

        private void openRadioChannelInternal()
        {
            if (!channelOpen)
            {
                channelOpen = true;                
                if (getBackgroundVolume() > 0 && loadNewBackground && backgroundToLoad != null)
                {
                    Console.WriteLine("Setting background sounds file to  " + backgroundToLoad);
                    String path = Path.Combine(backgroundFilesPath, backgroundToLoad);
                    if (!backgroundPlayerInitialised)
                    {
                        initialiseBackgroundPlayer();
                    }
                    backgroundPlayer.Volume = getBackgroundVolume();
                    backgroundPlayer.Open(new System.Uri(path, System.UriKind.Absolute));
                    loadNewBackground = false;
                }

                // this looks like we're doing it the wrong way round but there's a short
                // delay playing the event sound, so if we kick off the background before the bleep
                if (getBackgroundVolume() > 0)
                {
                    if (!backgroundPlayerInitialised)
                    {
                        initialiseBackgroundPlayer();
                    }
                    backgroundPlayer.Volume = getBackgroundVolume();
                    int backgroundDuration = 0;
                    int backgroundOffset = 0;
                    if (backgroundPlayer.NaturalDuration.HasTimeSpan)
                    {
                        backgroundDuration = (backgroundPlayer.NaturalDuration.TimeSpan.Minutes * 60) +
                            backgroundPlayer.NaturalDuration.TimeSpan.Seconds;
                        //Console.WriteLine("Duration from file is " + backgroundDuration);
                        backgroundOffset = random.Next(0, backgroundDuration - backgroundLeadout);
                    }
                    //Console.WriteLine("Background offset = " + backgroundOffset);
                    backgroundPlayer.Position = TimeSpan.FromSeconds(backgroundOffset);
                    backgroundPlayer.Play();
                }

                if (enableStartBleep)
                {                    
                    String bleepName;
                    if (useShortBeepWhenOpeningChannel)
                    {
                        bleepName = "short_bleep";
                    }
                    else
                    {
                        bleepName = "start_bleep";
                    }
                    List<SoundPlayer> bleeps = clips[bleepName];
                    int bleepIndex = random.Next(0, bleeps.Count);
                    Console.WriteLine("*** Opening channel, using bleep " + bleepName + " at position " + bleepIndex);
                    bleeps[bleepIndex].PlaySync();                    
                }
            }
        }

        private void closeRadioInternalChannel()
        {
            if (channelOpen)
            {
                Console.WriteLine("*** Closing channel");
                if (enableEndBleep)
                {
                    List<SoundPlayer> bleeps = clips["end_bleep"];
                    int bleepIndex = random.Next(0, bleeps.Count);
                    bleeps[bleepIndex].PlaySync();
                }
                if (getBackgroundVolume() > 0)
                {
                    if (!backgroundPlayerInitialised)
                    {
                        initialiseBackgroundPlayer();
                    }
                    backgroundPlayer.Stop();
                }                                
                channelOpen = false;
            }
            useShortBeepWhenOpeningChannel = false;
        }

        /**
         * If this queue only contains a single gap filler message, always play it. If it contains 2
         * messages we play it 50% of the time. Otherwise it's not played.
         */
        private Boolean playGapFillerMessage(OrderedDictionary queueToPlay)
        {
            return queueToPlay.Count == 1 || (queueToPlay.Count == 2 && random.NextDouble() >= 0.5);
        }

        public void purgeQueues()
        {
            foreach (KeyValuePair<string, List<SoundPlayer>> entry in clips)
            {
                foreach (SoundPlayer clip in entry.Value)
                {
                    clip.Stop();
                }
            }
            lock (queuedClips)
            {
                queuedClips.Clear();
            }
            lock (immediateClips)
            {
                immediateClips.Clear();
            }
        }

        public void queueClip(String eventName, int secondsDelay, AbstractEvent abstractEvent)
        {
            queueClip(eventName, secondsDelay, abstractEvent, PearlsOfWisdom.PearlType.NONE, 0);
        }

        // we pass in the event which triggered this clip so that we can query the event before playing the
        // clip to check if it's still valid against the latest game state. This is necessary for clips queued
        // with non-zero delays (e.g. you might have crossed the start / finish line between the clip being 
        // queued and it being played)
        public void queueClip(String eventName, int secondsDelay, AbstractEvent abstractEvent,
            PearlsOfWisdom.PearlType pearlType, double pearlMessageProbability)
        {
            queueClip(eventName, new QueuedMessage(secondsDelay, abstractEvent), pearlType, pearlMessageProbability);
        }

        public void queueClip(String eventName, QueuedMessage queuedMessage)
        {
            queueClip(eventName, queuedMessage, PearlsOfWisdom.PearlType.NONE, 0);
        }

        public void openChannel()
        {
            openChannel(false);
        }

        public void holdOpenChannel()
        {
            holdOpenChannel(false);
        }

        public void openChannel(Boolean useShortBeep)
        {
            useShortBeepWhenOpeningChannel = useShortBeep;
            requestChannelOpen = true;
        }

        public void holdOpenChannel(Boolean useShortBeep)
        {
            useShortBeepWhenOpeningChannel = useShortBeep;
            requestChannelOpen = true;
            holdChannelOpen = true;
            requestChannelClose = false;
        }

        public void closeChannel()
        {
            requestChannelClose = true;
        }

        public Boolean isChannelOpen()
        {
            return channelOpen;
        }

        public void playClipImmediately(String eventName, QueuedMessage queuedMessage)
        {
            if (disableImmediateMessages)
            {
                return;
            }
            lock (immediateClips)
            {
                if (immediateClips.Contains(eventName))
                {
                    Console.WriteLine("Clip for event " + eventName + " is already queued, ignoring");
                    return;
                }
                else
                {
                    immediateClips.Add(eventName, queuedMessage);
                }
            }
        }

        public void queueClip(String eventName, QueuedMessage queuedMessage, PearlsOfWisdom.PearlType pearlType, double pearlMessageProbability)
        {
            lock (queuedClips)
            {
                if (queuedClips.Contains(eventName))
                {
                    Console.WriteLine("Clip for event " + eventName + " is already queued, ignoring");
                    return;
                }
                else
                {
                    PearlsOfWisdom.PearlMessagePosition pearlPosition = PearlsOfWisdom.PearlMessagePosition.NONE;
                    if (pearlType != PearlsOfWisdom.PearlType.NONE && checkPearlOfWisdomValid(pearlType))
                    {
                        pearlPosition = pearlsOfWisdom.getMessagePosition(pearlMessageProbability);
                    }
                    if (pearlPosition == PearlsOfWisdom.PearlMessagePosition.BEFORE)
                    {
                        QueuedMessage pearlQueuedMessage = new QueuedMessage(queuedMessage.abstractEvent);
                        pearlQueuedMessage.dueTime = queuedMessage.dueTime;
                        queuedClips.Add(PearlsOfWisdom.getMessageFolder(pearlType), pearlQueuedMessage);
                    }
                    queuedClips.Add(eventName, queuedMessage);
                    if (pearlPosition == PearlsOfWisdom.PearlMessagePosition.AFTER)
                    {
                        QueuedMessage pearlQueuedMessage = new QueuedMessage(queuedMessage.abstractEvent);
                        pearlQueuedMessage.dueTime = queuedMessage.dueTime;
                        queuedClips.Add(PearlsOfWisdom.getMessageFolder(pearlType), pearlQueuedMessage);
                    }
                }
            }
        }

        public Boolean removeQueuedClip(String eventName)
        {
            lock (queuedClips)
            {
                if (queuedClips.Contains(eventName))
                {
                    queuedClips.Remove(eventName);
                    return true;
                }
                return false;
            }
        }

        public Boolean removeImmediateClip(String eventName)
        {
            if (disableImmediateMessages)
            {
                return false;
            }
            lock (immediateClips)
            {
                if (immediateClips.Contains(eventName))
                {
                    immediateClips.Remove(eventName);
                    return true;
                }
                return false;
            }
        }

        private void openAndCacheClip(String eventName, String file)
        {
            SoundPlayer clip = new SoundPlayer(file);
            clip.Load();
            if (!clips.ContainsKey(eventName))
            {
                clips.Add(eventName, new List<SoundPlayer>());
            }
            clips[eventName].Add(clip);
           // Console.WriteLine("cached clip " + file + " into set " + eventName);
        }

        private void backgroundPlayer_MediaEnded(object sender, EventArgs e)
        {
            Console.WriteLine("looping...");
            backgroundPlayer.Position = TimeSpan.FromMilliseconds(1);
        }

        // checks that another pearl isn't already queued. If one of the same type is already
        // in the queue this method just returns false. If a conflicting pearl is in the queue
        // this method removes it and returns false, so we don't end up with, for example, 
        // a 'keep it up' message in a block that contains a 'your lap times are worsening' message
        private Boolean checkPearlOfWisdomValid(PearlsOfWisdom.PearlType newPearlType)
        {
            Boolean isValid = true;
            if (queuedClips != null && queuedClips.Count > 0)
            {
                List<String> pearlsToPurge = new List<string>();
                foreach (String eventName in queuedClips.Keys)
                {
                    if (clipIsPearlOfWisdom(eventName))
                    {
                        Console.WriteLine("There's already a pearl in the queue, can't add another");
                        isValid = false;
                        if (eventName != PearlsOfWisdom.getMessageFolder(newPearlType))
                        {
                            pearlsToPurge.Add(eventName);
                        }
                    }
                }
                foreach (String pearlToPurge in pearlsToPurge)
                {
                    queuedClips.Remove(pearlToPurge);
                    Console.WriteLine("Queue contains a pearl " + pearlToPurge + " which conflicts with " + newPearlType);
                }
            }
            return isValid;
        }

        private Boolean clipIsPearlOfWisdom(String eventName)
        {
            foreach (PearlsOfWisdom.PearlType pearlType in Enum.GetValues(typeof(PearlsOfWisdom.PearlType)))
            {
                if (pearlType != PearlsOfWisdom.PearlType.NONE && PearlsOfWisdom.getMessageFolder(pearlType) == eventName)
                {
                    return true;
                }
            }
            return false;
        }

        private Boolean hasPearlJustBeenPlayed()
        {
            return timeLastPearlOfWisdomPlayed.Add(minTimeBetweenPearlsOfWisdom) > DateTime.UtcNow;
        }
    }
}
