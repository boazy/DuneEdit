using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DuneEdit
{
    public static class SavegameCompression
    {
        public static byte[] Decompress(byte[] data)
        {
            var ms = new MemoryStream();

            for (int i = 0; i < data.Length; i++)
            {
                byte c = data[i];
                if (c == 0xF7 && (i >= 6)) // Skip first 6 bytes - these should be uncompressed?
                {
                    // F7 Repeat sequence
                    byte count = data[i + 1];
                    byte repeat = data[i + 2];
                    for (int j = 0; j < count; j++)
                        ms.WriteByte(repeat);

                    i += 2; // Skip the rest of the sequence
                }
                else
                    ms.WriteByte(c);
            }

            return ms.ToArray();
        }

        private static void WriteCount(MemoryStream ms, byte last, byte count)
        {
            if ((last == 0xF7) || count > 2)
            {
                ms.WriteByte(0xF7);
                ms.WriteByte(count);
                ms.WriteByte(last);
            }
            else
            {
                for (int i = 0; i < count; i++)
                    ms.WriteByte(last);
            }
        }

        public static byte[] Compress(byte[] data)
        {
            var ms = new MemoryStream();

            byte last = 0;
            byte count = 0;
            ms.Write(data, 0, 6); // Write 6-byte header as-is, without compression
            foreach (byte c in data.Skip(6))
            {
                if ((count > 0) && ((last != c) || (count == 0xFF)))
                {
                    WriteCount(ms, last, count);
                    count = 1;
                }
                else
                    count++;

                last = c;
            }

            if (count > 0)
                WriteCount(ms, last, count);

            var compressed = ms.ToArray();

            // Header must contain the size of the entire compressed file minus 2 as a Little Endian UInt16 at offsets 4-5.
            // If data is longer than supported by Dune, an exception will be thrown.
            ushort length = Convert.ToUInt16(compressed.Length - 2);
            compressed[4] = (byte)(length & 0xff);
            compressed[5] = (byte)((length >> 8) & 0xff);

            return compressed;
        }
    }
}
