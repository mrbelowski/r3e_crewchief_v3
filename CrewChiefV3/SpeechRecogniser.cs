using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrewChiefV3.Events;
using System.Threading;

namespace CrewChiefV3
{
    class SpeechRecogniser : IDisposable
    {
        private SpeechRecognitionEngine sre;

        private String location = UserSettings.GetUserSettings().getString("speech_recognition_location");

        // externalise these?
        public static String FUEL = "fuel";
        public static String FUEL_LEVEL = "fuel level";
        public static String TYRE_WEAR = "tyre wear";
        public static String TYRE_TEMPS = "tyre temps";
        public static String TYRE_TEMPERATURES = "tyre temperatures";
        public static String BRAKE_TEMPS = "brake temps";
        public static String BRAKE_TEMPERATURES = "brake temperatures";
        public static String AERO = "aero";
        public static String BODY_WORK = "body work";
        public static String TRANSMISSION = "transmission";
        public static String ENGINE = "engine";
        public static String SUSPENSION = "suspension";
        public static String BRAKES = "brakes";
        public static String ENGINE_TEMPS = "engine temps";
        public static String ENGINE_TEMPERATURES = "engine temperatures";
        public static String GAP_IN_FRONT = "gap in front";
        public static String GAP_AHEAD = "gap ahead";
        public static String GAP_BEHIND = "gap behind";
        public static String LAST_LAP_TIME = "last lap time";
        public static String LAP_TIME = "lap time";
        public static String LAST_LAP = "last lap";
        public static String POSITION = "position";
        public static String PEA = "pea";
        public static String PACE = "pace";

        private static String KEEP_QUIET = "keep quiet";
        private static String SHUT_UP = "shut up";
        private static String I_KNOW_WHAT_IM_DOING = "I know what I'm doing";
        private static String LEAVE_ME_ALONE = "leave me alone";
        private static String DONT_TELL_ME_THE_GAPS = "don't tell me the gaps";
        private static String DONT_TELL_ME_THE_DELTAS = "don't tell me the deltas";
        private static String DONT_GIVE_ME_THE_DELTAS = "don't give me the deltas"; 
        private static String NO_MORE_DELTAS = "no more deltas";
        private static String NO_MORE_GAPS = "no more gaps";

        private static String KEEP_ME_UPDATED = "keep me updated";
        private static String KEEP_ME_INFORMED = "keep me informed";
        private static String KEEP_ME_POSTED = "keep me posted";
        private static String TELL_ME_THE_GAPS = "tell me the gaps";
        private static String TELL_ME_THE_DELTAS = "tell me the deltas";
        private static String GIVE_ME_THE_DELTAS = "give me the deltas";

        private static String HOW_LONGS_LEFT = "how long's left";
        private static String HOW_MANY_LAPS_LEFT = "how many laps left";
        private static String HOW_MANY_LAPS_TO_GO = "how many laps to go";

        private static String SPOT = "spot";
        private static String DONT_SPOT = "don't spot";

        public static String DO_I_STILL_HAVE_A_PENALTY = "do I still have a penalty";
        public static String DO_I_HAVE_A_PENALTY = "do I have a penalty";
        public static String HAVE_I_SERVED_MY_PENALTY = "have I served my penalty";

        public static String DO_I_HAVE_TO_PIT = "do I have to pit";
        public static String DO_I_NEED_TO_PIT = "do I need to pit";
        public static String DO_I_HAVE_A_MANDATORY_PIT_STOP = "do I have a mandatory pit stop";
        public static String DO_I_HAVE_A_MANDATORY_STOP = "do I have a mandatory stop";
        public static String DO_I_HAVE_TO_MAKE_A_PIT_STOP = "do I have to make a pit stop";

        public static String WHERE_IS = "where's";
        public static String WHOS_IN_FRONT = "who's in front";
        public static String WHOS_BEHIND = "who's behind";
        public static String WHOS_LEADING = "who's leading";
        public static String WHAT_WAS = "what was";
        public static String WHAT_IS = "what is";
        public static String BEST_LAP = "best lap";
        public static String THE_LEADER = "the leader";
        public static String THE_CAR_AHEAD = "the car ahead";
        public static String THE_CAR_IN_FRONT = "the car in front";
        public static String THE_GUY_AHEAD = "the guy ahead";
        public static String THE_GUY_IN_FRONT = "the guy in front";
        public static String THE_CAR_BEHIND = "the car behind";
        public static String THE_GUY_BEHIND = "the guy behind;";

