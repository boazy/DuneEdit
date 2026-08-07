using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

#pragma warning disable 67

namespace DuneEdit
{
    public class SietchStatus
    {
        [BitField] public bool Vegetation;
        [BitField] public bool UnderAttack;
        [BitField] public bool Infiltrated;
        [BitField] public bool BattleWon;
        [BitField] public bool InventoryVisible;
        [BitField] public bool HasWindtrap;
        [BitField] public bool Prospected;
        [BitField] public bool Undiscovered;
    }

    public class Sietch : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private byte[] data;

        public Sietch(byte[] data)
        {
            this.data = data;
        }

        public byte[] RawData { get { return data; } }

        public byte Unk05 { get { return data[0x05]; } set { data[0x05] = value; } }
        public byte Unk0B { get { return data[0x0B]; } set { data[0x0B] = value; } }
        public byte Unk0C { get { return data[0x0C]; } set { data[0x0C] = value; } }
        public byte Unk0D { get { return data[0x0D]; } set { data[0x0D] = value; } }
        public byte Unk0E { get { return data[0x0E]; } set { data[0x0E] = value; } }
        public byte Unk0F { get { return data[0x0F]; } set { data[0x0F] = value; } }
        public byte Unk11 { get { return data[0x11]; } set { data[0x11] = value; } }
        public byte Unk13 { get { return data[0x13]; } set { data[0x13] = value; } }

        public byte RegionId            { get { return data[0x00]; } set { data[0x00] = value; } }
        public byte SubregionId         { get { return data[0x01]; } set { data[0x01] = value; } }
        public byte DesertAroundSietch  { get { return data[0x02]; } set { data[0x02] = value; } }
        public byte MapPosX             { get { return data[0x03]; } set { data[0x03] = value; } }
        public byte MapPosY             { get { return data[0x04]; } set { data[0x04] = value; } }
        // 0x05 - Unknown
        public byte PosX                { get { return data[0x06]; } set { data[0x06] = value; } }
        public byte PosY                { get { return data[0x07]; } set { data[0x07] = value; } }
        public byte LocationType        { get { return data[0x08]; } set { data[0x08] = value; } }
        public byte PrimaryTroopId      { get { return data[0x09]; } set { data[0x09] = value; } }
        private byte StatusByte         { get { return data[0x0A]; } set { data[0x0A] = value; } }
        // 0x0B-0x0F - Unknown
        public byte SpiceFieldId        { get { return data[0x10]; } set { data[0x10] = value; } } // a.k.a ConnectedArea (?)
        // 0x11 - Unknown
        public byte Spice               { get { return data[0x12]; } set { data[0x12] = value; } }
        // 0x13 - Unknown
        public byte Harvesters          { get { return data[0x14]; } set { data[0x14] = value; } }
        public byte Ornis               { get { return data[0x15]; } set { data[0x15] = value; } }
        public byte Krys                { get { return data[0x16]; } set { data[0x16] = value; } }
        public byte Laserguns           { get { return data[0x17]; } set { data[0x17] = value; } }
        public byte WierdingModules     { get { return data[0x18]; } set { data[0x18] = value; } }
        public byte Atomics             { get { return data[0x19]; } set { data[0x19] = value; } }
        public byte Bulbs               { get { return data[0x1A]; } set { data[0x1A] = value; } }
        public byte Water               { get { return data[0x1B]; } set { data[0x1B] = value; } }

        public string Name { get { return Region + " " + Subregion; } }
        public string Region { get { return Regions.GetRegion(RegionId); } }
        public string Subregion { get { return Regions.GetSubregion(SubregionId); } }

        public string LocationTypeGroup
        {
            get
            {
                if (LocationType <= 0x10) // 0x10 is special and is used by Sihaya-Tuek (Liet Kynes's place)
                    return "Sietch";
                else if (LocationType == 0x20)
                    return "Carthag";
                else if (LocationType == 0x21)
                    return "Village";
                else if (LocationType >= 0x22 && LocationType <= 0x2F)
                    return "Fort";
                else if (LocationType == 0x30)
                    return "Arrakeen";
                else
                    return "Unknown";
            }
        }

        public string LocationTypeTitle
        {
            get
            {
                string title = LocationTypeGroup;
                switch (title)
                {
                    case "Carthag":
                        return "Carthag Palace";
                    case "Arrakeen":
                        return "Arrakeen Palace";
                    default:
                        return title + ":";
                }
            }
        }

        private SietchStatus Status
        {
            get
            {
                return BitFieldDecoder<SietchStatus>.Decode(StatusByte);
            }
            set
            {
                StatusByte = BitFieldEncoder<byte>.Encode(value);
            }
        }

        public bool Vegetation
        {
            get { return Status.Vegetation; }
            set { var st = Status; st.Vegetation = value; Status = st; }
        }

        public bool UnderAttack
        {
            get { return Status.UnderAttack; }
            set { var st = Status; st.UnderAttack = value; Status = st; }
        }

        public bool Infiltrated
        {
            get { return Status.Infiltrated; }
            set { var st = Status; st.Infiltrated = value; Status = st; }
        }
        
        public bool BattleWon
        {
            get { return Status.BattleWon; }
            set { var st = Status; st.BattleWon = value; Status = st; }
        }

        public bool InventoryVisible
        {
            get { return Status.InventoryVisible; }
            set { var st = Status; st.InventoryVisible = value; Status = st; }
        }
        
        public bool HasWindtrap
        {
            get { return Status.HasWindtrap; }
            set { var st = Status; st.HasWindtrap = value; Status = st; }
        }

        public bool Prospected
        {
            get { return Status.Prospected; }
            set { var st = Status; st.Prospected = value; Status = st; }
        }

        public bool Discovered
        {
            get { return !Status.Undiscovered; }
            set { var st = Status; st.Undiscovered = !value; Status = st; }
        }
    }
}
