using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace JetBlack.TopicBus.IO
{
    public static class NetworkExtensions
    {
        enum ValueType : byte
        {
            NULL = 0,
            BOOL = 1,
            CHAR = 2,
            BYTE = 3,
            SHORT = 4,
            INT = 5,
            LONG = 6,
            FLOAT = 7,
            DOUBLE = 8,
            DATE = 9,
            STRING = 10,
            ARRAY = 11,
            MAP = 12
        }

        static ValueType GetValueType(object value)
        {
            if (value == null)
                return ValueType.NULL;

            Type type = value.GetType();
            TypeCode typeCode = Type.GetTypeCode(type);

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return ValueType.BOOL;
                case TypeCode.Byte:
                    return ValueType.BYTE;
                case TypeCode.Char:
                    return ValueType.CHAR;
                case TypeCode.DateTime:
                    return ValueType.DATE;
                case TypeCode.Double:
                    return ValueType.DOUBLE;
                case TypeCode.Int16:
                    return ValueType.SHORT;
                case TypeCode.Int32:
                    return ValueType.INT;
                case TypeCode.Int64:
                    return ValueType.LONG;
                case TypeCode.Single:
                    return ValueType.FLOAT;
                case TypeCode.String:
                    return ValueType.STRING;
                default:
                    if (type.IsArray)
                        return ValueType.ARRAY;
                    if (value is IDictionary<string, object>)
                        return ValueType.MAP;
                    throw new InvalidDataException("unsupported field type");
            }
        }

        #region Reading

        public static int Read(this Stream stream, byte[] value)
        {
            return stream.Read(value, 0, value.Length);
        }

        public static int Read(this Stream stream, byte[] value, int off, int len)
        {
            return stream.Read(value, off, len);
        }

        public static bool ReadBoolean(this Stream stream)
        {
            return (stream.ReadByte() != 0);
        }

        public static byte ReadByte(this Stream stream)
        {
            int ch = stream.ReadByte();
            if (ch < 0)
                throw new EndOfStreamException();
            return (byte)ch;
        }

        public static char ReadChar(this Stream stream)
        {
            return NetworkBitConverter.ToChar(stream.ReadFully(new byte[2]), 0);
        }

        public static double ReadDouble(this Stream stream)
        {
            return BitConverter.Int64BitsToDouble(stream.ReadInt64());
        }

        public static float ReadFloat(this Stream stream)
        {
            return NetworkBitConverter.ToFloat(stream.ReadFully(new byte[4]), 0);
        }

        public static short ReadInt16(this Stream stream)
        {
            return NetworkBitConverter.ToInt16(stream.ReadFully(new byte[2]), 0);
        }

        public static ushort ReadUInt16(this Stream stream)
        {
            return (ushort)NetworkBitConverter.ToInt16(stream.ReadFully(new byte[2]), 0);
        }

        public static int ReadInt32(this Stream stream)
        {
            return NetworkBitConverter.ToInt32(stream.ReadFully(new byte[4]), 0);
        }

        public static long ReadInt64(this Stream stream)
        {
            return NetworkBitConverter.ToInt64(stream.ReadFully(new byte[8]), 0);
        }

        public static IPAddress ReadIPAddress(this Stream stream)
        {
            var len = stream.ReadInt32();
            var address = new byte[len];
            stream.ReadFully(address);
            return new IPAddress(address);
        }

        public static string ReadString(this Stream stream)
        {
            var len = stream.ReadInt32();
            return Encoding.UTF8.GetString(stream.ReadFully(new byte[len]));
        }

        static byte[] ReadFully(this Stream stream, byte[] buf)
        {
            return stream.ReadFully(buf, 0, buf.Length);
        }

        static byte[] ReadFully(this Stream stream, byte[] buf, int off, int len)
        {
            if (len < 0)
                throw new IndexOutOfRangeException();

            int n = 0;
            while (n < len)
            {
                int count = stream.Read(buf, off + n, len - n);
                if (count < 0)
                    throw new EndOfStreamException();
                n += count;
            }

            return buf;
        }

        public static IDictionary<string, object> ReadDictionary(this Stream stream)
        {
            int fieldCount = stream.ReadInt32();
            return fieldCount <= 0 ? null : stream.ReadDictionary(fieldCount);
        }

        public static DateTime ReadDate(this Stream stream)
        {
            return NetworkBitConverter.Int64ToDate(stream.ReadInt64());
        }

        public static object[] ReadArray(this Stream stream)
        {
            int count = stream.ReadInt32();
            return count <= 0 ? null : stream.ReadArray(count);
        }

        static object[] ReadArray(this Stream stream, int count)
        {
            var values = new object[count];
            for (int i = 0; i < count; ++i)
                values[i] = stream.ReadTypedValue();
            return values;
        }

        static IDictionary<string, object> ReadDictionary(this Stream stream, int count)
        {
            IDictionary<string, object> data = new Dictionary<string, object>(count);
            for (int i = 0; i < count; ++i)
                data.Add(stream.ReadKeyValuePair());
            return data;
        }

        static KeyValuePair<string, object> ReadKeyValuePair(this Stream stream)
        {
            string key = stream.ReadString();
            object value = stream.ReadTypedValue();
            return new KeyValuePair<string, object>(key, value);
        }

        static object ReadTypedValue(this Stream stream)
        {
            var valueType = (ValueType)stream.ReadByte();
            return stream.ReadValue(valueType);
        }

        static object ReadValue(this Stream stream, ValueType valueType)
        {
            switch (valueType)
            {
                case ValueType.NULL:
                    return null;
                case ValueType.BOOL:
                    return stream.ReadBoolean();
                case ValueType.BYTE:
                    return stream.ReadByte();
                case ValueType.CHAR:
                    return stream.ReadChar();
                case ValueType.DATE:
                    return stream.ReadDate();
                case ValueType.DOUBLE:
                    return stream.ReadDouble();
                case ValueType.SHORT:
                    return stream.ReadInt16();
                case ValueType.INT:
                    return stream.ReadInt32();
                case ValueType.LONG:
                    return stream.ReadInt64();
                case ValueType.FLOAT:
                    return stream.ReadFloat();
                case ValueType.STRING:
                    return stream.ReadString();
                case ValueType.ARRAY:
                    return stream.ReadArray();
                case ValueType.MAP:
                    return stream.ReadDictionary();
                default:
                    throw new InvalidDataException("unsupported field type");
            }
        }

        #endregion

        #region Writing

        public static void Write(this Stream stream, bool value)
        {
            stream.WriteByte((byte)(value ? 1 : 0));
        }

        public static void Write(this Stream stream, byte value)
        {
            stream.WriteByte(value);
        }

        public static void Write(this Stream stream, char value)
        {
            stream.Write(NetworkBitConverter.GetBytes(value), 0, 2);
        }

        public static void Write(this Stream stream, int value)
        {
            stream.Write(NetworkBitConverter.GetBytes(value), 0, 4);
        }

        public static void Write(this Stream stream, long value)
        {
            stream.Write(NetworkBitConverter.GetBytes(value), 0, 8);
        }

        public static void Write(this Stream stream, short value)
        {
            stream.Write(NetworkBitConverter.GetBytes(value), 0, 2);
        }

        public static void Write(this Stream stream, ushort value)
        {
            stream.Write(NetworkBitConverter.GetBytes(value), 0, 2);
        }

        public static void Write(this Stream stream, float value)
        {
            stream.Write(NetworkBitConverter.GetBytes(value), 0, 4);
        }

        public static void Write(this Stream stream, double value)
        {
            stream.Write(BitConverter.DoubleToInt64Bits(value));
        }

        public static void Write(this Stream stream, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            stream.Write(bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void Write(this Stream stream, DateTime value)
        {
            stream.Write(NetworkBitConverter.DateToInt64(value));
        }

        public static void Write(this Stream stream, IPAddress ipAddress)
        {
            byte[] address = ipAddress.GetAddressBytes();
            stream.Write(address.Length);
            stream.Write(address, 0, address.Length);
        }

        public static void Write(this Stream stream, IDictionary<string, object> dictionary)
        {
            if (dictionary == null)
            {
                // Field/Value pair count
                stream.Write(0);
            }
            else
            {
                // Field/Value pair count
                stream.Write(dictionary.Count);

                // Field/Value pairs
                foreach (KeyValuePair<string, object> keyValuePair in dictionary)
                    stream.Write(keyValuePair);
            }
        }

        static void Write(this Stream stream, object[] values)
        {
            if (values == null)
                stream.Write(0);
            else
            {
                stream.Write(values.Length);
                foreach (object value in values)
                    stream.WriteValue(value);
            }
        }

        static void Write(this Stream stream, KeyValuePair<string, object> keyValuePair)
        {
            stream.Write(keyValuePair.Key);
            stream.WriteValue(keyValuePair.Value);
        }

        static void WriteValue(this Stream stream, object value)
        {
            ValueType valueType = GetValueType(value);
            stream.Write((byte)valueType);
            stream.Write(valueType, value);
        }

        static void Write(this Stream stream, ValueType valueType, object value)
        {
            switch (valueType)
            {
                case ValueType.NULL:
                    break;
                case ValueType.BOOL:
                    stream.Write((Boolean)value);
                    break;
                case ValueType.BYTE:
                    stream.Write((Byte)value);
                    break;
                case ValueType.CHAR:
                    stream.Write((Char)value);
                    break;
                case ValueType.DATE:
                    stream.Write((DateTime)value);
                    break;
                case ValueType.DOUBLE:
                    stream.Write((Double)value);
                    break;
                case ValueType.SHORT:
                    stream.Write((Int16)value);
                    break;
                case ValueType.INT:
                    stream.Write((Int32)value);
                    break;
                case ValueType.LONG:
                    stream.Write((Int64)value);
                    break;
                case ValueType.FLOAT:
                    stream.Write((Single)value);
                    break;
                case ValueType.STRING:
                    stream.Write((String)value);
                    break;
                case ValueType.ARRAY:
                    stream.Write((object[])value);
                    break;
                case ValueType.MAP:
                    stream.Write(value as IDictionary<string, object>);
                    break;
                default:
                    throw new InvalidDataException("unsupported field type");
            }
        }

        #endregion
    }}