        public static String REPEAT_LAST_MESSAGE = "repeat last message";
        public static String SAY_AGAIN = "say again";

        private float confidenceLimit = 0.5f;

        private CrewChief crewChief;

        public Boolean initialised = false;

        public MainWindow.VoiceOptionEnum voiceOptionEnum;

        private Grammar namesGrammar = null;
        
        private List<String> loadedDriverNames;

        private System.Globalization.CultureInfo cultureInfo;

        public void Dispose()
        {
            if (sre != null)
            {
                sre.Dispose();
                sre = null;
            }
            initialised = false;
        }

        public SpeechRecogniser(CrewChief crewChief)
        {
            this.crewChief = crewChief;
        }

        public void initialiseSpeechEngine()
        {
            initialised = false;
            try
            {
                cultureInfo = new System.Globalization.CultureInfo(location);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to initialise culture info object for location " + location +
                    ". Check that MSSpeech_SR_" + location + "_TELE.msi is installed");
                Console.WriteLine("Exception message: " + e.Message);
                return;
            }
            try
            {
                this.sre = new SpeechRecognitionEngine(cultureInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to initialise speech recognition engine, check that SpeechPlatformRuntime.msi is installed and " +
                    " MSSpeech_SR_" + location + "_TELE.msi is installed");
                Console.WriteLine("Exception message: " + e.Message);
                return;
            }

