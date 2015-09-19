using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/**
 * Utility class to ease some of the pain of managing driver names.
 */
namespace CrewChiefV3
{
    class DriverNameHelper
    {
        private static Dictionary<String, String> rawNameToUsableName = new Dictionary<String, String>();

        private static Dictionary<String, String> usableNamesForSession = new Dictionary<String, String>();

        private static Boolean useLastNameWherePossible = true;

        public static String getUsableNameForRawName(String rawName)
        {
            if (usableNamesForSession.ContainsKey(rawName))
            {
                return usableNamesForSession[rawName];
            }
            else
            {
                return rawName;
            }
        }

        private static void readRawNamesToUsableNamesFile(String soundsFolderName)
        {
            int counter = 0;
            string line;
            StreamReader file = new StreamReader(soundsFolderName +@"\driver_names\names.txt");
            while ((line = file.ReadLine()) != null)
            {
                int separatorIndex = line.LastIndexOf(":");
                if (separatorIndex > 0 && line.Length > separatorIndex + 1)
                {
                    String rawName = line.Substring(0, separatorIndex);
                    String usableName = validateAndCleanUpName(line.Substring(separatorIndex + 1));
                    if (usableName != null && !rawNameToUsableName.ContainsKey(rawName))
                    {
                        rawNameToUsableName.Add(rawName, usableName);
                    }
                }
                counter++;
            }
            file.Close();
        }

        private static String validateAndCleanUpName(String name)
        {
            name = name.Replace('_', ' ');
            name = name.Replace('-', ' ');
            if (name.EndsWith("]") && name.Contains("["))
            {
                name = name.Substring(0, name.LastIndexOf('['));
            }

            if (name.All(c=>Char.IsLetter(c) || c==' ' || c=='\'' || c=='.') && name.Length > 0) {
                return name.Trim();
            }
            else
            {
                return null;
            }
        }

        public static List<String> getUsableDriverNames(List<String> rawDriverNames, String soundsFolderName)
        {
            readRawNamesToUsableNamesFile(soundsFolderName);
            usableNamesForSession.Clear();
            foreach (String rawDriverName in rawDriverNames)
            {
                if (rawNameToUsableName.ContainsKey(rawDriverName))
                {
                    String usableDriverName = rawNameToUsableName[rawDriverName];
                    if (!usableNamesForSession.ContainsKey(rawDriverName))
                    {
                        Console.WriteLine("Using mapped drivername " + usableDriverName + " for raw driver name " + rawDriverName);
                        usableNamesForSession.Add(rawDriverName, usableDriverName);
                    }                    
                }
                else
                {
                    String usableDriverName = validateAndCleanUpName(rawDriverName);
                    if (usableDriverName != null)
                    {
                        Boolean usedLastName = false;
                        if (useLastNameWherePossible)
                        {
                            String lastName = getUnambiguousLastName(usableDriverName);
                            if (lastName != null && lastName.Count() > 0 && !usableNamesForSession.ContainsKey(rawDriverName))
                            {
                                Console.WriteLine("Using unmapped driver last name " + lastName + " for raw driver name " + rawDriverName);
                                usableNamesForSession.Add(rawDriverName, lastName);
                                usedLastName = true;
                            }
                        }
                        if (!usedLastName && !usableNamesForSession.ContainsKey(rawDriverName))
                        {
                            Console.WriteLine("Using unmapped drivername " + usableDriverName + " for raw driver name " + rawDriverName);
                            usableNamesForSession.Add(rawDriverName, usableDriverName);
                        }                        
                    }
                    else
                    {
                        Console.WriteLine("Unable to create a usable driver name for " + rawDriverName);
                    }
                }
            }
            return usableNamesForSession.Values.ToList();
        }

        private static String getUnambiguousLastName(String fullName)
        {
            if (fullName.Count(Char.IsWhiteSpace) == 1)
            {
                return fullName.Split(' ')[1];
            }
            if (fullName.LastIndexOf(". ") > 0 && fullName.LastIndexOf(". ") == fullName.LastIndexOf(" ") - 1)
            {
                String[] split = fullName.Split(' ');
                return split[split.Length - 1];
            }
            return null;
        }
    }
}
