using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV3.PCars
{
    public class PCarsUDPreader : GameDataReader
    {
        private MemoryMappedFile memoryMappedFile;
        private GCHandle handle;
        private int sharedmemorysize;
        private Boolean initialised = false;
        private List<PCarsStructWrapper> dataToDump;
        private PCarsStructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;

        private String dumpFile = "c:/projects/udp.bin";

        public class PCarsStructWrapper
        {
            public long ticksWhenRead;
            public pCarsAPIStruct data;
        }

        public override void DumpRawGameData()
        {
            if (dumpToFile && dataToDump != null && dataToDump.Count > 0 && filenameToDump != null)
            {
                SerializeObject(dataToDump.ToArray<PCarsStructWrapper>(), filenameToDump);
            }
        }

        public override Object ReadGameDataFromFile(String filename)
        {
            if (dataReadFromFile == null)
            {
                dataReadFromFileIndex = 0;
                dataReadFromFile = DeSerializeObject<PCarsStructWrapper[]>(dataFilesPath + filename);
            }
            if (dataReadFromFile.Length > dataReadFromFileIndex)
            {
                PCarsStructWrapper structWrapperData = dataReadFromFile[dataReadFromFileIndex];
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
                dataToDump = new List<PCarsStructWrapper>();
            }
            initialised = true;
            return true;
        }

        public override Object ReadGameData(Boolean allowRecording)
        {
            lock (this)
            {
                pCarsAPIStruct _pcarsapistruct = new pCarsAPIStruct();
                if (!initialised)
                {
                    if (!InitialiseInternal())
                    {
                        throw new GameDataReadException("Failed to initialise shared memory");
                    }
                }
                try
                {
                    byte[] data = File.ReadAllBytes(dumpFile);
                    int offset = 0;
                    while (offset < data.Count())
                    {
                        offset = readFromOffset(offset, data);
                    }
                    PCarsStructWrapper structWrapper = new PCarsStructWrapper();
                    structWrapper.ticksWhenRead = DateTime.Now.Ticks;
                    structWrapper.data = _pcarsapistruct;
                    if (allowRecording && dumpToFile && dataToDump != null && _pcarsapistruct.mTrackLocation != null &&
                        _pcarsapistruct.mTrackLocation.Length > 0)
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

        private int readFromOffset(int offset, byte[] rawData)
        {
            // the first 2 bytes are the version - discard it for now
            int frameType = rawData[offset + 2];
            int frameLength = 0;
            if (frameType == 0)
            {
                frameLength = 1347;
                handle = GCHandle.Alloc(rawData.Skip(offset).Take(frameLength).ToArray(), GCHandleType.Pinned);
                sTelemetryData telem = (sTelemetryData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(sTelemetryData));
                //Console.WriteLine(_pcarsapistruct.mSpeed);
                handle.Free();
            }
            else if (frameType == 1)
            {
                frameLength = 1028;
                handle = GCHandle.Alloc(rawData.Skip(offset).Take(frameLength).ToArray(), GCHandleType.Pinned);
                sParticipantInfoStrings strings = (sParticipantInfoStrings)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(sParticipantInfoStrings));
                //Console.WriteLine(_pcarsapistruct.mSpeed);
                handle.Free();
            }
            else if (frameType == 2)
            {
                frameLength = 941;
                handle = GCHandle.Alloc(rawData.Skip(offset).Take(frameLength).ToArray(), GCHandleType.Pinned);
                sParticipantInfoStringsAdditional additional = (sParticipantInfoStringsAdditional)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(sParticipantInfoStringsAdditional));
                //Console.WriteLine(_pcarsapistruct.mSpeed);
                handle.Free();
            }
            return frameLength + offset;
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
