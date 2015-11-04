﻿using CrewChiefV3.RaceRoom;
using CrewChiefV3.RaceRoom.RaceRoomData;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace CrewChiefV3.RaceRoom
{
    public class R3ESharedMemoryReader : GameDataReader
    {
        private MemoryMappedFile memoryMappedFile;
        private GCHandle handle;
        private int sharedmemorysize;
        private byte[] sharedMemoryReadBuffer;
        private Boolean initialised = false;
        private List<R3EStructWrapper> dataToDump;
        private R3EStructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;

        public class R3EStructWrapper
        {
            public long ticksWhenRead;
            public RaceRoomShared data;
        }

        public override void DumpRawGameData()
        {
            if (dumpToFile && dataToDump != null && dataToDump.Count > 0 && filenameToDump != null)
            {
                foreach (R3EStructWrapper wrapper in dataToDump)
                {
                    wrapper.data.all_drivers_data = getPopulatedDriverDataArray(wrapper.data.all_drivers_data);
                }
                SerializeObject(dataToDump.ToArray<R3EStructWrapper>(), filenameToDump);
            }
        }

        public override Object ReadGameDataFromFile(String filename)
        {
            if (dataReadFromFile == null)
            {
                dataReadFromFileIndex = 0;
                dataReadFromFile = DeSerializeObject<R3EStructWrapper[]>(dataFilesPath + filename);
            }
            if (dataReadFromFile.Length > dataReadFromFileIndex)
            {
                R3EStructWrapper structWrapperData = dataReadFromFile[dataReadFromFileIndex];
                dataReadFromFileIndex++;
                return structWrapperData;
            }
            else
            {
                return null;
            }
        }

        protected override Boolean InitialiseInternal()
        {
            if (dumpToFile)
            {
                dataToDump = new List<R3EStructWrapper>();
            }
            lock (this)
            {
                if (!initialised)
                {
                    try
                    {
                        memoryMappedFile = MemoryMappedFile.OpenExisting(RaceRoomConstant.SharedMemoryName);
                        sharedmemorysize = Marshal.SizeOf(typeof(RaceRoomShared));
                        sharedMemoryReadBuffer = new byte[sharedmemorysize];
                        initialised = true;
                        Console.WriteLine("Initialised raceroom shared memory");
                    }
                    catch (Exception ex)
                    {
                        initialised = false;
                    }
                }
                return initialised;
            }
        }

        public override Object ReadGameData(Boolean forSpotter)
        {
            lock (this)
            {
                RaceRoomShared _raceroomapistruct = new RaceRoomShared();
                if (!initialised)
                {
                    if (!InitialiseInternal())
                    {
                        throw new GameDataReadException("Failed to initialise shared memory");
                    }
                }
                try
                {
                    using (var sharedMemoryStreamView = memoryMappedFile.CreateViewStream())
                    {
                        BinaryReader _SharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                        sharedMemoryReadBuffer = _SharedMemoryStream.ReadBytes(sharedmemorysize);
                        handle = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);
                        _raceroomapistruct = (RaceRoomShared)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(RaceRoomShared));
                        handle.Free();
                    }
                    R3EStructWrapper structWrapper = new R3EStructWrapper();
                    structWrapper.ticksWhenRead = DateTime.Now.Ticks;
                    structWrapper.data = _raceroomapistruct;
                    if (!forSpotter && dumpToFile && dataToDump != null)
                    {
                        dataToDump.Add(structWrapper);
                    }
                    return structWrapper;
                }
                catch (Exception ex)
                {
                    throw new GameDataReadException(ex.Message, ex);
                }
            }
        }

        private DriverData[] getPopulatedDriverDataArray(DriverData[] raw)
        {
            List<DriverData> populated = new List<DriverData>();
            foreach (DriverData rawData in raw)
            {
                if (rawData.place > 0)
                {
                    populated.Add(rawData);
                }
            }
            if (populated.Count == 0)
            {
                populated.Add(raw[0]);
            }
            return populated.ToArray();
        }

        public override void Dispose()
        {
            if (memoryMappedFile != null)
            {
                memoryMappedFile.Dispose();
            }
        }
    }
}