            try
            {
                sre.SetInputToDefaultAudioDevice();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to set default audio device");
                Console.WriteLine("Exception message: " + e.Message);
                return;
            }
            try
            {
                Choices info0 = new Choices();
                info0.Add(new string[] { "how's my", "how is my" });
                Choices info1 = new Choices();
                info1.Add(new string[] { FUEL, TYRE_WEAR, AERO, BODY_WORK, TRANSMISSION, ENGINE, SUSPENSION, PACE, 
                    TYRE_TEMPS, TYRE_TEMPERATURES, BRAKE_TEMPS, BRAKE_TEMPERATURES, BRAKES, ENGINE_TEMPS, ENGINE_TEMPERATURES });
                GrammarBuilder gb1 = new GrammarBuilder();
                gb1.Culture = cultureInfo;
                gb1.Append(info0);
                gb1.Append(info1);
                Grammar g1 = new Grammar(gb1);

                Choices info2 = new Choices();
                info2.Add(new string[] { GAP_IN_FRONT, GAP_AHEAD, GAP_BEHIND, LAST_LAP, LAP_TIME, LAST_LAP_TIME, POSITION, FUEL_LEVEL });
                GrammarBuilder gb2 = new GrammarBuilder();
                gb2.Culture = cultureInfo;
                gb2.Append("what's my");
                gb2.Append(info2);
                Grammar g2 = new Grammar(gb2);

                Choices info3 = new Choices();
                info3.Add(new string[] { KEEP_QUIET, SHUT_UP, I_KNOW_WHAT_IM_DOING, LEAVE_ME_ALONE, DONT_TELL_ME_THE_GAPS, DONT_GIVE_ME_THE_DELTAS, DONT_TELL_ME_THE_GAPS,
                    NO_MORE_DELTAS, NO_MORE_GAPS});
                GrammarBuilder gb3 = new GrammarBuilder();
                gb3.Culture = cultureInfo;
                gb3.Append(info3);
                Grammar g3 = new Grammar(gb3);

                Choices info4 = new Choices();
                info4.Add(new string[] { KEEP_ME_INFORMED, KEEP_ME_POSTED, KEEP_ME_UPDATED, TELL_ME_THE_GAPS, GIVE_ME_THE_DELTAS, TELL_ME_THE_DELTAS });
                GrammarBuilder gb4 = new GrammarBuilder();
                gb4.Culture = cultureInfo;
                gb4.Append(info4);
                Grammar g4 = new Grammar(gb4);

                Choices info5 = new Choices();
                info5.Add(new string[] { HOW_LONGS_LEFT, HOW_MANY_LAPS_LEFT, HOW_MANY_LAPS_TO_GO });
                GrammarBuilder gb5 = new GrammarBuilder();
                gb5.Culture = cultureInfo;
                gb5.Append(info5);
                Grammar g5 = new Grammar(gb5);

                Choices info6 = new Choices();
                info6.Add(new string[] { SPOT, DONT_SPOT, REPEAT_LAST_MESSAGE, SAY_AGAIN });
                GrammarBuilder gb6 = new GrammarBuilder();
                gb6.Culture = cultureInfo;
                gb6.Append(info6);
                Grammar g6 = new Grammar(gb6);

                Choices info7 = new Choices();
                info7.Add(new string[] { HAVE_I_SERVED_MY_PENALTY, DO_I_HAVE_A_PENALTY, DO_I_STILL_HAVE_A_PENALTY });
                GrammarBuilder gb7 = new GrammarBuilder();
                gb7.Culture = cultureInfo;
                gb7.Append(info7);
                Grammar g7 = new Grammar(gb7);

                Choices info8 = new Choices();
                info8.Add(new string[] { DO_I_HAVE_A_MANDATORY_PIT_STOP, DO_I_NEED_TO_PIT, DO_I_HAVE_A_MANDATORY_STOP, DO_I_HAVE_TO_MAKE_A_PIT_STOP, DO_I_HAVE_TO_PIT });
                GrammarBuilder gb8 = new GrammarBuilder();
                gb8.Culture = cultureInfo;
                gb8.Append(info8);
                Grammar g8 = new Grammar(gb8);

                Choices info9 = new Choices();
                info9.Add(new string[] { TYRE_TEMPS, TYRE_TEMPERATURES, BRAKE_TEMPS, BRAKE_TEMPERATURES, BRAKES, ENGINE_TEMPS, ENGINE_TEMPERATURES });
                GrammarBuilder gb9 = new GrammarBuilder();
                gb9.Culture = cultureInfo;
                gb9.Append("how are my");
                gb9.Append(info9);
                Grammar g9 = new Grammar(gb9);

                sre.LoadGrammar(g1);
                sre.LoadGrammar(g2);
                sre.LoadGrammar(g3);
                sre.LoadGrammar(g4);
                sre.LoadGrammar(g5);
                sre.LoadGrammar(g6);
                sre.LoadGrammar(g7);
                sre.LoadGrammar(g8);
                sre.LoadGrammar(g9);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to configure speech engine grammar");
                Console.WriteLine("Exception message: " + e.Message);
                return;
            }
            sre.InitialSilenceTimeout = TimeSpan.Zero;
            try
            {
                sre.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(sre_SpeechRecognized);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to add event handler to speech engine");
                Console.WriteLine("Exception message: " + e.Message);
                return;
            }
            initialised = true;
        }

