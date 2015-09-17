using CrewChiefV3.RaceRoom;
using CrewChiefV3.RaceRoom.RaceRoomData;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CrewChiefV3.RaceRoom
{
    class R3ESharedMemoryReader : GameDataReader
    {
        private MemoryMappedFile _file;
        private MemoryMappedViewAccessor _view;

        public Boolean Initialise()
        {
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

        public Object ReadGameData()
        {
            lock (this)
            {
                if (_view == null && _file == null)
                {
                    Initialise();
                }
                RaceRoomShared currentState = new RaceRoomShared();
                _view.Read(0, out currentState);
                return currentState;
            }            
        }

        public void Dispose()
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
