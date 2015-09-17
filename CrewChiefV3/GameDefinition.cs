using CrewChiefV3.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV3
{
    public enum GameEnum
    {
        RACE_ROOM, PCARS_64BIT, PCARS_32BIT
    }
    class GameDefinition
    {
        public static GameDefinition pCars64Bit = new GameDefinition(GameEnum.PCARS_64BIT, "Project Cars (64 bit)", "pCARS64",
            "CrewChiefV3.PCars.PCarsSharedMemoryReader", "CrewChiefV3.PCars.PCarsGameStateMapper", "CrewChiefV3.PCars.PCarsSpotter",
            "pcars64_launch_exe", "pcars64_launch_params", "launch_pcars");
        public static GameDefinition pCars32Bit = new GameDefinition(GameEnum.PCARS_32BIT, "Project Cars (32 bit)", "pCARS32",
            "CrewChiefV3.PCars.PCarsSharedMemoryReader", "CrewChiefV3.PCars.PCarsGameStateMapper", "CrewChiefV3.PCars.PCarsSpotter",
            "pcars64_launch_exe", "pcars64_launch_params", "launch_pcars");
        public static GameDefinition raceRoom = new GameDefinition(GameEnum.RACE_ROOM, "Race Room", "RRRE",
            "CrewChiefV3.RaceRoom.R3ESharedMemoryReader", "CrewChiefV3.RaceRoom.R3EGameStateMapper", "CrewChiefV3.RaceRoom.R3ESpotter",
            "r3e_launch_exe", "r3e_launch_params", "launch_raceroom");

        public static List<GameDefinition> getAllGameDefinitions()
        {
            List<GameDefinition> definitions = new List<GameDefinition>();
            definitions.Add(pCars64Bit); definitions.Add(pCars32Bit); definitions.Add(raceRoom);
            return definitions;
        }

        public static GameDefinition getGameDefinitionForFriendlyName(String friendlyName)
        {
            List<GameDefinition> definitions = getAllGameDefinitions();
            foreach (GameDefinition def in definitions)
            {
                if (def.friendlyName == friendlyName)
                {
                    return def;
                }
            }
            return null;
        }

        public static String[] getGameDefinitionFriendlyNames()
        {
            List<String> names = new List<String>();
            foreach (GameDefinition def in getAllGameDefinitions())
            {
                names.Add(def.friendlyName);
            }
            return names.ToArray();
        }

        public GameEnum gameEnum;
        public String friendlyName;
        public String processName;
        public String gameDataReaderName;
        public String gameStateMapperName;
        public String spotterName;
        public String gameStartCommandProperty;
        public String gameStartCommandOptionsProperty;
        public String gameStartEnabledProperty;

        public GameDefinition(GameEnum gameEnum, String friendlyName, String processName, String gameDataReaderName, String gameStateMapperName, 
            String spotterName, String gameStartCommandProperty, String gameStartCommandOptionsProperty, String gameStartEnabledProperty)
        {
            this.gameEnum = gameEnum;
            this.friendlyName = friendlyName;
            this.processName = processName;
            this.gameDataReaderName = gameDataReaderName;
            this.gameStateMapperName = gameStateMapperName;
            this.spotterName = spotterName;
            this.gameStartCommandProperty = gameStartCommandProperty;
            this.gameStartCommandOptionsProperty = gameStartCommandOptionsProperty;
            this.gameStartEnabledProperty = gameStartEnabledProperty;
        }
    }
}