        public void addNames(List<String> names)
        {
            if (loadedDriverNames == null ||
                !loadedDriverNames.All(names.Contains))
            {
                if (namesGrammar != null)
                {
                    sre.UnloadGrammar(namesGrammar);
                    Console.WriteLine("Unloaded names");
                }
                Choices nameChoices = new Choices();
                foreach (String name in names)
                {
                    nameChoices.Add(WHERE_IS + " " + name);
                    nameChoices.Add(WHAT_WAS + " " + name + "'s " + LAST_LAP);
                    nameChoices.Add(WHAT_IS + " " + name + "'s " + BEST_LAP);
                }
                for (int i = 0; i < 30; i++)
                {
                    nameChoices.Add(WHAT_WAS + " " + POSITION + " " + i + "'s " + LAST_LAP);
                    nameChoices.Add(WHAT_IS + " " + POSITION + " " + i + "'s " + BEST_LAP);
                    nameChoices.Add(WHAT_WAS + " " + POSITION + " " + i + "'s " + LAST_LAP);
                    nameChoices.Add(WHAT_IS + " " + POSITION + " " + i + "'s " + BEST_LAP);
                    nameChoices.Add(WHAT_WAS + " " + PEA + " " + i + "'s " + LAST_LAP);
                    nameChoices.Add(WHAT_IS + " " + PEA + " " + i + "'s " + BEST_LAP);
                    nameChoices.Add(WHAT_WAS + " " + PEA + " " + i + "'s " + LAST_LAP);
                    nameChoices.Add(WHAT_IS + " " + PEA + " " + i + "'s " + BEST_LAP);
                }
                nameChoices.Add(WHAT_IS + " " + THE_LEADER +"'s " + BEST_LAP);
                nameChoices.Add(WHAT_WAS + " " + THE_LEADER + "'s " + LAST_LAP);
                nameChoices.Add(WHAT_IS + " " + THE_GUY_IN_FRONT + "'s " + BEST_LAP);
                nameChoices.Add(WHAT_WAS + " " + THE_CAR_IN_FRONT + "'s " + LAST_LAP);
                nameChoices.Add(WHAT_IS + " " + THE_GUY_AHEAD + "'s " + BEST_LAP);
                nameChoices.Add(WHAT_WAS + " " + THE_CAR_AHEAD + "'s " + LAST_LAP);
                nameChoices.Add(WHAT_IS + " " + THE_CAR_BEHIND + "'s " + BEST_LAP);
                nameChoices.Add(WHAT_WAS + " " + THE_GUY_BEHIND + "'s " + LAST_LAP);

                nameChoices.Add(WHOS_BEHIND);
                nameChoices.Add(WHOS_IN_FRONT);
                nameChoices.Add(WHOS_LEADING);
                GrammarBuilder nameGB = new GrammarBuilder();
                nameGB.Culture = cultureInfo;
                nameGB.Append(nameChoices);
                namesGrammar = new Grammar(nameGB);
                sre.LoadGrammar(namesGrammar);
                Console.WriteLine("loaded names: " + String.Join(", ", names));
                loadedDriverNames = names;
            }
            else
            {
                Console.WriteLine("Driver names are already loaded");
            }
        }
        
