using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CrewChiefV3.PCars
{
    // simple type to hold a name, so we can map to an array of these
    public struct nameString
    {
        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 64)]
        string name;
    }

    public struct sParticipantInfoStrings
    {
        ushort	sBuildVersionNumber;	// 0
        byte	sPacketType;	// 2
        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 64)]
        string	sCarName;	// 3
        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 64)]
        string	sCarClassName;	// 131
        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 64)]
        string	sTrackLocation;	// 195        
        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 64)]
        string	sTrackVariation;	// 259
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)]
        nameString[]	sName;	// 323
    // 1347
    };

    public struct sParticipantInfoStringsAdditional
    {
        ushort	sBuildVersionNumber;	// 0
        byte	sPacketType;	// 2
        byte	sOffset;	// 3
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)]
        nameString[] sName;	// 4
        // 1028
    };
    public struct sTelemetryData
    {
        ushort	sBuildVersionNumber;	// 0
        byte	sPacketType;	// 2

        // Game states
        byte	sGameSessionState;	// 3

        // Participant info
        sbyte	sViewedParticipantIndex;	// 4
        sbyte	sNumParticipants;	// 5

        // Unfiltered input
        byte	sUnfilteredThrottle;	// 6
        byte	sUnfilteredBrake;	// 7
        sbyte	sUnfilteredSteering;	// 8
        byte	sUnfilteredClutch;	// 9
        byte	sRaceStateFlags;	// 10

        // Event information
        byte	sLapsInEvent;	// 11

        // Timings
        Single	sBestLapTime;	// 12
        Single	sLastLapTime;	// 16
        Single	sCurrentTime;	// 20
        Single	sSplitTimeAhead;	// 24
        Single	sSplitTimeBehind;	// 28
        Single	sSplitTime;	// 32
        Single	sEventTimeRemaining;	// 36
        Single	sPersonalFastestLapTime;	// 40
        Single	sWorldFastestLapTime;	// 44
        Single	sCurrentSector1Time;	// 48
        Single	sCurrentSector2Time;	// 52
        Single	sCurrentSector3Time;	// 56
        Single	sFastestSector1Time;	// 60
        Single	sFastestSector2Time;	// 64
        Single	sFastestSector3Time;	// 68
        Single	sPersonalFastestSector1Time;	// 72
        Single	sPersonalFastestSector2Time;	// 76
        Single	sPersonalFastestSector3Time;	// 80
        Single	sWorldFastestSector1Time;	// 84
        Single	sWorldFastestSector2Time;	// 88
        Single	sWorldFastestSector3Time;	// 92

        ushort	sTrackLength;	// 96

        // Flags
        byte	sHighestFlag;	// 98

        // Pit info
        byte	sPitModeSchedule;	// 99

        // Car state
        short	sOilTempCelsius;	// 100
        ushort	sOilPressureKPa;	// 102
        short	sWaterTempCelsius;	// 104
        ushort	sWaterPressureKpa;	// 106
        ushort	sFuelPressureKpa;	// 108
        byte	sCarFlags;	// 110
        byte	sFuelCapacity;	// 111
        byte	sBrake;	// 112
        byte	sThrottle;	// 113
        byte	sClutch;	// 114
        sbyte	sSteering;	// 115
        Single	sFuelLevel;	// 116
        Single	sSpeed;	// 120
        ushort	sRpm;	// 124
        ushort	sMaxRpm;	// 126
        byte	sGearNumGears;	// 128
        sbyte	sLastOpponentCollisionIndex;	// 129

        byte	sLastOpponentCollisionMagnitude;	// 130
        byte	sBoostAmount;	// 131

        Single	sOdometerKM;	// 132
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        Single[]	sOrientation;	// 136
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        Single[]	sLocalVelocity;	// 148
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        Single[] sWorldVelocity;	// 160
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        Single[] sAngularVelocity;	// 172
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        Single[] sLocalAcceleration;	// 184
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        Single[] sWorldAcceleration;	// 196
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        Single[] sExtentsCentre;	// 208

        // Wheels / Tyres
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        byte[] sTyreFlags;	// 220
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        byte[] sTerrain;	// 224
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        Single[] sTyreY;	// 228
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        Single[] sTyreRPS;	// 244
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        Single[] sTyreSlipSpeed;	// 260
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        byte[] sTyreTemp;	// 276
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        byte[] sTyreGrip;	// 280
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        Single[] sTyreHeightAboveGround;	// 284
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        Single[] sTyreLateralStiffness;	// 300
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        byte[] sTyreWear;	// 316
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        byte[] sBrakeDamage;	// 320
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        byte[] sSuspensionDamage;	// 324
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        short[] sBrakeTempCelsius;	// 328
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        ushort[] sTyreTreadTemp;	// 336
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        ushort[] sTyreLayerTemp;	// 344
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        ushort[] sTyreCarcassTemp;	// 352
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        ushort[] sTyreRimTemp;	// 360
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        ushort[] sTyreInternalAirTemp;	// 368


        // Car damage
        byte	sCrashState;	// 376
        byte	sAeroDamage;	// 377
        byte	sEngineDamage;	// 378

        // Weather
        sbyte	sAmbientTemperature;	// 379
        sbyte	sTrackTemperature;	// 380
        byte	sRainDensity;	// 381
        sbyte	sWindSpeed;	// 382
        sbyte	sWindDirectionX;	// 383
        sbyte	sWindDirectionY;	// 384
        byte	sCloudBrightness;	// 385

        // Buttons

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 46)]
        sParticipantInfo[] sParticipantInfo;	// 386
        // 46*12=552
        ushort	sJoyPad;	// 938
        byte	sDPad;	// 940
    }
    struct sParticipantInfo
    {
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        short[]	sWorldPosition;	// 0
        ushort	sCurrentLapDistance;	// 6
        byte	sRacePosition;	// 8
        byte	sLapsCompleted;	// 9
        byte	sCurrentLap;	// 10
        byte	sSector;	// 11
        // 12
        };

}
