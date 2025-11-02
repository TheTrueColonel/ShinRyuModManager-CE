using System.Text;

namespace CriPakTools {
    public class EndianReader(Stream input, Encoding encoding, bool isLittleEndian) : BinaryReader(input, encoding) {
        private readonly byte[] _buffer = new byte[8];
        
        public EndianReader(Stream input, bool isLittleEndian)
            : this(input, Encoding.UTF8, isLittleEndian) { }
        
        public bool IsLittleEndian { get; set; } = isLittleEndian;
        
        public override double ReadDouble() {
            if (IsLittleEndian)
                return base.ReadDouble();
            
            FillMyBuffer(8);
            
            return BitConverter.ToDouble(_buffer.Take(8).Reverse().ToArray(), 0);
        }
        
        public override short ReadInt16() {
            if (IsLittleEndian)
                return base.ReadInt16();
            
            FillMyBuffer(2);
            
            return BitConverter.ToInt16(_buffer.Take(2).Reverse().ToArray(), 0);
        }
        
        public override int ReadInt32() {
            if (IsLittleEndian)
                return base.ReadInt32();
            
            FillMyBuffer(4);
            
            return BitConverter.ToInt32(_buffer.Take(4).Reverse().ToArray(), 0);
        }
        
        public override long ReadInt64() {
            if (IsLittleEndian)
                return base.ReadInt64();
            
            FillMyBuffer(8);
            
            return BitConverter.ToInt64(_buffer.Take(8).Reverse().ToArray(), 0);
        }
        
        public override float ReadSingle() {
            if (IsLittleEndian)
                return base.ReadSingle();
            
            FillMyBuffer(4);
            
            return BitConverter.ToSingle(_buffer.Take(4).Reverse().ToArray(), 0);
        }
        
        public override ushort ReadUInt16() {
            if (IsLittleEndian)
                return base.ReadUInt16();
            
            FillMyBuffer(2);
            
            return BitConverter.ToUInt16(_buffer.Take(2).Reverse().ToArray(), 0);
        }
        
        public override uint ReadUInt32() {
            if (IsLittleEndian)
                return base.ReadUInt32();
            
            FillMyBuffer(4);
            
            return BitConverter.ToUInt32(_buffer.Take(4).Reverse().ToArray(), 0);
        }
        
        public override ulong ReadUInt64() {
            if (IsLittleEndian)
                return base.ReadUInt64();
            
            FillMyBuffer(8);
            
            return BitConverter.ToUInt64(_buffer.Take(8).Reverse().ToArray(), 0);
        }
        
        private void FillMyBuffer(int numBytes) {
            var offset = 0;
            int num2;
            
            if (numBytes == 1) {
                num2 = BaseStream.ReadByte();
                
                if (num2 == -1) {
                    throw new EndOfStreamException("Attempted to read past the end of the stream.");
                }
                
                _buffer[0] = (byte)num2;
            } else {
                do {
                    num2 = BaseStream.Read(_buffer, offset, numBytes - offset);
                    
                    if (num2 == 0) {
                        throw new EndOfStreamException("Attempted to read past the end of the stream.");
                    }
                    
                    offset += num2;
                } while (offset < numBytes);
            }
        }
    }
    
    public class EndianWriter(Stream input, Encoding encoding, bool isLittleEndian) : BinaryWriter(input, encoding) {
        public EndianWriter(Stream input, bool isLittleEndian)
            : this(input, Encoding.UTF8, isLittleEndian) { }
        
        public bool IsLittleEndian { get; set; } = isLittleEndian;
        
        public void Write<T>(T value) {
            // A bit gross, but AOT and Trimming compatible
            var someBytes = value switch {
                bool b => BitConverter.GetBytes(b),
                char c => BitConverter.GetBytes(c),
                double d => BitConverter.GetBytes(d),
                Half h => BitConverter.GetBytes(h),
                short s => BitConverter.GetBytes(s),
                int i => BitConverter.GetBytes(i),
                long l => BitConverter.GetBytes(l),
                float f => BitConverter.GetBytes(f),
                ushort us => BitConverter.GetBytes(us),
                uint ui => BitConverter.GetBytes(ui),
                ulong ul => BitConverter.GetBytes(ul),
                _ => throw new NotSupportedException($"Type {typeof(T)} is not supported.")
            };
            
            if (!IsLittleEndian)
                someBytes = someBytes.Reverse().ToArray();
            
            base.Write(someBytes);
        }
        
        public void Write(FileEntry entry) {
            if (entry.ExtractSizeType == typeof(byte)) {
                Write((byte)entry.ExtractSize);
            } else if (entry.ExtractSizeType == typeof(ushort)) {
                Write((ushort)entry.ExtractSize);
            } else if (entry.ExtractSizeType == typeof(uint)) {
                Write((uint)entry.ExtractSize);
            } else if (entry.ExtractSizeType == typeof(ulong)) {
                Write((ulong)entry.ExtractSize);
            } else if (entry.ExtractSizeType == typeof(float)) {
                Write((float)entry.ExtractSize);
            } else {
                throw new Exception("Not supported type!");
            }
        }
    }
}
