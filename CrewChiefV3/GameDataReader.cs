using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV3
{
    interface GameDataReader
    {
        Boolean Initialise();

        Object ReadGameData();

        void Dispose();
    }
}