        void sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Console.WriteLine("recognised : " + e.Result.Text + " confidence = " + e.Result.Confidence);
            if (e.Result.Confidence > confidenceLimit)
            {
                try
                {
                    if (e.Result.Grammar == namesGrammar)
                    {
                        CrewChief.getEvent("Opponents").respond(e.Result.Text);
                    }
                    else if (e.Result.Text.Contains(REPEAT_LAST_MESSAGE) || e.Result.Text.Contains(SAY_AGAIN))
                    {
                        crewChief.audioPlayer.repeatLastMessage();
                    }
                    else
                    {
                        AbstractEvent abstractEvent = getEventForSpeech(e.Result.Text);
                        if (abstractEvent != null)
                        {
                            abstractEvent.respond(e.Result.Text);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Unable to respond - error message: " + exception.Message);
                }
            }
            else
            {
                crewChief.youWot();
            }

            recognizeAsyncStop();
            Thread.Sleep(500);
            if (voiceOptionEnum == MainWindow.VoiceOptionEnum.ALWAYS_ON ||
                voiceOptionEnum == MainWindow.VoiceOptionEnum.TOGGLE)
            {
                Console.WriteLine("restarting speech recognition");
                recognizeAsync();
            }
        }

        public void recognizeAsync()
        {
            sre.RecognizeAsync(RecognizeMode.Multiple);
        }

        public void recognizeAsyncStop()
        {
            sre.RecognizeAsyncStop();
        }

        public void recognizeAsyncCancel()
        {
            sre.RecognizeAsyncCancel();
        }

        private AbstractEvent getEventForSpeech(String recognisedSpeech)
        {
            if (recognisedSpeech.Contains(DONT_SPOT))
            {
                crewChief.disableSpotter();
            }
            else if (recognisedSpeech.Contains(SPOT))
            {
                crewChief.enableSpotter();
            }
            else if (recognisedSpeech.Contains(KEEP_QUIET) ||
                recognisedSpeech.Contains(SHUT_UP) ||
                recognisedSpeech.Contains(I_KNOW_WHAT_IM_DOING) ||
                recognisedSpeech.Contains(LEAVE_ME_ALONE))
            {
                crewChief.enableKeepQuietMode();
            }
            else if (recognisedSpeech.Contains(DONT_TELL_ME_THE_GAPS) || recognisedSpeech.Contains(DONT_TELL_ME_THE_DELTAS) ||
                recognisedSpeech.Contains(DONT_GIVE_ME_THE_DELTAS) || recognisedSpeech.Contains(NO_MORE_DELTAS) ||
                recognisedSpeech.Contains(NO_MORE_GAPS))
            {
                crewChief.disableDeltasMode();
            }
            else if (recognisedSpeech.Contains(TELL_ME_THE_GAPS) || recognisedSpeech.Contains(GIVE_ME_THE_DELTAS) ||
                recognisedSpeech.Contains(TELL_ME_THE_DELTAS))
            {
                crewChief.enableDeltasMode();
            }
            else if (recognisedSpeech.Contains(AERO) ||
               recognisedSpeech.Contains(BODY_WORK) ||
               recognisedSpeech.Contains(TRANSMISSION) ||
               recognisedSpeech.Contains(ENGINE) ||
               recognisedSpeech.Contains(SUSPENSION) ||
               recognisedSpeech.Contains(BRAKES))
            {
                return CrewChief.getEvent("DamageReporting");
            }
            else if (recognisedSpeech.Contains(KEEP_ME_UPDATED) ||
                recognisedSpeech.Contains(KEEP_ME_POSTED) ||
                recognisedSpeech.Contains(KEEP_ME_INFORMED))
            {
                crewChief.disableKeepQuietMode();
            }
            else if (recognisedSpeech.Contains(FUEL) || recognisedSpeech.Contains(FUEL_LEVEL))
            {
                return CrewChief.getEvent("Fuel");
            }
            else if (recognisedSpeech.Contains(GAP_IN_FRONT) ||
                recognisedSpeech.Contains(GAP_AHEAD) ||
                recognisedSpeech.Contains(GAP_BEHIND))
            {
                return CrewChief.getEvent("Timings");
            }
            else if (recognisedSpeech.Contains(POSITION))
            {
                return CrewChief.getEvent("Position");
            }
            else if (recognisedSpeech.Contains(LAST_LAP_TIME) ||
                recognisedSpeech.Contains(LAP_TIME) ||
                recognisedSpeech.Contains(LAST_LAP) ||
                recognisedSpeech.Contains(PACE))
            {
                return CrewChief.getEvent("LapTimes");
            }
            else if (recognisedSpeech.Contains(TYRE_TEMPS) ||
                recognisedSpeech.Contains(TYRE_TEMPERATURES) || 
                recognisedSpeech.Contains(TYRE_WEAR) ||
                recognisedSpeech.Contains(BRAKE_TEMPS) ||
                recognisedSpeech.Contains(BRAKE_TEMPERATURES))
            {
                return CrewChief.getEvent("TyreMonitor");
            }
            else if (recognisedSpeech.Contains(HOW_LONGS_LEFT) || 
                recognisedSpeech.Contains(HOW_MANY_LAPS_TO_GO) ||
                recognisedSpeech.Contains(HOW_MANY_LAPS_LEFT))
            {
                return CrewChief.getEvent("RaceTime");
            }
            else if (recognisedSpeech.Contains(DO_I_STILL_HAVE_A_PENALTY) ||
                recognisedSpeech.Contains(DO_I_HAVE_A_PENALTY) ||
                recognisedSpeech.Contains(HAVE_I_SERVED_MY_PENALTY))
            {
                return CrewChief.getEvent("Penalties");
            }
            else if (recognisedSpeech.Contains(DO_I_HAVE_TO_PIT) ||
               recognisedSpeech.Contains(DO_I_HAVE_A_MANDATORY_PIT_STOP) ||
               recognisedSpeech.Contains(DO_I_HAVE_A_MANDATORY_STOP) ||
               recognisedSpeech.Contains(DO_I_NEED_TO_PIT) ||
                recognisedSpeech.Contains(DO_I_HAVE_TO_MAKE_A_PIT_STOP))
            {
                return CrewChief.getEvent("MandatoryPitStops");
            }
            else if (recognisedSpeech.Contains(ENGINE_TEMPS) || recognisedSpeech.Contains(ENGINE_TEMPERATURES))
            {
                return CrewChief.getEvent("EngineMonitor");
            }
            return null;
        }
    }
}
