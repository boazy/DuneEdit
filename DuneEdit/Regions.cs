using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DuneEdit
{
    public static class Regions
    {
        public static string GetRegion(byte id)
        {
            switch (id)
            {
                case 0x01: return "Arrakeen";
                case 0x02: return "Carthag";
                case 0x03: return "Tuono";
                case 0x04: return "Habbanya";
                case 0x05: return "Oxtyn";
                case 0x06: return "Tsympo";
                case 0x07: return "Bledan";
                case 0x08: return "Ergsun";
                case 0x09: return "Haga";
                case 0x0A: return "Cielago";
                case 0x0B: return "Sihaya";
                case 0x0C: return "Celimyn";
            }
            return "Unknown";
        }

        public static string GetSubregion(byte id)
        {
            switch (id)
            {
                case 0x01: return "(Atreides Palace)";
                case 0x02: return "(Harkonnen Palace)";
                case 0x03: return "Tabr";
                case 0x04: return "Timin";
                case 0x05: return "Tuek";
                case 0x06: return "Harg";
                case 0x07: return "Clam";
                case 0x08: return "Tsymyn";
                case 0x09: return "Siet";
                case 0x0A: return "Pyons";
                case 0x0B: return "Pyort";
            }
            return "Unknown";
        }
    }
}
