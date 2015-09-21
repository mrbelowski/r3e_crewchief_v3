using CrewChiefV3.RaceRoom;
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
    class R3ESharedMemoryReader : GameDataReader
    {
        private MemoryMappedFile _file;
        private MemoryMappedViewAccessor _view;
        private List<RaceRoomShared> dataToDump = null;
        private RaceRoomShared[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;

        public override Object ReadGameDataFromFile(String filename)
        {
            if (dataReadFromFile == null)
            {
                dataReadFromFileIndex = 0;
                dataReadFromFile = DeSerializeObject<RaceRoomShared[]>(filename);
            }
            if (dataReadFromFile.Length > dataReadFromFileIndex)
            {
                RaceRoomShared data = dataReadFromFile[dataReadFromFileIndex];
                dataReadFromFileIndex++;
                return data;
            }
            else
            {
                return null;
            }
        }

        public override void DumpRawGameData() 
        {
            if (dumpToFile && dataToDump != null && dataToDump.Count > 0 && filenameToDump!= null)
            {
                SerializeObject(dataToDump.ToArray<RaceRoomShared>(), filenameToDump);
            }
        }
        
        protected override Boolean InitialiseInternal()
        {
            if (dumpToFile)
            {
                dataToDump = new List<RaceRoomShared>();
            }
            lock (this)
            {
                try
                {
                    _file = MemoryMappedFile.OpenExisting(RaceRoomConstant.SharedMemoryName);
                    _view = _file.CreateViewAccessor(0, Marshal.SizeOf(typeof(RaceRoomShared)));
                    return true;
                }
                catch (FileNotFoundException)
                {
                    return false;
                }
            }            
        }

        public override Object ReadGameData()
        {
            lock (this)
            {
                if (_view == null && _file == null)
                {
                    InitialiseInternal();
                }
                RaceRoomShared currentState = new RaceRoomShared();
                _view.Read(0, out currentState);
                if (dumpToFile && dataToDump != null)
                {
                    dataToDump.Add(currentState);
                }
                return currentState;
            }            
        }

        public override void Dispose()
        {
            if (_view != null)
            {
                _view.Dispose();
                _view = null;
            }
            if (_file != null)
            {
                _file.Dispose();
                _file = null;
            }
        }
    }
}
