using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV3
{
    class TrackData
    {
        public static List<TrackDefinition> pCarsTracks = new List<TrackDefinition>()
        {
            new TrackDefinition("Autodromo Nazionale Monza:Grand Prix", 5782.521f, new float[] {22.20312f, -437.1672f}, new float[] {63.60915f, -1.117797f}),
            new TrackDefinition("Autodromo Nazionale Monza:Short", 2422.831f, new float[] {22.20312f, -437.1672f}, new float[] {63.60915f, -1.117797f}),
            new TrackDefinition("Azure Circuit:Grand Prix", 3325.762f, new float[] {-203.8109f, 613.3162f}, new float[] {105.2057f, 525.9147f}),
            new TrackDefinition("Bathurst:", 6238.047f, new float[] {80.84997f, 7.21405f}, new float[] {-368.7227f, 12.93535f}),
            new TrackDefinition("Brands Hatch:Indy", 1924.475f, new float[] {-329.1295f, 165.8752f}, new float[] {-36.68332f, 355.611f}),
            new TrackDefinition("Brands Hatch:Grand Prix", 3890.407f, new float[] {-329.1295f, 165.8752f}, new float[] {-36.68332f, 355.611f}),
            new TrackDefinition("Brno:", 5377.732f, new float[] {-194.1228f, -11.41852f}, new float[] {139.6739f, 0.06169825f}),
            new TrackDefinition("Cadwell:Club Circuit", 2346.271f),
            new TrackDefinition("Cadwell:Woodland", 1335.426f, new float[] {45.92422f, 72.04858f}, new float[] {-10.31487f, -40.43255f}),
            new TrackDefinition("Cadwell:Grand Prix", 3453.817f, new float[] {45.92422f, 72.04858f}, new float[] {-10.31487f, -40.43255f}),
            new TrackDefinition("Circuit de Barcelona-Catalunya:Club", 1691.681f),
            new TrackDefinition("Circuit de Barcelona-Catalunya:Grand Prix", 4630.515f, new float[] {622.7108f, -137.3975f}, new float[] {26.52858f, -167.9301f}),
            new TrackDefinition("Circuit de Barcelona-Catalunya:National", 2948.298f, new float[] {622.7108f, -137.3975f}, new float[] {26.52858f, -167.9301f}),
            new TrackDefinition("Circuit de Spa-Francorchamps:", 6997.816f, new float[] {-685.1871f, 1238.607f}, new float[] {-952.3125f, 1656.81f}),
            new TrackDefinition("Le Mans:Circuit des 24 Heures du Mans", 13595.01f, new float[] {-737.9395f, 1107.367f}, new float[] {-721.3452f, 1582.873f}),
            new TrackDefinition("Donington Park:Grand Prix", 3982.949f, new float[] {200.8843f, 144.8465f}, new float[] {486.4654f, 119.9713f}),
            new TrackDefinition("Donington Park:National", 3175.802f, new float[] {200.8843f, 144.8465f}, new float[] {486.4654f, 119.9713f}),
            new TrackDefinition("Hockenheim:Grand Prix", 4563.47f, new float[] {-483.1076f, -428.47f}, new float[] {-704.3397f, 11.15407f}),
            new TrackDefinition("Hockenheim:Short", 2593.466f, new float[] {-483.1076f, -428.47f}, new float[] {-704.3397f, 11.15407f}),
            new TrackDefinition("Hockenheim:National", 3685.798f, new float[] {-483.1076f, -428.47f}, new float[] {-704.3397f, 11.15407f}),
            new TrackDefinition("Imola:Grand Prix", 4847.88f, new float[] {311.259f, 420.3269f}, new float[] {-272.6198f, 418.3795f}),
            new TrackDefinition("Le Mans:Le Circuit Bugatti", 4149.839f, new float[] {-737.9395f, 1107.367f}, new float[] {-721.3452f, 1582.873f}),
            new TrackDefinition("Mazda Raceway:Laguna Seca", 3593.582f, new float[] {-70.22401f, 432.3777f}, new float[] {-279.2681f, 228.165f}),
            new TrackDefinition("Nordschleife:Full", 20735.4f,  new float[] {599.293f, 606.7135f}, new float[] {391.6694f, 694.4844f}),
            new TrackDefinition("Nürburgring:Grand Prix", 5122.845f, new float[] {443.6332f, 527.8024f}, new float[] {66.84711f, 96.7378f}),
            new TrackDefinition("Nürburgring:MuellenBach", 1488.941f),
            new TrackDefinition("Nürburgring:Sprint", 3603.18f, new float[] {443.6332f, 527.8024f}, new float[] {66.84711f, 96.7378f}),
            new TrackDefinition("Nürburgring:Sprint Short", 3083.551f, new float[] {443.6332f, 527.8024f}, new float[] {66.84711f, 96.7378f}),
            new TrackDefinition("Oschersleben:C Circuit", 1061.989f),
            new TrackDefinition("Oschersleben:Grand Prix", 3656.855f, new float[] {-350.7033f, 31.39084f}, new float[] {239.3137f, 91.73861f}),
            new TrackDefinition("Oschersleben:National", 2417.65f, new float[] {-350.7033f, 31.39084f}, new float[] {239.3137f, 91.73861f}),
            new TrackDefinition("Oulton Park:Fosters", 2649.91f, new float[] {46.9972f, 80.40176f}, new float[] {114.8132f, -165.5994f}),
            new TrackDefinition("Oulton Park:International", 4302.739f, new float[] {46.9972f, 80.40176f}, new float[] {114.8132f, -165.5994f}),
            new TrackDefinition("Oulton Park:Island", 3547.586f, new float[] {46.9972f, 80.40176f}, new float[] {114.8132f, -165.5994f}),
            new TrackDefinition("Road America:", 6482.489f, new float[] {430.8689f, 245.7329f}, new float[] {451.5659f, -330.7411f}),
            new TrackDefinition("Sakitto:Grand Prix", 5383.884f, new float[] {576.6671f, -142.1608f}, new float[] {607.291f, -646.9218f}),
            new TrackDefinition("Sakitto:International", 2845.161f, new float[] {-265.1671f, 472.4344f}, new float[] {-154.9505f, 278.1627f}),
            new TrackDefinition("Sakitto:National", 3100.48f, new float[] {-265.1671f, 472.4344f}, new float[] {-154.9505f, 278.1627f}),
            new TrackDefinition("Sakitto:Sprint", 3539.384f, new float[] {576.6671f -142.1608f}, new float[] {607.291f, -646.9218f}),
            new TrackDefinition("Silverstone:Grand Prix", 5809.965f, new float[] {-504.739f, -1274.686f}, new float[] {-273.1427f, -861.1436f}),
            new TrackDefinition("Silverstone:International", 2978.349f, new float[] {-504.739f, -1274.686f}, new float[] {-273.1427f, -861.1436f}),
            new TrackDefinition("Silverstone:National", 2620.891f, new float[] {-323.1119f, -115.6939f},new float[] {157.4515f, 0.4208831f}),
            new TrackDefinition("Silverstone:Stowe", 1712.277f, new float[] {-75.90499f, -1396.183f}, new float[] {-0.5095776f, -1096.397f}),
            new TrackDefinition("Snetterton:100 Circuit", 1544.868f),
            new TrackDefinition("Snetterton:200 Circuit", 3170.307f, new float[] {228.4838f, -25.23679f}, new float[] {-44.5122f, -55.82156f}),
            new TrackDefinition("Snetterton:300 Circuit", 4747.875f, new float[] {228.4838f, -25.23679f}, new float[] {-44.5122f, -55.82156f}),
            new TrackDefinition("Watkins Glen International:Grand Prix", 5112.122f, new float[] {589.6273f, -928.2814f}, new float[] {542.0042f, -1410.464f}),
            new TrackDefinition("Watkins Glen International:Short Circuit", 3676.561f, new float[] {589.6273f, -928.2814f}, new float[] {542.0042f, -1410.464f}),
            new TrackDefinition("Zhuhai:International Circuit", 4293.098f, new float[] {-193.7068f, 123.679f}, new float[] {64.56277f, -71.51254f}),
            new TrackDefinition("Zolder:Grand Prix", 4146.733f, new float[] {112.9851f, 120.8965f}, new float[] {682.2009f, 179.8147f})
            // TODO: dubai, willow springs, sonoma
        };

        public static TrackDefinition getTrackDefinition(String trackName, float trackLength, GameEnum game)
        {
            if (game == GameEnum.PCARS_32BIT || game == GameEnum.PCARS_64BIT)
            {
                foreach (TrackDefinition def in pCarsTracks)
                {
                    if (def.name == trackName || def.trackLength == trackLength)
                    {
                        return def;
                    }
                }
            }
            return null;
        }

        public Boolean isAtPitEntry(TrackDefinition trackDefinition, float x, float y) 
        {
            return trackDefinition.hasPitLane && Math.Abs(trackDefinition.pitEntryPoint[0] - x) < trackDefinition.pitEntryExitPointsDiameter && 
                Math.Abs(trackDefinition.pitEntryPoint[1] - y) < trackDefinition.pitEntryExitPointsDiameter;
        }

        public Boolean isAtPitExit(TrackDefinition trackDefinition, float x, float y)
        {
            return trackDefinition.hasPitLane && Math.Abs(trackDefinition.pitExitPoint[0] - x) < trackDefinition.pitEntryExitPointsDiameter &&
                Math.Abs(trackDefinition.pitExitPoint[1] - y) < trackDefinition.pitEntryExitPointsDiameter;
        }
    }

    class TrackDefinition
    {
        public String name;
        public float trackLength;
        public Boolean hasPitLane;
        public float[] pitEntryPoint = new float[] { 0, 0 };
        public float[] pitExitPoint = new float[] { 0, 0 };
        public int pitEntryExitPointsDiameter = 2;   // if we're within 2 metres of the pit entry point, we're entering the pit

        public TrackDefinition(String name, float trackLength, float[] pitEntryPoint, float[] pitExitPoint)
        {
            this.name = name;
            this.trackLength = trackLength;
            this.hasPitLane = true;
            this.pitEntryPoint = pitEntryPoint;
            this.pitExitPoint = pitExitPoint;
        }

        public TrackDefinition(String name, float trackLength)
        {
            this.name = name;
            this.trackLength = trackLength;
            this.hasPitLane = false;
        }
    }
}
