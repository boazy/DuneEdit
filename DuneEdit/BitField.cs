using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace DuneEdit
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class BitFieldAttribute : Attribute
    {
        public uint Length { get; set; }

        public BitFieldAttribute(uint length = 1)
        {
            this.Length = length;
        }
    }

    public static class BitFieldDecoder<T> where T : new()
    {
        public static T Decode<Src>(Src src) where Src : System.IConvertible
        {
            return DecodeLong((ulong)Convert.ChangeType(src, typeof(ulong)));
        }

        private static T DecodeLong(ulong src)
        {
            T res = new T();
            int offset = 0;

            // For every field suitably attributed with a BitfieldLength
            foreach (FieldInfo f in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object[] attrs = f.GetCustomAttributes(typeof(BitFieldAttribute), false);
                if (attrs.Length == 1)
                {
                    uint fieldLength = ((BitFieldAttribute)attrs[0]).Length;

                    // Calculate a bitmask of the desired length
                    ulong mask = 0;
                    for (int i = 0; i < fieldLength; i++)
                        mask |= (ulong)1 << i;

                    object lVal = (src & (mask << offset)) >> offset;
                    object fieldVal = Convert.ChangeType(lVal, f.FieldType);
                    f.SetValue(res, fieldVal);

                    offset += (int)fieldLength;
                }
            }

            return res;
        }
    }

    public static class BitFieldEncoder<Target> where Target : System.IConvertible
    {
        public static Target Encode<T>(T t) where T : class
        {
            return (Target)Convert.ChangeType(EncodeLong(t), typeof(Target));
        }

        private static ulong EncodeLong<T>(T t) where T : class
        {
            ulong res = 0;
            int offset = 0;

            // For every field suitably attributed with a BitfieldLength
            foreach (FieldInfo f in t.GetType().GetFields())
            {
                object[] attrs = f.GetCustomAttributes(typeof(BitFieldAttribute), false);
                if (attrs.Length == 1)
                {
                    uint fieldLength = ((BitFieldAttribute)attrs[0]).Length;

                    // Calculate a bitmask of the desired length
                    ulong mask = 0;
                    for (int i = 0; i < fieldLength; i++)
                        mask |= (ulong)1 << i;

                    ulong fieldVal = (ulong)Convert.ChangeType(f.GetValue(t), typeof(ulong));
                    res |= (fieldVal & mask) << offset;

                    offset += (int)fieldLength;
                }
            }

            return res;
        }
    }
}
