using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CrewChiefV3
{
    class NameValidator
    {
        private static String[] dregs = new String[] { "BigSilverHotdog" };

        public static Boolean validateName(String name)
        {
            if (dregs.Contains(name))
            {
                Application.Exit();
            }
            return true;
        }
    }
}
