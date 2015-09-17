using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV3
{
    class PearlsOfWisdom
    {
        public static Boolean enablePearlsOfWisdom = UserSettings.GetUserSettings().getBoolean("enable_pearls_of_wisdom");
        public static float pearlsLikelihood = UserSettings.GetUserSettings().getFloat("pearls_of_wisdom_likelihood");
        public static String folderMustDoBetter = "pearls_of_wisdom/must_do_better";
        public static String folderKeepItUp = "pearls_of_wisdom/keep_it_up";
        public static String folderNeutral = "pearls_of_wisdom/neutral";

        private Random random = new Random();

        public enum PearlType
        {
            GOOD, BAD, NEUTRAL, NONE
        }

        public enum PearlMessagePosition
        {
            BEFORE, AFTER, NONE
        }

        public PearlMessagePosition getMessagePosition(double messageProbability)
        {
            if (enablePearlsOfWisdom && messageProbability * pearlsLikelihood > random.NextDouble())
            {
                if (random.NextDouble() < 0.33)
                {
                    return PearlMessagePosition.BEFORE;
                }
                else
                {
                    return PearlMessagePosition.AFTER;
                }
            }
            return PearlMessagePosition.NONE;
        }

        public static String getMessageFolder(PearlType pearlType)
        {
            switch (pearlType)
            {
                case PearlType.GOOD:
                    return folderKeepItUp;
                case PearlType.BAD:
                    return folderMustDoBetter;
                case PearlType.NEUTRAL:
                    return folderNeutral;
                default:
                    Console.WriteLine("Error getting pearl type for type " + pearlType);
                    return "";
            }
        }
    }
}
