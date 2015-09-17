﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;



namespace CrewChiefV3.PCars
{

    public enum eTyreFlags
    {
        TYRE_ATTACHED = (1 << 0),
        TYRE_INFLATED = (1 << 1),
        TYRE_IS_ON_GROUND = (1 << 2),
    }

}
