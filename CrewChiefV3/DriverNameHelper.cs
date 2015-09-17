using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/**
 * Utility class to ease some of the pain of creating a vast driver name folder set
 */
namespace CrewChiefV3
{
    class DriverNameHelper
    {
        private static Dictionary<String, String> nameToPhonetic = new Dictionary<String, String>();

        static Dictionary<String, String> phoneticNamesForSession = new Dictionary<String, String>();

        public static String getPhoneticForRealName(String realName)
        {
            if (phoneticNamesForSession.ContainsKey(realName))
            {
                return phoneticNamesForSession[realName];
            }
            else
            {
                return realName;
            }
        }

        private static void readNamesToPhoneticsFile(String soundsFolderName)
        {
            int counter = 0;
            string line;
            StreamReader file = new StreamReader(soundsFolderName +@"\driver_names\names.txt");
            while ((line = file.ReadLine()) != null)
            {
                String[] split = line.Split(new char[] {':'});
                if (!nameToPhonetic.ContainsKey(split[0].Trim()))
                {
                    String phonetic = "";
                    if (split.Length == 2)
                    {
                        phonetic = split[1].Trim();
                    }
                    nameToPhonetic.Add(split[0].Trim(), phonetic);
                }
                counter++;
            }
            file.Close();
        }

        public static List<String> getPhoneticDriverNames(List<String> driverNames, String soundsFolderName)
        {
            readNamesToPhoneticsFile(soundsFolderName);
            phoneticNamesForSession.Clear();
            List<String> phoneticNames = new List<String>();
            foreach (String driverName in driverNames)
            {
                String phoneticName = driverName;
                if (nameToPhonetic.ContainsKey(driverName) && nameToPhonetic[driverName].Length > 0)
                {
                    phoneticName = nameToPhonetic[driverName];
                    Console.WriteLine("Using phonetic " + phoneticName + " for driver name " + driverName);
                }
                if (!phoneticNames.Contains(phoneticName))
                {
                    phoneticNames.Add(phoneticName);
                }
                if (!phoneticNamesForSession.ContainsKey(driverName))
                {
                    phoneticNamesForSession.Add(driverName, phoneticName);
                }
            }
            return phoneticNames;
        }
    }
}
