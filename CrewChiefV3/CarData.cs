using CrewChiefV3.Events;
using CrewChiefV3.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV3
{
    class CarData
    {
        // some temperatures - maybe externalise these
        private static float maxColdRoadTyreTemp = 50;
        private static float maxWarmRoadTyreTemp = 80;
        private static float maxHotRoadTyreTemp = 100;

        private static float maxColdUnknownRaceTyreTemp = 70;
        private static float maxWarmUnknownRaceTyreTemp = 100;
        private static float maxHotUnknownRaceTyreTemp = 120;

        private static float maxColdDtmOptionTyreTemp = 60;
        private static float maxWarmDtmOptionTyreTemp = 95;
        private static float maxHotDtmOptionTyreTemp = 110;

        private static float maxColdDtmPrimeTyreTemp = 75;
        private static float maxWarmDtmPrimeTyreTemp = 110;
        private static float maxHotDtmPrimeTyreTemp = 130;


        private static float maxColdIronRoadBrakeTemp = 80;
        private static float maxWarmIronRoadBrakeTemp = 300;
        private static float maxHotIronRoadBrakeTemp = 600;

        private static float maxColdIronRaceBrakeTemp = 150;
        private static float maxWarmIronRaceBrakeTemp = 500;
        private static float maxHotIronRaceBrakeTemp = 700;

        private static float maxColdCeramicBrakeTemp = 150;
        private static float maxWarmCeramicBrakeTemp = 800;
        private static float maxHotCeramicBrakeTemp = 1000;

        private static float maxColdCarbonBrakeTemp = 400;
        private static float maxWarmCarbonBrakeTemp = 1200;
        private static float maxHotCarbonBrakeTemp = 1400;


        private static float maxRoadSafeWaterTemp = 96;
        private static float maxRoadSafeOilTemp = 115;

        private static float maxRaceSafeWaterTemp = 105;
        private static float maxRaceSafeOilTemp = 125;

        // for F1, GP2, LMP1 and DTM
        private static float maxExoticRaceSafeWaterTemp = 105;
        private static float maxExoticRaceSafeOilTemp = 140;


        public enum CarClassEnum
        {
            GT1, GT2, GT3, GT4, GT5, Kart, LMP1, LMP2, LMP3, ROAD, ROAD_SUPERCAR, GROUPC, GROUPA, GROUP4, GROUP5, VINTAGE_RACE_SLICKS, 
            VINTAGE_RACE_BIAS_PLY, STOCK_CAR, F1, F2, F3, F4, FF, TC3, TC1, TRANS_AM, UNKNOWN_RACE
        }

        public class CarClass
        {
            public CarClassEnum carClassEnum;
            public String[] classNames;
            public BrakeType brakeType;
            public TyreType defaultTyreType;
            public float maxSafeWaterTemp;
            public float maxSafeOilTemp;            

            public CarClass(CarClassEnum carClassEnum, String[] classNames, BrakeType brakeType, TyreType defaultTyreType, float maxSafeWaterTemp, float maxSafeOilTemp)
            {
                this.carClassEnum = carClassEnum;
                this.classNames = classNames;
                this.brakeType = brakeType;
                this.defaultTyreType = defaultTyreType;
                this.maxSafeOilTemp = maxSafeOilTemp;
                this.maxSafeWaterTemp = maxSafeWaterTemp;
            }
        }

        private static List<CarClass> carClasses = new List<CarClass>();

        public static Dictionary<TyreType, List<CornerData.EnumWithThresholds>> tyreTempThresholds = new Dictionary<TyreType, List<CornerData.EnumWithThresholds>>();
        public static Dictionary<BrakeType, List<CornerData.EnumWithThresholds>> brakeTempThresholds = new Dictionary<BrakeType, List<CornerData.EnumWithThresholds>>();

        private static Dictionary<String, BrakeType> brakeTypesPerCarName = new Dictionary<string, BrakeType>();
        private static Dictionary<String, TyreType> defaultTyreTypesPerCarName = new Dictionary<string, TyreType>();

        static CarData() 
        {
            carClasses.Add(new CarClass(CarClassEnum.UNKNOWN_RACE, new String[] { "" }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));

            carClasses.Add(new CarClass(CarClassEnum.GT1, new String[] { "GT1X" }, BrakeType.Ceramic, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.GT2, new String[] { "GT2" }, BrakeType.Ceramic, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.GT3, new String[] { "GT3" }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.GT4, new String[] { "GT4" }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.GT5, new String[] { "GT5" }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));

            carClasses.Add(new CarClass(CarClassEnum.Kart, new String[] { "Kart1", "Kart2" }, BrakeType.Iron_Road, TyreType.Unknown_Race, maxRoadSafeWaterTemp, maxRoadSafeOilTemp));

            carClasses.Add(new CarClass(CarClassEnum.LMP1, new String[] { "LMP1" }, BrakeType.Carbon, TyreType.Unknown_Race, maxExoticRaceSafeWaterTemp, maxExoticRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.LMP2, new String[] { "LMP2" }, BrakeType.Ceramic, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.LMP3, new String[] { "LMP3" }, BrakeType.Ceramic, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));

            carClasses.Add(new CarClass(CarClassEnum.GROUPC, new String[] { "Group C1" }, BrakeType.Ceramic, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.GROUP5, new String[] { "Group 5" }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.GROUP4, new String[] { "Group 4" }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.GROUPA, new String[] { "Group A", "DTM92" }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp)); // just for reference...

            carClasses.Add(new CarClass(CarClassEnum.F1, new String[] { "FA" }, BrakeType.Carbon, TyreType.Unknown_Race, maxExoticRaceSafeWaterTemp, maxExoticRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.F2, new String[] { "FB" }, BrakeType.Carbon, TyreType.Unknown_Race, maxExoticRaceSafeWaterTemp, maxExoticRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.F3, new String[] { "FC" }, BrakeType.Ceramic, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.F4, new String[] { "F4" }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.FF, new String[] { "F5" }, BrakeType.Iron_Race, TyreType.Road, maxRoadSafeWaterTemp, maxRoadSafeOilTemp));   // formula ford

            // here we assume the old race cars (pre-radial tyres) will race on bias ply tyres
            carClasses.Add(new CarClass(CarClassEnum.VINTAGE_RACE_SLICKS, new String[] { 
                "Vintage F1 B", "Vintage F1 C" }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.VINTAGE_RACE_BIAS_PLY, new String[] { 
                "Vintage F1 A", "Vintage GT", "Historic Touring 2", "Vintage GT3" }, BrakeType.Iron_Race, TyreType.Road, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));

            carClasses.Add(new CarClass(CarClassEnum.STOCK_CAR, new String[] { "Vintage Stockcar" }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.TRANS_AM, new String[] { "Trans-Am" }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));

            carClasses.Add(new CarClass(CarClassEnum.TC3, new String[] { "TC3" }, BrakeType.Carbon, TyreType.Unknown_Race, maxExoticRaceSafeWaterTemp, maxExoticRaceSafeOilTemp)); // modern DTM
            carClasses.Add(new CarClass(CarClassEnum.TC1, new String[] { "TC1" }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp)); // clios

            carClasses.Add(new CarClass(CarClassEnum.ROAD, new String[] { "Road B", "Road C1", "Road C2", "Road D" }, 
                BrakeType.Iron_Road, TyreType.Road, maxRoadSafeWaterTemp, maxRoadSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.ROAD_SUPERCAR, new String[] { "Road A" }, BrakeType.Ceramic, TyreType.Road, maxRoadSafeWaterTemp, maxRoadSafeOilTemp));

            
            List<CornerData.EnumWithThresholds> roadTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            roadTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdRoadTyreTemp));
            roadTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdRoadTyreTemp, maxWarmRoadTyreTemp));
            roadTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmRoadTyreTemp, maxHotRoadTyreTemp));
            roadTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotRoadTyreTemp, 10000));
            tyreTempThresholds.Add(TyreType.Road, roadTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> unknownRaceTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            unknownRaceTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdUnknownRaceTyreTemp));
            unknownRaceTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdUnknownRaceTyreTemp, maxWarmUnknownRaceTyreTemp));
            unknownRaceTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmUnknownRaceTyreTemp, maxHotUnknownRaceTyreTemp));
            unknownRaceTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotUnknownRaceTyreTemp, 10000));
            tyreTempThresholds.Add(TyreType.Unknown_Race, unknownRaceTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> dtmOptionTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            dtmOptionTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdDtmOptionTyreTemp));
            dtmOptionTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdDtmOptionTyreTemp, maxWarmDtmOptionTyreTemp));
            dtmOptionTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmDtmOptionTyreTemp, maxHotDtmOptionTyreTemp));
            dtmOptionTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotDtmOptionTyreTemp, 10000));
            tyreTempThresholds.Add(TyreType.DTM_Option, dtmOptionTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> dtmPrimeTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            dtmPrimeTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdDtmPrimeTyreTemp));
            dtmPrimeTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdDtmPrimeTyreTemp, maxWarmDtmPrimeTyreTemp));
            dtmPrimeTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmDtmPrimeTyreTemp, maxHotDtmPrimeTyreTemp));
            dtmPrimeTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotDtmPrimeTyreTemp, 10000));
            tyreTempThresholds.Add(TyreType.DTM_Prime, dtmPrimeTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> ironRoadBrakeTempsThresholds = new List<CornerData.EnumWithThresholds>();
            ironRoadBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdIronRoadBrakeTemp));
            ironRoadBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdIronRoadBrakeTemp, maxWarmIronRoadBrakeTemp));
            ironRoadBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmIronRoadBrakeTemp, maxHotIronRoadBrakeTemp));
            ironRoadBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotIronRoadBrakeTemp, 10000));
            brakeTempThresholds.Add(BrakeType.Iron_Road, ironRoadBrakeTempsThresholds);

            List<CornerData.EnumWithThresholds> ironRaceBrakeTempsThresholds = new List<CornerData.EnumWithThresholds>();
            ironRaceBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdIronRaceBrakeTemp));
            ironRaceBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdIronRaceBrakeTemp, maxWarmIronRaceBrakeTemp));
            ironRaceBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmIronRaceBrakeTemp, maxHotIronRaceBrakeTemp));
            ironRaceBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotIronRaceBrakeTemp, 10000));
            brakeTempThresholds.Add(BrakeType.Iron_Race, ironRaceBrakeTempsThresholds);

            List<CornerData.EnumWithThresholds> ceramicBrakeTempsThresholds = new List<CornerData.EnumWithThresholds>();
            ceramicBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdCeramicBrakeTemp));
            ceramicBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdCeramicBrakeTemp, maxWarmCeramicBrakeTemp));
            ceramicBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmCeramicBrakeTemp, maxHotCeramicBrakeTemp));
            ceramicBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotCeramicBrakeTemp, 10000));
            brakeTempThresholds.Add(BrakeType.Ceramic, ceramicBrakeTempsThresholds);

            List<CornerData.EnumWithThresholds> carbonBrakeTempsThresholds = new List<CornerData.EnumWithThresholds>();
            carbonBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdCarbonBrakeTemp));
            carbonBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdCarbonBrakeTemp, maxWarmCarbonBrakeTemp));
            carbonBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmCarbonBrakeTemp, maxHotCarbonBrakeTemp));
            carbonBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotCarbonBrakeTemp, 10000));
            brakeTempThresholds.Add(BrakeType.Carbon, carbonBrakeTempsThresholds);
        }

        public static CarClass getCarClass(String carClassName)
        {
            if (carClassName != null)
            {
                foreach (CarClass carClass in carClasses)
                {
                    if (carClass.classNames.Contains(carClassName))
                    {
                        return carClass;
                    }
                }
            }
            return carClasses[0];
        }

        public static List<CornerData.EnumWithThresholds> getBrakeTempThresholds(String carClassName, String carName)
        {
            if (carName!= null && brakeTypesPerCarName.ContainsKey(carName))
            {
                return brakeTempThresholds[brakeTypesPerCarName[carName]];
            }
            return brakeTempThresholds[getCarClass(carClassName).brakeType];
        }

        public static TyreType getDefaultTyreType(String carClassName, String carName)
        {
            if (carName != null && defaultTyreTypesPerCarName.ContainsKey(carName))
            {
                return defaultTyreTypesPerCarName[carName];
            }
            return getCarClass(carClassName).defaultTyreType;
        }
    }
}
