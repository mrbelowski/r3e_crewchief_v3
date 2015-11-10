﻿using System;
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
        // if there's more than 2 names, and the second to last name isn't one of the common middle bits, 
        // use the last part
        private static Boolean optimisticSurnameExtraction = true;

        private static String[] middleBits = new String[] { "van", "de", "da", "le", "la", "von", "di", "eg", "du", "el", "del", "de la", "de le", "saint", "st"};

        private static Dictionary<String, String> lowerCaseRawNameToUsableName = new Dictionary<String, String>();

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
                    String lowerCaseRawName = line.Substring(0, separatorIndex).ToLower();
                    String usableName = validateAndCleanUpName(line.Substring(separatorIndex + 1));
                    if (usableName != null && !lowerCaseRawNameToUsableName.ContainsKey(lowerCaseRawName))
                    {
                        lowerCaseRawNameToUsableName.Add(lowerCaseRawName, usableName);
                    }
                }
                counter++;
            }
            file.Close();
        }

        private static String validateAndCleanUpName(String name)
        {
            try
            {
                name = name.Replace('_', ' ');
                // be a bit careful with hypens - if it's before the first space, just remove it as
                // it's a separated firstname
                if (name.IndexOf(' ') > 0 && name.IndexOf('-') > 0 && name.IndexOf('-') < name.IndexOf(' '))
                {
                    name = name.Replace("-", "");
                }
                name = name.Replace('-', ' ');
                name = name.Replace('.', ' ');
                if (name.EndsWith("]") && name.Contains("["))
                {
                    name = name.Substring(0, name.LastIndexOf('['));
                }
                if (name.StartsWith("[") && name.Contains("]"))
                {
                    name = name.Substring(name.LastIndexOf(']') + 1);
                }
                for (int i = 0; i < 4; i++)
                {
                    if (name.Count() > 1 && Char.IsNumber(name[name.Count() - 1]))
                    {
                        name = name.Substring(0, name.Count() - 1);
                    }
                    else
                    {
                        break;
                    }
                }   
                if (name.All(c=>Char.IsLetter(c) || c==' ' || c=='\'') && name.Length > 0) {
                    return name.Trim();
                }
            }
            catch (Exception)
            {
                
            }
            return null;
        }

        public static String getUsableDriverName(String rawDriverName, String soundsFolderName)
        {
            String usableDriverName = null;
            if (!usableNamesForSession.ContainsKey(rawDriverName))
            {
                readRawNamesToUsableNamesFile(soundsFolderName);
                if (lowerCaseRawNameToUsableName.ContainsKey(rawDriverName.ToLower()))
                {
                    usableDriverName = lowerCaseRawNameToUsableName[rawDriverName.ToLower()];
                    Console.WriteLine("Using mapped drivername " + usableDriverName + " for raw driver name " + rawDriverName);
                    usableNamesForSession.Add(rawDriverName, usableDriverName);
                }
                else
                {
                    usableDriverName = validateAndCleanUpName(rawDriverName);
                    if (usableDriverName != null)
                    {
                        Boolean usedLastName = false;
                        if (useLastNameWherePossible)
                        {
                            String lastName = getUnambiguousLastName(usableDriverName);
                            if (lastName != null && lastName.Count() > 0)
                            {
                                Console.WriteLine("Using unmapped driver last name " + lastName + " for raw driver name " + rawDriverName);
                                usableDriverName = lastName;
                                usableNamesForSession.Add(rawDriverName, usableDriverName);
                                usedLastName = true;
                            }
                        }
                        if (!usedLastName)
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
            return usableDriverName;
        }

        public static List<String> getUsableDriverNames(List<String> rawDriverNames, String soundsFolderName)
        {
            readRawNamesToUsableNamesFile(soundsFolderName);
            usableNamesForSession.Clear();
            foreach (String rawDriverName in rawDriverNames)
            {
                if (lowerCaseRawNameToUsableName.ContainsKey(rawDriverName.ToLower()))
                {
                    String usableDriverName = lowerCaseRawNameToUsableName[rawDriverName.ToLower()];
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
            if (fullName.Count(Char.IsWhiteSpace) == 0) 
            {
                return fullName;
            } 
            else
            {
                String[] fullNameSplit = trimEmptyStrings(fullName.Split(' '));
                if (fullNameSplit.Count() == 2)
                {
                    return fullName.Split(' ')[1];
                }
                else if (fullNameSplit[fullNameSplit.Count() - 2].Length == 1) 
                {
                    return fullNameSplit[fullNameSplit.Count() - 1];
                }
                else if (middleBits.Contains(fullNameSplit[fullNameSplit.Count() - 2].ToLower()))
                {
                    return fullNameSplit[fullNameSplit.Count() - 2] + " " + fullNameSplit[fullNameSplit.Count() - 1];
                }
                else if (fullNameSplit.Length > 3 && middleBits.Contains((fullNameSplit[fullNameSplit.Count() - 3] + " " + fullNameSplit[fullNameSplit.Count() - 2]).ToLower()))
                {
                    return fullNameSplit[fullNameSplit.Count() - 3] + " " + fullNameSplit[fullNameSplit.Count() - 2] + " " + fullNameSplit[fullNameSplit.Count() - 1];
                }
                else if (optimisticSurnameExtraction)
                {
                    return fullNameSplit[fullNameSplit.Count() - 1];
                }
            }
            return null;
        }

        private static String[] trimEmptyStrings(String[] strings)
        {
            List<String> trimmedList = new List<string>();
            foreach (String str in strings) {
                if (str.Trim().Length > 0)
                {
                    trimmedList.Add(str.Trim());
                }
            }
            return trimmedList.ToArray();
        }
    }
}
