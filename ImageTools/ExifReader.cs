using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImageTools {
    #region ImageProperty
    public class ImagePropertyId {
        public static ImagePropertyId FileChangeDateTime = new ImagePropertyId(306);
        public static ImagePropertyId DateTimeOriginal = new ImagePropertyId(36867);
        public static ImagePropertyId DateTimeDigitized = new ImagePropertyId(36868);

        readonly int id;
        internal ImagePropertyId(int id) {
            this.id = id;
        }

        public override bool Equals(object obj) {
            return (obj is ImagePropertyId) && ((ImagePropertyId)obj).id == id;
        }
        public override int GetHashCode() {
            return id;
        }
        public override string ToString() {
            return id.ToString();
        }
        public static implicit operator int(ImagePropertyId prop) {
            return prop.id;
        }
        public static implicit operator ImagePropertyId(int id) {
            return new ImagePropertyId(id);
        }
    }
    #endregion

    class ExifReader {
        public enum ByteAlignType {
            Unknown,
            Intel,
            Motorola
        }

        static Dictionary<DataFormat, int> dataFormatBytes = CreateDataFormatTable();

        static Dictionary<DataFormat, int> CreateDataFormatTable() {
            Dictionary<DataFormat, int> result = new Dictionary<DataFormat, int>();
            result.Add(DataFormat.UnsignedByte, 1);
            result.Add(DataFormat.AsciiString, 1);
            result.Add(DataFormat.UnsignedShort, 2);
            result.Add(DataFormat.UnsignedLong, 4);
            result.Add(DataFormat.UnsignedRational, 8);
            result.Add(DataFormat.SignedByte, 1);
            result.Add(DataFormat.Undefined, 1);
            result.Add(DataFormat.SignedShort, 2);
            result.Add(DataFormat.SignedLong, 4);
            result.Add(DataFormat.SignedRational, 8);
            result.Add(DataFormat.SignedFloat, 4);
            result.Add(DataFormat.DoubleFloat, 8);
            return result;
        }

        const int SOIMarker = 0xFFD8;
        const int APP0Marker = 0xFFE0;
        const int APP1Marker = 0xFFE1;
        const int IntelByteAlignMarker = 0x4949;
        const int MotorolaByteAlignMarker = 0x4d4d;
        const int TAGMark = 0x002A;

        const int TIFFHeaderSize = 8;

        int APP1SectionSize;
        ByteAlignType byteAlignType;
        long theTIFFHeaderStartPosition;
        readonly Stream stream;
        public ExifReader(Stream stream) {
            this.stream = stream;
            this.stream.Position = 0;
        }

        public Dictionary<ImagePropertyId, object> ReadMetadata() {
            SkipSOIMarker();
            //SkipAPP1Marker();
            int marker = ReadMarker(2);
            if (marker == APP0Marker) {
                ProcessAPP0Section(marker);
                marker = ReadMarker(2);
            }
            if (marker != APP1Marker)
                throw new Exception(string.Format("{0} markes has not found.", "APP1"));

            this.APP1SectionSize = ReadAPPSectionSize();
            string metadataType = ReadMetadataType();
            if (metadataType != "Exif")
                throw new Exception("Metadata is not an exif format.");
            this.theTIFFHeaderStartPosition = this.stream.Position;
            ReadTIFFHeader();

            Dictionary<ImagePropertyId, object> entries = new Dictionary<ImagePropertyId, object>();
            ReadImageFileDirectoryCore(entries);
            int IFD1Offset = ReadIntValue(4);

            long position = stream.Position;
            stream.Position = IFD1Offset + this.theTIFFHeaderStartPosition;
            ReadImageFileDirectoryCore(entries);
            stream.Position = position;
            return entries;
        }
        void ProcessAPP0Section(int marker) {
            int sectionLength = ReadAPPSectionSize();
            int identifierLength = 5;
            string identifier = ReadASCIIString(identifierLength);
            if (identifier != "JFIF")
                throw new Exception("Metadata is not an JFIF format.");
            this.stream.Position += sectionLength - identifierLength - 2;
        }
        char ConvertToASCIICharValue(byte[] bytes, int startIndex) {
            return (char)bytes[startIndex];
        }
        object ConvertToValue(DataFormat format, byte[] bytes, int startIndex) {
            int dataLength = dataFormatBytes[format];
            switch (format) {
                case DataFormat.DoubleFloat:
                case DataFormat.SignedByte:
                case DataFormat.SignedFloat:
                case DataFormat.SignedLong:
                case DataFormat.SignedShort:
                case DataFormat.UnsignedByte:
                case DataFormat.UnsignedLong:
                case DataFormat.UnsignedShort:
                    return ConvertToInt(bytes, startIndex, dataLength);
                case DataFormat.SignedRational:
                case DataFormat.UnsignedRational:
                    return ConvertToRational(bytes, startIndex);
                default:
                    throw new Exception();
            }
        }
        double ConvertToRational(byte[] bytes, int startIndex) {
            int signedLongLength = dataFormatBytes[DataFormat.SignedLong];
            int numerator = ConvertToInt(bytes, startIndex, signedLongLength);
            int denominator = ConvertToInt(bytes, startIndex + signedLongLength, signedLongLength);
            return denominator != 0 ? numerator / denominator : 0;
        }
        void ReadTIFFHeader() {
            this.byteAlignType = ReadByteAlignType();
            SkipTAGMarker();
            int IFDSectionOffset = ReadIntValue(4) - TIFFHeaderSize;
            this.stream.Position += IFDSectionOffset;
        }
        void ReadImageFileDirectoryCore(Dictionary<ImagePropertyId, object> entries) {
            int entryCount = ReadIntValue(2);
            for (int i = 0; i < entryCount; i++) {
                int tagNumber = ReadIntValue(2);
                DataFormat dataFormat = (DataFormat)ReadIntValue(2);
                int count = ReadIntValue(4);
                int formatLength = dataFormatBytes[dataFormat];
                int valueByteCount = count * formatLength;
                byte[] valueBytes = ReadBytes(4);
                object value;
                if (valueByteCount > 4) {
                    int valueOffset = ConvertToInt(valueBytes);
                    long position = stream.Position;
                    stream.Position = valueOffset + this.theTIFFHeaderStartPosition;
                    value = ReadValue(ReadBytes(valueByteCount), dataFormat, count);
                    stream.Position = position;
                }
                else
                    value = ReadValue(valueBytes, dataFormat, count);
                ProcessEntry(tagNumber, value, entries);
            }
        }
        void ProcessEntry(int tagNumber, object value, Dictionary<ImagePropertyId, object> entries) {
            if (tagNumber == 34665 || tagNumber == 34853) {
                long position = stream.Position;
                stream.Position = (int)value + this.theTIFFHeaderStartPosition;
                ReadImageFileDirectoryCore(entries);
                stream.Position = position;
            }
            else
                entries[new ImagePropertyId(tagNumber)] = value;
        }
        object ReadValue(byte[] valueBytes, DataFormat format, int count) {
            int dataLength = dataFormatBytes[format];
            if (format == DataFormat.AsciiString) {
                string value = string.Empty;
                for (int i = 0; i < count; i++)
                    value += ConvertToASCIICharValue(valueBytes, i * dataLength);
                return value;
            }
            else if (format == DataFormat.Undefined) {
                return valueBytes;
            }
            else {
                List<object> result = new List<object>();
                for (int i = 0; i < count; i++) {
                    object value = ConvertToValue(format, valueBytes, i * dataLength);
                    result.Add(value);
                }
                return result.Count == 1 ? result[0] : result.ToArray();
            }
        }
        void SkipSOIMarker() {
            SkipMarker(SOIMarker, "SOI");
        }
        void SkipAPP1Marker() {
            SkipMarker(APP1Marker, "APP1");
        }
        void SkipTAGMarker() {
            SkipMarker(TAGMark, "TAG");
        }
        int ReadAPPSectionSize() {
            return ConvertToInt(ReadBytes(2));
        }
        string ReadMetadataType() {
            StringBuilder result = new StringBuilder();
            bool waitTheEnd = false;
            while (true) {
                int b = this.stream.ReadByte();
                if (b < 0)
                    throw new Exception("The stream is unexpected end.");
                if (b == 0) {
                    if (waitTheEnd)
                        break;
                    else
                        waitTheEnd = true;
                }
                else {
                    result.Append((char)b);
                    waitTheEnd = false;
                }
            }
            return result.ToString();
        }
        ByteAlignType ReadByteAlignType() {
            int result = ReadMarker(2);
            if (result == IntelByteAlignMarker)
                return ByteAlignType.Intel;
            if (result == MotorolaByteAlignMarker)
                return ByteAlignType.Motorola;
            return ByteAlignType.Unknown;
        }
        int ReadIntValue(int byteCount) {
            return ConvertToInt(ReadBytes(byteCount));
        }
        void SkipMarker(int marker, string markerName) {
            int result = ReadMarker(2);
            if (result < 0 || result != marker)
                throw new Exception(string.Format("{0} markes has not found.", markerName));
        }
        int ReadMarker(int byteCount) {
            return ConvertToInt(ReadBytes(2));
        }
        int ConvertToInt(byte[] bytes, int startIndex, int length) {
            if (bytes == null || bytes.Length < startIndex + length)
                return 0;
            if (this.byteAlignType == ByteAlignType.Intel)
                return ConvertToIntIntelOrder(bytes, startIndex, length);
            else
                return ConvertToIntMotorolaOrder(bytes, startIndex, length);
        }
        static int ConvertToIntMotorolaOrder(byte[] bytes, int startIndex, int length) {
            int result = bytes[startIndex];
            for (int i = 1; i < length; i++)
                result = (result << 8) | bytes[startIndex + i];
            return result;
        }
        static int ConvertToIntIntelOrder(byte[] bytes, int startIndex, int length) {
            int result = bytes[startIndex];
            for (int i = 1; i < length; i++)
                result = result | bytes[startIndex + i] << 8;
            return result;
        }
        int ConvertToInt(byte[] bytes) {
            return ConvertToInt(bytes, 0, bytes.Length);
        }
        byte[] ReadBytes(int byteCount) {
            byte[] bytes = new byte[byteCount];
            int readedByteCount = this.stream.Read(bytes, 0, byteCount);
            if (readedByteCount < byteCount)
                throw new Exception("The stream is unexpected end.");
            return bytes;
        }
        byte[] ReverseBytes(byte[] bytes) {
            int first = 0;
            int last = bytes.Length - 1;
            while (first < last) {
                byte temp = bytes[first];
                bytes[first] = bytes[last];
                bytes[last] = temp;
                first++;
                last--;
            }
            return bytes;
        }
        string ReadASCIIString(int byteCount) {
            byte[] bytes = ReadBytes(byteCount);
            if (bytes == null || bytes.Length == 0)
                return null;
            if (bytes[bytes.Length - 1] != '\0')
                throw new Exception("The string is not complited.");
            return Encoding.ASCII.GetString(bytes, 0, bytes.Length - 1);
        }
    }
    public enum DataFormat {
        UnsignedByte = 1,
        AsciiString = 2,
        UnsignedShort = 3,
        UnsignedLong = 4,
        UnsignedRational = 5,
        SignedByte = 6,
        Undefined = 7,
        SignedShort = 8,
        SignedLong = 9,
        SignedRational = 10,
        SignedFloat = 11,
        DoubleFloat = 12
    }
}
