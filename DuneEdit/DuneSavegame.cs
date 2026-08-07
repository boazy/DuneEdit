using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DuneEdit
{
    static class ArrayExt
    {
        /// <summary>
        /// Get the array slice between the two indexes.
        /// ... Inclusive for start index, exclusive for end index.
        /// </summary>
        public static T[] MakeSlice<T>(this T[] source, int start, int end)
        {
            // Handles negative ends.
            if (end < 0)
            {
                end = source.Length + end;
            }
            int len = end - start;

            // Return new array.
            T[] res = new T[len];
            for (int i = 0; i < len; i++)
            {
                res[i] = source[i + start];
            }
            return res;
        }
    }

    class PossibleLocationOffsets
    {
        private struct Offsets
        {
            public Offsets(int[] offsetArray)
            {
                offsets = offsetArray;
            }

            public int[] offsets;
        }

        public int[] this[byte region, byte subregion]
        {
            get
            {
                if (region == 0xFF && subregion == 0xFF)
                    return terminator;
                else
                    return offsets[region, subregion].offsets;
            }
            set
            {
                if (region == 0xFF && subregion == 0xFF)
                    terminator = value;
                else
                    offsets[region, subregion] = new Offsets(value);
            }
        }

        private int[] terminator;

        private Offsets[,] offsets = new Offsets[0x0D, 0x0C];
    }

    class FinalOffsets
    {
        public int this[byte region, byte subregion]
        {
            get
            {
                if (region == 0xFF && subregion == 0xFF)
                    return terminator;
                else
                    return offsets[region, subregion];
            }
            set
            {
                if (region == 0xFF && subregion == 0xFF)
                    terminator = value;
                else
                    offsets[region, subregion] = value;
            }
        }

        private int terminator;

        private int[,] offsets = new int[0x0D, 0x0C];
    }

    public class DuneSavegame
    {
        public Sietch[,] sietchesMatrix = new Sietch[0x0D, 0x0C];
        public List<Sietch> sietches = new List<Sietch>();

        private Loc[] saveSequences = LocSequences.compressed;   // sequences used to identify the sietches (compressed, i.e. savegame )
        private Loc[] exeSequences = LocSequences.uncompressed;   // sequences used to identify the sietches (uncompressed, i.e. exe)

        private byte[] rawSaveData;
        private int locStart;

        public FileInfo SaveFile { get; set; }

        public DuneSavegame() { }

        private static FinalOffsets FindLocationOffsets(byte[] saveData, Loc[] locSeqs)
        {
            var offsets = new FinalOffsets();
            var possibleOffsets = new PossibleLocationOffsets();
            int[] thisOffsets = { }, nextOffsets = { };
            int l = locSeqs.Length;

            // Find all possible offsets and put them into the sOff Array2D
            foreach (var seq in locSeqs)
            {
                possibleOffsets[seq.region, seq.subregion] = FindSequence(saveData, seq);
            }

            // Test whether the found offset is connected to a sietch by checking whether
            // it's followed by the sequence identifying the next sietch
            l--;
            for (int i = 0; i < l; i++)
            {
                var thisSeq = locSeqs[i];
                var nextSeq = locSeqs[i + 1];
                thisOffsets = possibleOffsets[thisSeq.region, thisSeq.subregion];
                nextOffsets = possibleOffsets[nextSeq.region, nextSeq.subregion];

                // Join two offset arrays and find cases where offsets are contingent.
                var query =
                    from off1 in thisOffsets
                    from off2 in nextOffsets
                    where (off2 - off1 <= 0x20) && (off2 - off1 > 0)
                    select new { off1, off2 };

                // Display joined groups.
                var results = query.FirstOrDefault();

                if (results != null)
                {
                    offsets[thisSeq.region, thisSeq.subregion] = results.off1;
                    offsets[nextSeq.region, nextSeq.subregion] = results.off2;
                }
                // If it's null something went wrong...

                /*
                if (thisSeq.region == 1 && thisSeq.subregion >= 9)
                {
                    System.Windows.MessageBox.Show(String.Format(
                        "{0} {1}: {2}",
                        Regions.GetRegion(thisSeq.region),
                        Regions.GetSubregion(thisSeq.subregion),
                        results.off1));
                }
                */
                
            }

            return offsets;
        }

        private const int SietchSeqSize = 0x1c;

        public void Load(FileInfo saveFile)
        {
            // Load savegame data
            byte[] saveData;
            using (var f = new BinaryReader(saveFile.OpenRead()))
            {
                saveData = f.ReadBytes((int)f.BaseStream.Length);
            }

            // Decompress the savegame if needed
            bool isCompressed = (saveFile.Extension.ToLower() != ".exe");
            if (isCompressed)
                saveData = SavegameCompression.Decompress(saveData);

            // Finding all offsets for the sietches, using exeSequences if not compressed.
            var offsets = FindLocationOffsets(saveData, isCompressed ? saveSequences : exeSequences);

            // Find start offset for the sietches/location part of the decompressed savegame.
            locStart = offsets[saveSequences.First().region, saveSequences.First().subregion];

            // Loading all sietches into a 2d array
            int start = locStart + 0;
            int end = locStart + SietchSeqSize;
            foreach (var loc in saveSequences.Take(saveSequences.Length - 1))
            {
                // Creating a new sietch
                var sietch = new Sietch(saveData.MakeSlice(start, end));
                sietchesMatrix[loc.region, loc.subregion] = sietch;
                sietches.Add(sietch);
                // Setting the offset bounds

                start += SietchSeqSize;
                end += SietchSeqSize;
            }

            SaveFile = saveFile;
            rawSaveData = saveData;
        }

        public void Save()
        {
            SaveAs(SaveFile);
        }

        public void SaveAs(FileInfo saveFile)
        {
            System.Diagnostics.Debug.Assert(rawSaveData != null);

            bool isCompressed = (saveFile.Extension.ToLower() != ".exe");

            // Get sietches order, using exeSequences if not compressed.
            var locSequences = isCompressed ? saveSequences : exeSequences;

            /*
            using (var f = File.OpenWrite(BsaveFile.FullName + ".rawBefore"))
            {
                f.SetLength(0);
                f.Write(rawSaveData, 0, rawSaveData.Length);
            }
            */

            // Update raw data for each sietch
            int start = locStart + 0;
            foreach (var loc in locSequences.Take(locSequences.Length - 1))
            {
                var sietch = sietchesMatrix[loc.region, loc.subregion];
                sietch.RawData.CopyTo(rawSaveData, start);
                start += SietchSeqSize;
            }

            /*
            using (var f = File.OpenWrite(saveFile.FullName + ".rawAfter"))
            {
                f.SetLength(0);
                f.Write(rawSaveData, 0, rawSaveData.Length);
            }
            */

            byte[] saveData = isCompressed ? SavegameCompression.Compress(rawSaveData) : rawSaveData;

            using (var f = saveFile.OpenWrite())
            {
                f.SetLength(0);
                f.Write(saveData, 0, saveData.Length);
            }
        }

        /*
        public void Save()
      {
         var startPart:Array = [];         // Beginning of save to beginning of sietch part
         var sietchPart:Array = [];         // Sietch part
         var endPart:Array = [];            // End of sietch part to save end
         var savegame:Array = [];         // The complete savegame
         var fSave:File = new File();      // The savegame file
         
         // Temporary array for the sietches
         var sietchesTemp:Array = [];
         
         var i:uint = 0, l:uint = saveSequences.length - 1, m:uint = 0;
         
         startPart = decompressedSave.slice(0, sietchStartIndex);
         endPart = decompressedSave.slice(sietchEndIndex);
         
         for (i = 0; i < l; i++) 
         {
            sietchesTemp = loadSietch(saveSequences[i][0], saveSequences[i][1]).toArray();
            for (m = 0; m < sietchesTemp.length; m++) sietchPart.push(sietchesTemp[m]);
         }
         
         if (!noCompression)
         {
            startPart = sc.compressArray(startPart);
            sietchPart = sc.insertF7ControlSequence(sietchPart);
            sietchPart = sc.compressArray(sietchPart);
            endPart = sc.compressArray(endPart);            
         }
         
         for (i = 0; i < startPart.length; i++) savegame.push(startPart[i]);
         for (i = 0; i < sietchPart.length; i++) savegame.push(sietchPart[i]);
         for (i = 0; i < endPart.length; i++) savegame.push(endPart[i]);
         
         compressedSave = savegame;
         
         fSave.addEventListener
         (
            Event.SELECT, 
            function (e:Event):void 
            { 
               var fsSave:FileStream = new FileStream();   
               var fNew:File = e.target as File;         
               fsSave.open(fNew, FileMode.WRITE);
               var i:uint = 0; 
               while (fsSave.position < compressedSave.length) fsSave.writeByte(compressedSave[i++]); 
               fsSave.close(); 
            } 
         );
         
         fSave.browseForSave("Save savegame...");   
      }
        */

        private static int[] FindSequence(byte[] source, Loc sequence)
        {
            var results = new List<int>();
            int l = source.Length - 3;

            for (int i = 0; i < l; )
            {
                if ((source[i] == sequence.v1) &&
                    (source[i + 1] == sequence.v2) &&
                    (source[i + 2] == sequence.v3))
                {
                    results.Add(i);
                    i += 3;
                }
                else i++;
            }
            return results.ToArray();
        }
    }
}
