﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace CrewChiefV3.PCars
{
    public class StructHelper
    {
        public static pCarsAPIStruct Clone<pCarsAPIStruct>(pCarsAPIStruct pcarsStruct)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, pcarsStruct);
                ms.Position = 0;
                return (pCarsAPIStruct)formatter.Deserialize(ms);
            }
        }

        public static pCarsAPIStruct MergeWithExistingState(pCarsAPIStruct existingState, sTelemetryData udpTelemetryData)
        {
            existingState.hasNewPositionData = false;
            existingState.mGameState = (uint) udpTelemetryData.sGameSessionState & 7;
            existingState.mSessionState = (uint) udpTelemetryData.sGameSessionState >> 4;
            existingState.mRaceState = (uint) udpTelemetryData.sRaceStateFlags & 7;

            // Participant Info
            existingState.mViewedParticipantIndex = udpTelemetryData.sViewedParticipantIndex;
            existingState.mNumParticipants = udpTelemetryData.sNumParticipants;            

            // Unfiltered Input
            existingState.mUnfilteredThrottle = udpTelemetryData.sUnfilteredThrottle / 255;
            existingState.mUnfilteredBrake = udpTelemetryData.sUnfilteredBrake / 255;
            existingState.mUnfilteredSteering = udpTelemetryData.sUnfilteredSteering / 127;
            existingState.mUnfilteredClutch = udpTelemetryData.sUnfilteredClutch / 255;

            existingState.mLapsInEvent = udpTelemetryData.sLapsInEvent;
            existingState.mTrackLength = udpTelemetryData.sTrackLength; 

            // Timing & Scoring
            existingState.mLapInvalidated = (udpTelemetryData.sRaceStateFlags >> 3 & 1) == 1;
            existingState.mSessionFastestLapTime = udpTelemetryData.sBestLapTime;
            existingState.mLastLapTime = udpTelemetryData.sLastLapTime;
            existingState.mCurrentTime = udpTelemetryData.sCurrentTime;
            existingState.mSplitTimeAhead = udpTelemetryData.sSplitTimeAhead;
            existingState.mSplitTimeBehind = udpTelemetryData.sSplitTimeBehind;
            existingState.mSplitTime = udpTelemetryData.sSplitTime;
            existingState.mEventTimeRemaining = udpTelemetryData.sEventTimeRemaining; 
            existingState.mPersonalFastestLapTime = udpTelemetryData.sPersonalFastestLapTime;
            existingState.mWorldFastestLapTime = udpTelemetryData.sWorldFastestLapTime;
            existingState.mCurrentSector1Time = udpTelemetryData.sCurrentSector1Time;
            existingState.mCurrentSector2Time = udpTelemetryData.sCurrentSector2Time; 
            existingState.mCurrentSector3Time = udpTelemetryData.sCurrentSector3Time; 
            existingState.mSessionFastestSector1Time = udpTelemetryData.sFastestSector1Time; 
            existingState.mSessionFastestSector2Time = udpTelemetryData.sFastestSector2Time; 
            existingState.mSessionFastestSector3Time = udpTelemetryData.sFastestSector3Time; 
            existingState.mPersonalFastestSector1Time = udpTelemetryData.sPersonalFastestSector1Time; 
            existingState.mPersonalFastestSector2Time = udpTelemetryData.sPersonalFastestSector2Time; 
            existingState.mPersonalFastestSector3Time = udpTelemetryData.sPersonalFastestSector3Time;
            existingState.mWorldFastestSector1Time = udpTelemetryData.sWorldFastestSector1Time; 
            existingState.mWorldFastestSector2Time = udpTelemetryData.sWorldFastestSector2Time;
            existingState.mWorldFastestSector3Time = udpTelemetryData.sWorldFastestSector3Time;

            // Flags
            existingState.mHighestFlagColour = (uint) udpTelemetryData.sHighestFlag & 7; 
            existingState.mHighestFlagReason = (uint) udpTelemetryData.sHighestFlag >> 3 & 3;

            // Pit Info
            existingState.mPitMode = (uint) udpTelemetryData.sPitModeSchedule & 7;
            existingState.mPitSchedule = (uint) udpTelemetryData.sPitModeSchedule >> 3 & 3;

            // Car State
            existingState.mCarFlags = udpTelemetryData.sCarFlags;
            existingState.mOilTempCelsius = udpTelemetryData.sOilTempCelsius; 
            existingState.mOilPressureKPa = udpTelemetryData.sOilPressureKPa; 
            existingState.mWaterTempCelsius = udpTelemetryData.sWaterTempCelsius; 
            existingState.mWaterPressureKPa = udpTelemetryData.sWaterPressureKpa;
            existingState.mFuelPressureKPa = udpTelemetryData.sFuelPressureKpa;
            existingState.mFuelLevel = udpTelemetryData.sFuelLevel;
            existingState.mFuelCapacity = udpTelemetryData.sFuelCapacity; 
            existingState.mSpeed = udpTelemetryData.sSpeed; 
            existingState.mRPM = udpTelemetryData.sRpm;
            existingState.mMaxRPM = udpTelemetryData.sMaxRpm;
            existingState.mBrake = udpTelemetryData.sBrake / 255;
            existingState.mThrottle = udpTelemetryData.sThrottle / 255;
            existingState.mClutch = udpTelemetryData.sClutch / 255;
            existingState.mSteering = udpTelemetryData.sSteering / 127;
            existingState.mGear = udpTelemetryData.sGearNumGears & 15;
            existingState.mNumGears = udpTelemetryData.sGearNumGears >> 4;
            existingState.mOdometerKM = udpTelemetryData.sOdometerKM;                               
            existingState.mAntiLockActive = (udpTelemetryData.sRaceStateFlags >> 4 & 1) == 1;
            existingState.mBoostActive = (udpTelemetryData.sRaceStateFlags >> 5 & 1) == 1;
            existingState.mBoostAmount = udpTelemetryData.sBoostAmount;

            // Motion & Device Related
            existingState.mOrientation = udpTelemetryData.sOrientation; 
            existingState.mLocalVelocity = udpTelemetryData.sLocalVelocity;
            existingState.mWorldVelocity = udpTelemetryData.sWorldVelocity;
            existingState.mAngularVelocity = udpTelemetryData.sAngularVelocity;
            existingState.mLocalAcceleration = udpTelemetryData.sLocalAcceleration;
            existingState.mWorldAcceleration = udpTelemetryData.sWorldAcceleration;
            existingState.mExtentsCentre = udpTelemetryData.sExtentsCentre; 


            existingState.mTyreFlags = toUIntArray(udpTelemetryData.sTyreFlags); 
            existingState.mTerrain = toUIntArray(udpTelemetryData.sTerrain);
            existingState.mTyreY = udpTelemetryData.sTyreY;
            existingState.mTyreRPS = udpTelemetryData.sTyreRPS;
            existingState.mTyreSlipSpeed = udpTelemetryData.sTyreSlipSpeed;
            existingState.mTyreTemp = toFloatArray(udpTelemetryData.sTyreTemp, 255); 
            existingState.mTyreGrip = toFloatArray(udpTelemetryData.sTyreGrip, 255);
            existingState.mTyreHeightAboveGround = udpTelemetryData.sTyreHeightAboveGround;
            existingState.mTyreLateralStiffness = udpTelemetryData.sTyreLateralStiffness;
            existingState.mTyreWear = toFloatArray(udpTelemetryData.sTyreWear, 255); 
            existingState.mBrakeDamage = toFloatArray(udpTelemetryData.sBrakeDamage, 255);
            existingState.mSuspensionDamage = toFloatArray(udpTelemetryData.sSuspensionDamage, 255);    
            existingState.mBrakeTempCelsius = toFloatArray(udpTelemetryData.sBrakeTempCelsius, 1);
            existingState.mTyreTreadTemp = toFloatArray(udpTelemetryData.sTyreTreadTemp, 1);            
            existingState.mTyreLayerTemp = toFloatArray(udpTelemetryData.sTyreLayerTemp, 1); 
            existingState.mTyreCarcassTemp = toFloatArray(udpTelemetryData.sTyreCarcassTemp, 1); 
            existingState.mTyreRimTemp = toFloatArray(udpTelemetryData.sTyreRimTemp, 1);    
            existingState.mTyreInternalAirTemp = toFloatArray(udpTelemetryData.sTyreInternalAirTemp, 1);
            existingState.mWheelLocalPosition = udpTelemetryData.sWheelLocalPositionY;
            existingState.mRideHeight = udpTelemetryData.sRideHeight;
            existingState.mSuspensionTravel = udpTelemetryData.sSuspensionTravel;
            existingState.mSuspensionVelocity = udpTelemetryData.sSuspensionVelocity;
            existingState.mAirPressure = toFloatArray(udpTelemetryData.sAirPressure, 1);

            existingState.mEngineSpeed = udpTelemetryData.sEngineSpeed;
            existingState.mEngineTorque = udpTelemetryData.sEngineTorque;
            existingState.mEnforcedPitStopLap = (uint) udpTelemetryData.sEnforcedPitStopLap;

            // Car Damage
            existingState.mCrashState = udpTelemetryData.sCrashState;
            existingState.mAeroDamage = udpTelemetryData.sAeroDamage / 255;  
            existingState.mEngineDamage = udpTelemetryData.sEngineDamage / 255; 

            // Weather
            existingState.mAmbientTemperature = udpTelemetryData.sAmbientTemperature;
            existingState.mTrackTemperature = udpTelemetryData.sTrackTemperature;
            existingState.mRainDensity = udpTelemetryData.sRainDensity / 255;         
            existingState.mWindSpeed = udpTelemetryData.sWindSpeed * 2;
            existingState.mWindDirectionX = udpTelemetryData.sWindDirectionX / 127;
            existingState.mWindDirectionY = udpTelemetryData.sWindDirectionY / 127;
            //existingState.mCloudBrightness = udpTelemetryData.sCloudBrightness / 255;

            if (existingState.mParticipantData == null)
            {
                existingState.mParticipantData = new pCarsAPIParticipantStruct[udpTelemetryData.sParticipantInfo.Count()];
            }
            for (int i = 0; i < udpTelemetryData.sParticipantInfo.Count(); i++) 
            {
                sParticipantInfo newPartInfo = udpTelemetryData.sParticipantInfo[i];
                Boolean isActive = (newPartInfo.sRacePosition >> 7) == 1;
                pCarsAPIParticipantStruct existingPartInfo = existingState.mParticipantData[i];
                existingPartInfo.mIsActive = isActive;
                if (isActive)
                {                      
                    existingPartInfo.mCurrentLap = newPartInfo.sCurrentLap;
                    existingPartInfo.mCurrentLapDistance = newPartInfo.sCurrentLapDistance;
                    existingPartInfo.mLapsCompleted = (uint) newPartInfo.sLapsCompleted & 127;
                    // TODO: there's a 'lapInvalidated' flag here but nowhere to put it in the existing struct
                    Boolean lapInvalidated = (newPartInfo.sLapsCompleted >> 7) == 1;
                    existingPartInfo.mRacePosition = (uint) newPartInfo.sRacePosition & 127;
                    existingPartInfo.mCurrentSector = (uint)newPartInfo.sSector & 7;

                    // and now the bit magic for the extra position precision...
                    float[] newWorldPositions = toFloatArray(newPartInfo.sWorldPosition, 1);
                    float xAdjustment = ((float)((uint)newPartInfo.sSector >> 3 & 3)) / 4f;
                    float zAdjustment = ((float)((uint)newPartInfo.sSector >> 5 & 3)) / 4f;
                    newWorldPositions[0] = newWorldPositions[0] + xAdjustment;
                    newWorldPositions[2] = newWorldPositions[2] + zAdjustment;
                    if (!existingState.hasNewPositionData && i != udpTelemetryData.sViewedParticipantIndex && 
                        (existingPartInfo.mWorldPosition == null || (newWorldPositions[0] != existingPartInfo.mWorldPosition[0] || newWorldPositions[2] != existingPartInfo.mWorldPosition[2])))
                    {
                        existingState.hasNewPositionData = true;
                    }
                    existingPartInfo.mWorldPosition = newWorldPositions;

                    // TODO: LastSectorTime is now in the UDP data, but there's no slot for this in the participants struct
                    // existingPartInfo.mLastSectorTime = newPartInfo.sLastSectorTime;
                }
                else
                {
                    existingPartInfo.mWorldPosition = new float[] { 0, 0, 0 };
                }
                existingState.mParticipantData[i] = existingPartInfo;
            }

            // TODO: buttons
            return existingState;
        }

        public static pCarsAPIStruct MergeWithExistingState(pCarsAPIStruct existingState, sParticipantInfoStringsAdditional udpAdditionalStrings)
        {
            if (existingState.mParticipantData == null)
            {
                existingState.mParticipantData = new pCarsAPIParticipantStruct[udpAdditionalStrings.sName.Count()];
            }
            for (int i = 0; i < udpAdditionalStrings.sName.Count(); i++)
            {
                String name = getNameFromBytes(udpAdditionalStrings.sName[i].nameByteArray);
                // TODO: will the mIsActive flag always have been set by the time we reach here?
                existingState.mParticipantData[i + udpAdditionalStrings.sOffset].mIsActive = name != null && name.Length > 0;
                existingState.mParticipantData[i + udpAdditionalStrings.sOffset].mName = name;
            }
            return existingState;
        }

        public static pCarsAPIStruct MergeWithExistingState(pCarsAPIStruct existingState, sParticipantInfoStrings udpParticipantStrings)
        {
            existingState.mCarClassName = udpParticipantStrings.sCarClassName;
            existingState.mCarName = udpParticipantStrings.sCarName;
            existingState.mTrackLocation = udpParticipantStrings.sTrackLocation;
            existingState.mTrackVariation = udpParticipantStrings.sTrackVariation;
            for (int i = 0; i < udpParticipantStrings.sName.Count(); i++)
            {
                String name = getNameFromBytes(udpParticipantStrings.sName[i].nameByteArray);
                existingState.mParticipantData[i].mIsActive = name != null && name.Length > 0;
                existingState.mParticipantData[i].mName = name;
            }
            return existingState;
        }

        private static String getNameFromBytes(byte[] name)
        {
            return Encoding.UTF8.GetString(name).TrimEnd('\0').Trim();
        } 

        private static float[] toFloatArray(int[] intArray, int factor)
        {
            List<float> l = new List<float>();
            foreach (int i in intArray)
            {
                l.Add(((float)i) / factor);
            }
            return l.ToArray();
        }

        private static float[] toFloatArray(byte[] byteArray, int factor)
        {
            List<float> l = new List<float>();
            foreach (byte i in byteArray)
            {
                l.Add(((float)i) / factor);
            }
            return l.ToArray();
        }

        private static float[] toFloatArray(short[] shortArray, int factor)
        {
            List<float> l = new List<float>();
            foreach (short i in shortArray)
            {
                l.Add(((float)i) / factor);
            }
            return l.ToArray();
        }

        private static float[] toFloatArray(ushort[] ushortArray, int factor)
        {
            List<float> l = new List<float>();
            foreach (ushort i in ushortArray)
            {
                l.Add(((float)i) / factor);
            }
            return l.ToArray();
        }

        private static uint[] toUIntArray(byte[] byteArray)
        {
            List<uint> l = new List<uint>();
            foreach (byte i in byteArray)
            {
                l.Add((byte)i);
            }
            return l.ToArray();
        }
    }

    [Serializable]
    public struct pCarsAPIParticipantStruct
    {
        [MarshalAs(UnmanagedType.I1)]
        public bool mIsActive;

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = (int)eAPIStructLengths.STRING_LENGTH_MAX)]
        public string mName;                                    // [ string ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eVector.VEC_MAX)]
        public float[] mWorldPosition;                          // [ UNITS = World Space  X  Y  Z ]

        public float mCurrentLapDistance;                       // [ UNITS = Metres ]   [ RANGE = 0.0f->... ]    [ UNSET = 0.0f ]
        public uint mRacePosition;                              // [ RANGE = 1->... ]   [ UNSET = 0 ]
        public uint mLapsCompleted;                             // [ RANGE = 0->... ]   [ UNSET = 0 ]
        public uint mCurrentLap;                                // [ RANGE = 0->... ]   [ UNSET = 0 ]
        public uint mCurrentSector;                             // [ enum (Type#4) Current Sector ]
    }

    [Serializable]
    public struct pCarsAPIStruct
    {
        //SMS supplied data structure
        // Version Number
        public uint mVersion;                           // [ RANGE = 0->... ]
        public uint mBuildVersion;                      // [ RANGE = 0->... ]   [ UNSET = 0 ]

        // Session type
        public uint mGameState;                         // [ enum (Type#1) Game state ]
        public uint mSessionState;                      // [ enum (Type#2) Session state ]
        public uint mRaceState;                         // [ enum (Type#3) Race State ]

        // Participant Info
        public int mViewedParticipantIndex;                      // [ RANGE = 0->STORED_PARTICIPANTS_MAX ]   [ UNSET = -1 ]
        public int mNumParticipants;                             // [ RANGE = 0->STORED_PARTICIPANTS_MAX ]   [ UNSET = -1 ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)eAPIStructLengths.NUM_PARTICIPANTS)]
        public pCarsAPIParticipantStruct[] mParticipantData;

        // Unfiltered Input
        public float mUnfilteredThrottle;                       // [ RANGE = 0.0f->1.0f ]
        public float mUnfilteredBrake;                          // [ RANGE = 0.0f->1.0f ]
        public float mUnfilteredSteering;                       // [ RANGE = -1.0f->1.0f ]
        public float mUnfilteredClutch;                         // [ RANGE = 0.0f->1.0f ]

        // Vehicle & Track information
        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = (int)eAPIStructLengths.STRING_LENGTH_MAX)]
        public string mCarName;                                 // [ string ]

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = (int)eAPIStructLengths.STRING_LENGTH_MAX)]
        public string mCarClassName;                            // [ string ]

        public uint mLapsInEvent;                               // [ RANGE = 0->... ]   [ UNSET = 0 ]

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = (int)eAPIStructLengths.STRING_LENGTH_MAX)]
        public string mTrackLocation;                           // [ string ]

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = (int)eAPIStructLengths.STRING_LENGTH_MAX)]
        public string mTrackVariation;                          // [ string ]

        public float mTrackLength;                              // [ UNITS = Metres ]   [ RANGE = 0.0f->... ]    [ UNSET = 0.0f ]

        // Timing & Scoring

        // NOTE: 
        // The mSessionFastest... times are only for the player. The overall session fastest time is NOT in the block. Anywhere...
        // The mPersonalFastest... times are often -1. Perhaps they're the player's hotlap / offline practice records for this track.
        //
        public bool mLapInvalidated;                            // [ UNITS = boolean ]   [ RANGE = false->true ]   [ UNSET = false ]
        public float mSessionFastestLapTime;                              // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mLastLapTime;                              // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mCurrentTime;                              // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mSplitTimeAhead;                            // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mSplitTimeBehind;                           // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mSplitTime;                                // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mEventTimeRemaining;                       // [ UNITS = milli-seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mPersonalFastestLapTime;                    // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mWorldFastestLapTime;                       // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mCurrentSector1Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mCurrentSector2Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mCurrentSector3Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mSessionFastestSector1Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mSessionFastestSector2Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mSessionFastestSector3Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mPersonalFastestSector1Time;                // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mPersonalFastestSector2Time;                // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mPersonalFastestSector3Time;                // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mWorldFastestSector1Time;                   // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mWorldFastestSector2Time;                   // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mWorldFastestSector3Time;                   // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]

        // Flags
        public uint mHighestFlagColour;                 // [ enum (Type#5) Flag Colour ]
        public uint mHighestFlagReason;                 // [ enum (Type#6) Flag Reason ]

        // Pit Info
        public uint mPitMode;                           // [ enum (Type#7) Pit Mode ]
        public uint mPitSchedule;                       // [ enum (Type#8) Pit Stop Schedule ]

        // Car State
        public uint mCarFlags;                          // [ enum (Type#6) Car Flags ]
        public float mOilTempCelsius;                           // [ UNITS = Celsius ]   [ UNSET = 0.0f ]
        public float mOilPressureKPa;                           // [ UNITS = Kilopascal ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mWaterTempCelsius;                         // [ UNITS = Celsius ]   [ UNSET = 0.0f ]
        public float mWaterPressureKPa;                         // [ UNITS = Kilopascal ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mFuelPressureKPa;                          // [ UNITS = Kilopascal ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mFuelLevel;                                // [ RANGE = 0.0f->1.0f ]
        public float mFuelCapacity;                             // [ UNITS = Liters ]   [ RANGE = 0.0f->1.0f ]   [ UNSET = 0.0f ]
        public float mSpeed;                                    // [ UNITS = Metres per-second ]   [ RANGE = 0.0f->... ]
        public float mRPM;                                      // [ UNITS = Revolutions per minute ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mMaxRPM;                                   // [ UNITS = Revolutions per minute ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mBrake;                                    // [ RANGE = 0.0f->1.0f ]
        public float mThrottle;                                 // [ RANGE = 0.0f->1.0f ]
        public float mClutch;                                   // [ RANGE = 0.0f->1.0f ]
        public float mSteering;                                 // [ RANGE = -1.0f->1.0f ]
        public int mGear;                                       // [ RANGE = -1 (Reverse)  0 (Neutral)  1 (Gear 1)  2 (Gear 2)  etc... ]   [ UNSET = 0 (Neutral) ]
        public int mNumGears;                                   // [ RANGE = 0->... ]   [ UNSET = -1 ]
        public float mOdometerKM;                               // [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public bool mAntiLockActive;                            // [ UNITS = boolean ]   [ RANGE = false->true ]   [ UNSET = false ]
        public int mLastOpponentCollisionIndex;                 // [ RANGE = 0->STORED_PARTICIPANTS_MAX ]   [ UNSET = -1 ]
        public float mLastOpponentCollisionMagnitude;           // [ RANGE = 0.0f->... ]
        public bool mBoostActive;                               // [ UNITS = boolean ]   [ RANGE = false->true ]   [ UNSET = false ]
        public float mBoostAmount;                              // [ RANGE = 0.0f->100.0f ] 

        // Motion & Device Related
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eVector.VEC_MAX)]
        public float[] mOrientation;                     // [ UNITS = Euler Angles ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eVector.VEC_MAX)]
        public float[] mLocalVelocity;                   // [ UNITS = Metres per-second ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eVector.VEC_MAX)]
        public float[] mWorldVelocity;                   // [ UNITS = Metres per-second ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eVector.VEC_MAX)]
        public float[] mAngularVelocity;                 // [ UNITS = Radians per-second ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eVector.VEC_MAX)]
        public float[] mLocalAcceleration;               // [ UNITS = Metres per-second ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eVector.VEC_MAX)]
        public float[] mWorldAcceleration;               // [ UNITS = Metres per-second ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eVector.VEC_MAX)]
        public float[] mExtentsCentre;                   // [ UNITS = Local Space  X  Y  Z ]

        // Wheels / Tyres
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public uint[] mTyreFlags;               // [ enum (Type#7) Tyre Flags ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public uint[] mTerrain;                 // [ enum (Type#3) Terrain Materials ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreY;                          // [ UNITS = Local Space  Y ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreRPS;                        // [ UNITS = Revolutions per second ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreSlipSpeed;                  // [ UNITS = Metres per-second ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreTemp;                       // [ UNITS = Celsius ]   [ UNSET = 0.0f ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreGrip;                       // [ RANGE = 0.0f->1.0f ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreHeightAboveGround;          // [ UNITS = Local Space  Y ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreLateralStiffness;           // [ UNITS = Lateral stiffness coefficient used in tyre deformation ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreWear;                       // [ RANGE = 0.0f->1.0f ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mBrakeDamage;                    // [ RANGE = 0.0f->1.0f ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mSuspensionDamage;               // [ RANGE = 0.0f->1.0f ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mBrakeTempCelsius;               // [ RANGE = 0.0f->1.0f ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreTreadTemp;                  // [ UNITS = Kelvin ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreLayerTemp;                  // [ UNITS = Kelvin ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreCarcassTemp;                // [ UNITS = Kelvin ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreRimTemp;                    // [ UNITS = Kelvin ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreInternalAirTemp;            // [ UNITS = Kelvin ]


        // Car Damage
        public uint mCrashState;                        // [ enum (Type#4) Crash Damage State ]
        public float mAeroDamage;                               // [ RANGE = 0.0f->1.0f ]
        public float mEngineDamage;                             // [ RANGE = 0.0f->1.0f ]

        // Weather
        public float mAmbientTemperature;                       // [ UNITS = Celsius ]   [ UNSET = 25.0f ]
        public float mTrackTemperature;                         // [ UNITS = Celsius ]   [ UNSET = 30.0f ]
        public float mRainDensity;                              // [ UNITS = How much rain will fall ]   [ RANGE = 0.0f->1.0f ]
        public float mWindSpeed;                                // [ RANGE = 0.0f->100.0f ]   [ UNSET = 2.0f ]
        public float mWindDirectionX;                           // [ UNITS = Normalised Vector X ]
        public float mWindDirectionY;                           // [ UNITS = Normalised Vector Y ]
        public float mCloudBrightness;                          // [ RANGE = 0.0f->... ]

        // extras from the UDP data
        public float[] mWheelLocalPosition;

        public float[] mRideHeight;

        public float[] mSuspensionTravel;

        public float[] mSuspensionVelocity;

        public float[] mAirPressure;

        public float mEngineSpeed;

        public float mEngineTorque;

        public uint mEnforcedPitStopLap;

        // TODO: front wing and rear wing 

        public Boolean hasNewPositionData;
    }
}