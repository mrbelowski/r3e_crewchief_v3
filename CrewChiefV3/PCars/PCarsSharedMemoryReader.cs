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
    class PCarsSharedMemoryReader : GameDataReader
    {
        private MemoryMappedFile memoryMappedFile;
        private GCHandle handle;
        private int sharedmemorysize;
        private byte[] sharedMemoryReadBuffer;
        private Boolean initialised = false;
        private List<pCarsAPIStruct> dataToDump;
        private pCarsAPIStruct[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;

        public override void DumpRawGameData()
        {
            if (dumpToFile && dataToDump != null && dataToDump.Count > 0 && filenameToDump != null)
            {
                SerializeObject(dataToDump.ToArray<pCarsAPIStruct>(), filenameToDump);
            }
        }

        public override Object ReadGameDataFromFile(String filename)
        {
            if (dataReadFromFile == null)
            {
                dataReadFromFileIndex = 0;
                dataReadFromFile = DeSerializeObject<pCarsAPIStruct[]>(dataFilesPath + filename);
            }
            if (dataReadFromFile.Length > dataReadFromFileIndex)
            {
                pCarsAPIStruct data = dataReadFromFile[dataReadFromFileIndex];
                dataReadFromFileIndex++;
                return data;
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
                dataToDump = new List<pCarsAPIStruct>();
            }
            lock (this)
            {
                if (!initialised)
                {
                    try
                    {
                        memoryMappedFile = MemoryMappedFile.OpenExisting("$pcars$");
                        sharedmemorysize = Marshal.SizeOf(typeof(pCarsAPIStruct));
                        sharedMemoryReadBuffer = new byte[sharedmemorysize];
                        initialised = true;
                        Console.WriteLine("Initialised pcars shared memory");
                    }
                    catch (Exception ex)
                    {
                        initialised = false;
                    }
                }
                return initialised;
            }            
        }

        public override Object ReadGameData()
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
                    using (var sharedMemoryStreamView = memoryMappedFile.CreateViewStream())
                    {
                        BinaryReader _SharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                        sharedMemoryReadBuffer = _SharedMemoryStream.ReadBytes(sharedmemorysize);
                        handle = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);
                        _pcarsapistruct = (pCarsAPIStruct)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(pCarsAPIStruct));
                        //Console.WriteLine(_pcarsapistruct.mSpeed);
                        handle.Free();
                    }
                    if (dumpToFile && dataToDump != null && _pcarsapistruct.mTrackLocation != null &&
                        _pcarsapistruct.mTrackLocation.Length > 0)
                    {
                        dataToDump.Add(_pcarsapistruct);
                    }
                    return _pcarsapistruct;
                }
                catch (Exception ex)
                {
                    throw new GameDataReadException(ex.Message, ex);
                }
            }            
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
