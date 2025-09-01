using System.Text;

namespace CriPakTools {
    public class CPK(Tools tool) {
        public readonly List<FileEntry> FileTable = new();
        public Dictionary<string, object> Cpkdata;
        public UTF Utf;
        
        private UTF _files;
        
        public bool ReadCPK(string sPath) {
            if (!File.Exists(sPath))
                return false;
            
            uint Files;
            ushort Align;
            
            var br = new EndianReader(File.OpenRead(sPath), true);
            
            if (Tools.ReadCString(br, 4) != "CPK ") {
                br.Close();
                
                return false;
            }
            
            ReadUTFData(br);
            
            CpkPacket = UtfPacket;
            //Dump CPK
            //File.WriteAllBytes("U_CPK", CPK_packet);
            
            var cpakEntry = new FileEntry {
                FileName = "CPK_HDR",
                FileOffsetPos = br.BaseStream.Position + 0x10,
                FileSize = CpkPacket.Length,
                Encrypted = IsUtfEncrypted,
                FileType = "CPK"
            };
            
            FileTable.Add(cpakEntry);
            
            var ms = new MemoryStream(UtfPacket);
            var utfr = new EndianReader(ms, false);
            
            Utf = new UTF(tool);
            
            if (!Utf.ReadUTF(utfr)) {
                br.Close();
                
                return false;
            }
            
            utfr.Close();
            ms.Close();
            
            Cpkdata = new Dictionary<string, object>();
            
            try {
                for (var i = 0; i < Utf.Columns.Count; i++) {
                    Cpkdata.Add(Utf.Columns[i].Name, Utf.Rows[0].Rows[i].GetValue());
                }
            } catch (Exception ex) {
                //MessageBox.Show(ex.ToString());
                Console.WriteLine(ex.ToString());
            }
            
            TocOffset = (ulong)GetColumnsData2(Utf, 0, "TocOffset", 3);
            var TocOffsetPos = GetColumnPosition(Utf, 0, "TocOffset");
            
            EtocOffset = (ulong)GetColumnsData2(Utf, 0, "EtocOffset", 3);
            var ETocOffsetPos = GetColumnPosition(Utf, 0, "EtocOffset");
            
            ItocOffset = (ulong)GetColumnsData2(Utf, 0, "ItocOffset", 3);
            var ITocOffsetPos = GetColumnPosition(Utf, 0, "ItocOffset");
            
            GtocOffset = (ulong)GetColumnsData2(Utf, 0, "GtocOffset", 3);
            var GTocOffsetPos = GetColumnPosition(Utf, 0, "GtocOffset");
            
            ContentOffset = (ulong)GetColumnsData2(Utf, 0, "ContentOffset", 3);
            var ContentOffsetPos = GetColumnPosition(Utf, 0, "ContentOffset");
            
            FileTable.Add(CreateFileEntry("CONTENT_OFFSET", ContentOffset, typeof(ulong), ContentOffsetPos, "CPK",
                "CONTENT", false));
            
            Files = (uint)GetColumnsData2(Utf, 0, "Files", 2);
            Align = (ushort)GetColumnsData2(Utf, 0, "Align", 1);
            
            if (TocOffset != 0xFFFFFFFFFFFFFFFF) {
                var entry = CreateFileEntry("TOC_HDR", TocOffset, typeof(ulong), TocOffsetPos, "CPK", "HDR",
                    false);
                
                FileTable.Add(entry);
                
                if (!ReadTOC(br, TocOffset, ContentOffset))
                    return false;
            }
            
            if (EtocOffset != 0xFFFFFFFFFFFFFFFF) {
                var entry = CreateFileEntry("ETOC_HDR", EtocOffset, typeof(ulong), ETocOffsetPos, "CPK",
                    "HDR", false);
                
                FileTable.Add(entry);
                
                if (!ReadETOC(br, EtocOffset))
                    return false;
            }
            
            if (ItocOffset != 0xFFFFFFFFFFFFFFFF) {
                //FileEntry ITOC_entry = new FileEntry { 
                //    FileName = "ITOC_HDR",
                //    FileOffset = ItocOffset, FileOffsetType = typeof(ulong), FileOffsetPos = ITocOffsetPos,
                //    TOCName = "CPK",
                //    FileType = "FILE", Encrypted = true,
                //};
                
                var entry = CreateFileEntry("ITOC_HDR", ItocOffset, typeof(ulong), ITocOffsetPos, "CPK",
                    "HDR", false);
                
                FileTable.Add(entry);
                
                if (!ReadITOC(br, ItocOffset, ContentOffset, Align))
                    return false;
            }
            
            if (GtocOffset != 0xFFFFFFFFFFFFFFFF) {
                var entry = CreateFileEntry("GTOC_HDR", GtocOffset, typeof(ulong), GTocOffsetPos, "CPK",
                    "HDR", false);
                
                FileTable.Add(entry);
                
                if (!ReadGTOC(br, GtocOffset))
                    return false;
            }
            
            br.Close();
            
            // at this point, we should have all needed file info
            
            //utf = null;
            _files = null;
            
            return true;
        }
        
        private static FileEntry CreateFileEntry(string fileName, ulong fileOffset, Type fileOffsetType,
            long fileOffsetPos, string tocName, string fileType, bool encrypted) {
            var entry = new FileEntry {
                FileName = fileName,
                FileOffset = fileOffset,
                FileOffsetType = fileOffsetType,
                FileOffsetPos = fileOffsetPos,
                TOCName = tocName,
                FileType = fileType,
                Encrypted = encrypted,
            };
            
            return entry;
        }
        
        public bool ReadTOC(EndianReader br, ulong tocOffset, ulong contentOffset) {
            var addOffset = contentOffset < tocOffset ? contentOffset : tocOffset;
            
            br.BaseStream.Seek((long)tocOffset, SeekOrigin.Begin);
            
            if (Tools.ReadCString(br, 4) != "TOC ") {
                br.Close();
                
                return false;
            }
            
            ReadUTFData(br);
            
            // Store unencrypted TOC
            TocPacket = UtfPacket;
            //Dump TOC
            //File.WriteAllBytes("U_TOC", TOC_packet);
            
            var tocEntry = FileTable.Single(x => x.FileName.ToString() == "TOC_HDR");
            tocEntry.Encrypted = IsUtfEncrypted;
            tocEntry.FileSize = TocPacket.Length;
            
            var ms = new MemoryStream(UtfPacket);
            var utfr = new EndianReader(ms, false);
            
            _files = new UTF(tool);
            
            if (!_files.ReadUTF(utfr)) {
                br.Close();
                
                return false;
            }
            
            utfr.Close();
            ms.Close();
            
            for (var i = 0; i < _files.NumRows; i++) {
                var temp = new FileEntry {
                    TOCName = "TOC",
                    DirName = GetColumnData(_files, i, "DirName"),
                    FileName = GetColumnData(_files, i, "FileName"),
                    FileSize = GetColumnData(_files, i, "FileSize"),
                    FileSizePos = GetColumnPosition(_files, i, "FileSize"),
                    FileSizeType = GetColumnType(_files, i, "FileSize"),
                    ExtractSize = GetColumnData(_files, i, "ExtractSize"),
                    ExtractSizePos = GetColumnPosition(_files, i, "ExtractSize"),
                    ExtractSizeType = GetColumnType(_files, i, "ExtractSize"),
                    FileOffset = ((ulong)GetColumnData(_files, i, "FileOffset") + addOffset),
                    FileOffsetPos = GetColumnPosition(_files, i, "FileOffset"),
                    FileOffsetType = GetColumnType(_files, i, "FileOffset"),
                    FileType = "FILE",
                    Offset = addOffset,
                    ID = GetColumnData(_files, i, "ID"),
                    UserString = GetColumnData(_files, i, "UserString")
                };
                
                FileTable.Add(temp);
            }
            
            _files = null;
            
            return true;
        }
        
        public void WriteCPK(BinaryWriter cpk) {
            WritePacket(cpk, "CPK ", 0, CpkPacket);
            
            cpk.BaseStream.Seek(0x800 - 6, SeekOrigin.Begin);
            cpk.Write("(c)CRI"u8.ToArray());
        }
        
        public void WriteTOC(BinaryWriter cpk) {
            WritePacket(cpk, "TOC ", TocOffset, TocPacket);
        }
        
        public void WriteITOC(BinaryWriter cpk) {
            WritePacket(cpk, "ITOC", ItocOffset, ItocPacket);
        }
        
        public void WriteETOC(BinaryWriter cpk) {
            WritePacket(cpk, "ETOC", EtocOffset, EtocPacket);
        }
        
        public void WriteGTOC(BinaryWriter cpk) {
            WritePacket(cpk, "GTOC", GtocOffset, GtocPacket);
        }
        
        public void WritePacket(BinaryWriter cpk, string id, ulong position, byte[] packet) {
            if (position == 0xffffffffffffffff)
                return;
            
            cpk.BaseStream.Seek((long)position, SeekOrigin.Begin);
            var encrypted = DecryptUTF(packet); // Yes it says decrypt...
            cpk.Write(Encoding.ASCII.GetBytes(id));
            cpk.Write(0);
            cpk.Write((ulong)encrypted.Length);
            cpk.Write(encrypted);
        }
        
        public bool ReadITOC(EndianReader br, ulong startOffset, ulong contentOffset, ushort align) {
            br.BaseStream.Seek((long)startOffset, SeekOrigin.Begin);
            
            if (Tools.ReadCString(br, 4) != "ITOC") {
                br.Close();
                
                return false;
            }
            
            ReadUTFData(br);
            
            ItocPacket = UtfPacket;
            //Dump ITOC
            //File.WriteAllBytes("U_ITOC", ITOC_packet);
            
            var itocEntry = FileTable.Single(x => x.FileName.ToString() == "ITOC_HDR");
            itocEntry.Encrypted = IsUtfEncrypted;
            itocEntry.FileSize = ItocPacket.Length;
            
            var ms = new MemoryStream(UtfPacket);
            var utfr = new EndianReader(ms, false);
            
            _files = new UTF(tool);
            
            if (!_files.ReadUTF(utfr)) {
                br.Close();
                
                return false;
            }
            
            utfr.Close();
            ms.Close();
            
            //uint FilesL = (uint)GetColumnData(files, 0, "FilesL");
            //uint FilesH = (uint)GetColumnData(files, 0, "FilesH");
            var dataL = (byte[])GetColumnData(_files, 0, "DataL");
            var dataLPos = GetColumnPosition(_files, 0, "DataL");
            
            var dataH = (byte[])GetColumnData(_files, 0, "DataH");
            var dataHPos = GetColumnPosition(_files, 0, "DataH");
            
            //MemoryStream ms;
            //EndianReader ir;
            UTF utfDataL, utfDataH;
            
            var IDs = new List<int>();
            
            var SizeTable = new Dictionary<int, uint>();
            var SizePosTable = new Dictionary<int, long>();
            var SizeTypeTable = new Dictionary<int, Type>();
            
            var CSizeTable = new Dictionary<int, uint>();
            var CSizePosTable = new Dictionary<int, long>();
            var CSizeTypeTable = new Dictionary<int, Type>();
            
            ushort ID;
            long pos;
            Type type;
            
            if (dataL != null) {
                ms = new MemoryStream(dataL);
                utfr = new EndianReader(ms, false);
                utfDataL = new UTF(tool);
                utfDataL.ReadUTF(utfr);
                
                for (var i = 0; i < utfDataL.NumRows; i++) {
                    ID = (ushort)GetColumnData(utfDataL, i, "ID");
                    var size1 = (ushort)GetColumnData(utfDataL, i, "FileSize");
                    SizeTable.Add(ID, size1);
                    
                    pos = GetColumnPosition(utfDataL, i, "FileSize");
                    SizePosTable.Add(ID, pos + dataLPos);
                    
                    type = GetColumnType(utfDataL, i, "FileSize");
                    SizeTypeTable.Add(ID, type);
                    
                    if ((GetColumnData(utfDataL, i, "ExtractSize")) != null) {
                        size1 = (ushort)GetColumnData(utfDataL, i, "ExtractSize");
                        CSizeTable.Add(ID, size1);
                        
                        pos = GetColumnPosition(utfDataL, i, "ExtractSize");
                        CSizePosTable.Add(ID, pos + dataLPos);
                        
                        type = GetColumnType(utfDataL, i, "ExtractSize");
                        CSizeTypeTable.Add(ID, type);
                    }
                    
                    IDs.Add(ID);
                }
            }
            
            if (dataH != null) {
                ms = new MemoryStream(dataH);
                utfr = new EndianReader(ms, false);
                utfDataH = new UTF(tool);
                utfDataH.ReadUTF(utfr);
                
                for (var i = 0; i < utfDataH.NumRows; i++) {
                    ID = (ushort)GetColumnData(utfDataH, i, "ID");
                    var size2 = (uint)GetColumnData(utfDataH, i, "FileSize");
                    SizeTable.Add(ID, size2);
                    
                    pos = GetColumnPosition(utfDataH, i, "FileSize");
                    SizePosTable.Add(ID, pos + dataHPos);
                    
                    type = GetColumnType(utfDataH, i, "FileSize");
                    SizeTypeTable.Add(ID, type);
                    
                    if ((GetColumnData(utfDataH, i, "ExtractSize")) != null) {
                        size2 = (uint)GetColumnData(utfDataH, i, "ExtractSize");
                        CSizeTable.Add(ID, size2);
                        
                        pos = GetColumnPosition(utfDataH, i, "ExtractSize");
                        CSizePosTable.Add(ID, pos + dataHPos);
                        
                        type = GetColumnType(utfDataH, i, "ExtractSize");
                        CSizeTypeTable.Add(ID, type);
                    }
                    
                    IDs.Add(ID);
                }
            }
            
            //int id = 0;
            var baseoffset = contentOffset;
            
            // Seems ITOC can mix up the IDs..... but they'll alwaysy be in order...
            IDs = IDs.OrderBy(x => x).ToList();
            
            foreach (var id in IDs) {
                var temp = new FileEntry();
                SizeTable.TryGetValue(id, out var value);
                CSizeTable.TryGetValue(id, out var value2);
                
                temp.TOCName = "ITOC";
                
                temp.DirName = null;
                temp.FileName = id.ToString("D4");
                
                temp.FileSize = value;
                temp.FileSizePos = SizePosTable[id];
                temp.FileSizeType = SizeTypeTable[id];
                
                if (CSizeTable.Count > 0 && CSizeTable.ContainsKey(id)) {
                    temp.ExtractSize = value2;
                    temp.ExtractSizePos = CSizePosTable[id];
                    temp.ExtractSizeType = CSizeTypeTable[id];
                }
                
                temp.FileType = "FILE";
                
                temp.FileOffset = baseoffset;
                temp.ID = id;
                temp.UserString = null;
                
                FileTable.Add(temp);
                
                if ((value % align) > 0)
                    baseoffset += value + (align - (value % align));
                else
                    baseoffset += value;
            }
            
            _files = null;
            utfDataL = null;
            utfDataH = null;
            
            ms.Close();
            utfr.Close();
            
            return true;
        }
        
        private void ReadUTFData(EndianReader br) {
            IsUtfEncrypted = false;
            br.IsLittleEndian = true;
            
            Unk1 = br.ReadInt32();
            UtfSize = br.ReadInt64();
            UtfPacket = br.ReadBytes((int)UtfSize);
            
            if (UtfPacket[0] != 0x40 && UtfPacket[1] != 0x55 && UtfPacket[2] != 0x54 && UtfPacket[3] != 0x46) //@UTF
            {
                UtfPacket = DecryptUTF(UtfPacket);
                IsUtfEncrypted = true;
            }
            
            br.IsLittleEndian = false;
        }
        
        public bool ReadGTOC(EndianReader br, ulong startoffset) {
            br.BaseStream.Seek((long)startoffset, SeekOrigin.Begin);
            
            if (Tools.ReadCString(br, 4) != "GTOC") {
                br.Close();
                
                return false;
            }
            
            br.BaseStream.Seek(0xC, SeekOrigin.Current); //skip header data
            
            return true;
        }
        
        public bool ReadETOC(EndianReader br, ulong startoffset) {
            br.BaseStream.Seek((long)startoffset, SeekOrigin.Begin);
            
            if (Tools.ReadCString(br, 4) != "ETOC") {
                br.Close();
                
                return false;
            }
            
            //br.BaseStream.Seek(0xC, SeekOrigin.Current); //skip header data
            
            ReadUTFData(br);
            
            EtocPacket = UtfPacket;
            //Dump ETOC
            //File.WriteAllBytes("U_ETOC", ETOC_packet);
            
            var etocEntry = FileTable.Single(x => x.FileName.ToString() == "ETOC_HDR");
            etocEntry.Encrypted = IsUtfEncrypted;
            etocEntry.FileSize = EtocPacket.Length;
            
            var ms = new MemoryStream(UtfPacket);
            var utfr = new EndianReader(ms, false);
            
            _files = new UTF(tool);
            
            if (!_files.ReadUTF(utfr)) {
                br.Close();
                
                return false;
            }
            
            utfr.Close();
            ms.Close();
            
            var fileEntries = FileTable.Where(x => x.FileType == "FILE").ToList();
            
            for (var i = 0; i < fileEntries.Count; i++) {
                FileTable[i].LocalDir = GetColumnData(_files, i, "LocalDir");
            }
            
            return true;
        }
        
        public byte[] DecryptUTF(byte[] input) {
            var result = new byte[input.Length];
            
            var m = 0x0000655f;
            const int t = 0x00004115;
            
            for (var i = 0; i < input.Length; i++) {
                var d = input[i];
                d = (byte)(d ^ (byte)(m & 0xff));
                result[i] = d;
                m *= t;
            }
            
            return result;
        }
        
        public byte[] DecompressCRILAYLA(byte[] input, int uSize) {
            var ms = new MemoryStream(input);
            var br = new EndianReader(ms, true);
            
            br.BaseStream.Seek(8, SeekOrigin.Begin); // Skip CRILAYLA
            var uncompressedSize = br.ReadInt32();
            var uncompressedHeaderOffset = br.ReadInt32();
            var result = new byte[uncompressedSize + 0x100]; // = new byte[USize];
            // do some error checks here.........
            // copy uncompressed 0x100 header to start of file
            Array.Copy(input, uncompressedHeaderOffset + 0x10, result, 0, 0x100);
            
            var inputEnd = input.Length - 0x100 - 1;
            var inputOffset = inputEnd;
            var outputEnd = 0x100 + uncompressedSize - 1;
            byte bitPool = 0;
            int bitsLeft = 0, bytesOutput = 0;
            var vleLens = new int[4] { 2, 3, 5, 8 };
            
            while (bytesOutput < uncompressedSize) {
                if (GenNextBits(input, ref inputOffset, ref bitPool, ref bitsLeft, 1) > 0) {
                    var backreferenceOffset = outputEnd - bytesOutput +
                        GenNextBits(input, ref inputOffset, ref bitPool, ref bitsLeft, 13) + 3;
                    
                    var backreferenceLength = 3;
                    int vleLevel;
                    
                    for (vleLevel = 0; vleLevel < vleLens.Length; vleLevel++) {
                        int thisLevel = GenNextBits(input, ref inputOffset, ref bitPool, ref bitsLeft,
                            vleLens[vleLevel]);
                        
                        backreferenceLength += thisLevel;
                        
                        if (thisLevel != ((1 << vleLens[vleLevel]) - 1))
                            break;
                    }
                    
                    if (vleLevel == vleLens.Length) {
                        int thisLevel;
                        
                        do {
                            thisLevel = GenNextBits(input, ref inputOffset, ref bitPool, ref bitsLeft, 8);
                            backreferenceLength += thisLevel;
                        } while (thisLevel == 255);
                    }
                    
                    for (var i = 0; i < backreferenceLength; i++) {
                        result[outputEnd - bytesOutput] = result[backreferenceOffset--];
                        bytesOutput++;
                    }
                } else {
                    // verbatim byte
                    result[outputEnd - bytesOutput] =
                        (byte)GenNextBits(input, ref inputOffset, ref bitPool, ref bitsLeft, 8);
                    
                    bytesOutput++;
                }
            }
            
            br.Close();
            ms.Close();
            
            return result;
        }
        
        private static ushort GenNextBits(byte[] input, ref int offsetP, ref byte bitPoolP, ref int bitsLeftP,
            int bitCount) {
            ushort outBits = 0;
            var numBitsProduced = 0;
            
            while (numBitsProduced < bitCount) {
                if (bitsLeftP == 0) {
                    bitPoolP = input[offsetP];
                    bitsLeftP = 8;
                    offsetP--;
                }
                
                int bitsThisRound;
                
                if (bitsLeftP > (bitCount - numBitsProduced))
                    bitsThisRound = bitCount - numBitsProduced;
                else
                    bitsThisRound = bitsLeftP;
                
                outBits <<= bitsThisRound;
                
                outBits |= (ushort)((ushort)(bitPoolP >> (bitsLeftP - bitsThisRound)) &
                    ((1 << bitsThisRound) - 1));
                
                bitsLeftP -= bitsThisRound;
                numBitsProduced += bitsThisRound;
            }
            
            return outBits;
        }
        
        public object GetColumnsData2(UTF utf, int row, string name, int type) {
            var temp = GetColumnData(utf, row, name);
            
            if (temp == null) {
                switch (type) {
                    case 0: // byte
                        return (byte)0xFF;
                    case 1: // short
                        return (ushort)0xFFFF;
                    case 2: // int
                        return 0xFFFFFFFF;
                    case 3: // long
                        return 0xFFFFFFFFFFFFFFFF;
                }
            }
            
            if (temp is ulong tempUlong) {
                return tempUlong;
            }
            
            if (temp is uint tempUint) {
                return tempUint;
            }
            
            if (temp is ushort tempUshort) {
                return tempUshort;
            }
            
            return 0;
        }
        
        public static object GetColumnData(UTF utf, int row, string name) {
            object result = null;
            
            try {
                for (var i = 0; i < utf.NumColumns; i++) {
                    if (utf.Columns[i].Name != name)
                        continue;
                    
                    result = utf.Rows[row].Rows[i].GetValue();
                    
                    break;
                }
            } catch (Exception ex) {
                //MessageBox.Show(ex.ToString());
                Console.WriteLine(ex.ToString());
                
                return null;
            }
            
            return result;
        }
        
        public static long GetColumnPosition(UTF utf, int row, string name) {
            long result = -1;
            
            try {
                for (var i = 0; i < utf.NumColumns; i++) {
                    if (utf.Columns[i].Name != name)
                        continue;
                    
                    result = utf.Rows[row].Rows[i].Position;
                    
                    break;
                }
            } catch (Exception ex) {
                //MessageBox.Show(ex.ToString());
                Console.WriteLine(ex.ToString());
                
                return -1;
            }
            
            return result;
        }
        
        public static Type GetColumnType(UTF utf, int row, string name) {
            Type result = null;
            
            try {
                for (var i = 0; i < utf.NumColumns; i++) {
                    if (utf.Columns[i].Name != name)
                        continue;
                    
                    result = utf.Rows[row].Rows[i].GetType();
                    
                    break;
                }
            } catch (Exception ex) {
                //MessageBox.Show(ex.ToString());
                Console.WriteLine(ex.ToString());
                
                return null;
            }
            
            return result;
        }
        
        public void UpdateFileEntry(FileEntry fileEntry) {
            if (fileEntry.FileType is not ("FILE" or "HDR"))
                return;
            
            var updateMe = fileEntry.TOCName switch {
                "CPK" => CpkPacket,
                "TOC" => TocPacket,
                "ITOC" => ItocPacket,
                "ETOC" => EtocPacket,
                _ => throw new Exception("I need to implement this TOC!")
            };
            
            //Update ExtractSize
            if (fileEntry.ExtractSizePos > 0)
                UpdateValue(ref updateMe, fileEntry.ExtractSize, fileEntry.ExtractSizePos,
                    fileEntry.ExtractSizeType);
            
            //Update FileSize
            if (fileEntry.FileSizePos > 0)
                UpdateValue(ref updateMe, fileEntry.FileSize, fileEntry.FileSizePos, fileEntry.FileSizeType);
            
            //Update FileOffset
            if (fileEntry.FileOffsetPos > 0)
                UpdateValue(ref updateMe, fileEntry.FileOffset - (ulong)((fileEntry.TOCName == "TOC") ? 0x800 : 0),
                    fileEntry.FileOffsetPos, fileEntry.FileOffsetType);
            
            switch (fileEntry.TOCName) {
                case "CPK":
                    updateMe = CpkPacket;
                    
                    break;
                case "TOC":
                    TocPacket = updateMe;
                    
                    break;
                case "ITOC":
                    ItocPacket = updateMe;
                    
                    break;
                case "ETOC":
                    updateMe = EtocPacket;
                    
                    break;
                default:
                    throw new Exception("I need to implement this TOC!");
            }
        }
        
        public static void UpdateValue(ref byte[] packet, object value, long pos, Type type) {
            var temp = new MemoryStream();
            temp.Write(packet, 0, packet.Length);
            
            var toc = new EndianWriter(temp, false);
            toc.Seek((int)pos, SeekOrigin.Begin);
            
            value = Convert.ChangeType(value, type);
            
            if (type == typeof(byte)) {
                toc.Write((byte)value);
            } else if (type == typeof(ushort)) {
                toc.Write((ushort)value);
            } else if (type == typeof(uint)) {
                toc.Write((uint)value);
            } else if (type == typeof(ulong)) {
                toc.Write((ulong)value);
            } else if (type == typeof(float)) {
                toc.Write((float)value);
            } else {
                throw new Exception("Not supported type!");
            }
            
            toc.Close();
            
            var myStream = (MemoryStream)toc.BaseStream;
            packet = myStream.ToArray();
        }
        
        public bool IsUtfEncrypted { get; set; }
        public int Unk1 { get; set; }
        public long UtfSize { get; set; }
        public byte[] UtfPacket { get; set; }
        
        public byte[] CpkPacket { get; set; }
        public byte[] TocPacket { get; set; }
        public byte[] ItocPacket { get; set; }
        public byte[] EtocPacket { get; set; }
        public byte[] GtocPacket { get; set; }
        
        public ulong TocOffset, EtocOffset, ItocOffset, GtocOffset, ContentOffset;
    }
    
    public class UTF {
        public enum COLUMN_FLAGS : int {
            STORAGE_MASK = 0xf0,
            STORAGE_NONE = 0x00,
            STORAGE_ZERO = 0x10,
            STORAGE_CONSTANT = 0x30,
            STORAGE_PERROW = 0x50,
            
            TYPE_MASK = 0x0f,
            TYPE_DATA = 0x0b,
            TYPE_STRING = 0x0a,
            TYPE_FLOAT = 0x08,
            TYPE_8BYTE2 = 0x07,
            TYPE_8BYTE = 0x06,
            TYPE_4BYTE2 = 0x05,
            TYPE_4BYTE = 0x04,
            TYPE_2BYTE2 = 0x03,
            TYPE_2BYTE = 0x02,
            TYPE_1BYTE2 = 0x01,
            TYPE_1BYTE = 0x00,
        }
        
        public List<COLUMN> Columns;
        public List<ROWS> Rows;
        
        Tools tools;
        
        public UTF(Tools tool) {
            tools = tool;
        }
        
        public bool ReadUTF(EndianReader br) {
            var offset = br.BaseStream.Position;
            
            if (Tools.ReadCString(br, 4) != "@UTF") {
                return false;
            }
            
            TableSize = br.ReadInt32();
            RowsOffset = br.ReadInt32();
            StringsOffset = br.ReadInt32();
            DataOffset = br.ReadInt32();
            
            // CPK Header & UTF Header are ignored, so add 8 to each offset
            RowsOffset += (offset + 8);
            StringsOffset += (offset + 8);
            DataOffset += (offset + 8);
            
            TableName = br.ReadInt32();
            NumColumns = br.ReadInt16();
            RowLength = br.ReadInt16();
            NumRows = br.ReadInt32();
            
            //read Columns
            Columns = [];
            
            for (var i = 0; i < NumColumns; i++) {
                var column = new COLUMN {
                    Flags = br.ReadByte()
                };
                
                if (column.Flags == 0) {
                    br.BaseStream.Seek(3, SeekOrigin.Current);
                    column.Flags = br.ReadByte();
                }
                
                column.Name = Tools.ReadCString(br, -1, br.ReadInt32() + StringsOffset);
                Columns.Add(column);
            }
            
            //read Rows
            
            Rows = [];
            
            for (var j = 0; j < NumRows; j++) {
                br.BaseStream.Seek(RowsOffset + (j * RowLength), SeekOrigin.Begin);
                
                var currentEntry = new ROWS();
                
                for (var i = 0; i < NumColumns; i++) {
                    var currentRow = new ROW();
                    var storageFlag = (Columns[i].Flags & (int)COLUMN_FLAGS.STORAGE_MASK);
                    
                    switch (storageFlag) {
                        case (int)COLUMN_FLAGS.STORAGE_NONE: // 0x00
                        case (int)COLUMN_FLAGS.STORAGE_ZERO: // 0x10
                        case (int)COLUMN_FLAGS.STORAGE_CONSTANT: // 0x30
                            currentEntry.Rows.Add(currentRow);
                            
                            continue;
                    }
                    
                    // 0x50
                    currentRow.Type = Columns[i].Flags & (int)COLUMN_FLAGS.TYPE_MASK;
                    
                    currentRow.Position = br.BaseStream.Position;
                    
                    switch (currentRow.Type) {
                        case 0:
                        case 1:
                            currentRow.Uint8 = br.ReadByte();
                            
                            break;
                        
                        case 2:
                        case 3:
                            currentRow.Uint16 = br.ReadUInt16();
                            
                            break;
                        
                        case 4:
                        case 5:
                            currentRow.Uint32 = br.ReadUInt32();
                            
                            break;
                        
                        case 6:
                        case 7:
                            currentRow.Uint64 = br.ReadUInt64();
                            
                            break;
                        
                        case 8:
                            currentRow.Ufloat = br.ReadSingle();
                            
                            break;
                        
                        case 0xA:
                            currentRow.Str = Tools.ReadCString(br, -1, br.ReadInt32() + StringsOffset);
                            
                            break;
                        
                        case 0xB:
                            var position = br.ReadInt32() + DataOffset;
                            currentRow.Position = position;
                            currentRow.Data = Tools.GetData(br, position, br.ReadInt32());
                            
                            break;
                        
                        default:
                            throw new NotImplementedException();
                    }
                    
                    currentEntry.Rows.Add(currentRow);
                }
                
                Rows.Add(currentEntry);
            }
            
            return true;
        }
        
        public int TableSize { get; set; }
        
        public long RowsOffset { get; set; }
        public long StringsOffset { get; set; }
        public long DataOffset { get; set; }
        public int TableName { get; set; }
        public short NumColumns { get; set; }
        public short RowLength { get; set; }
        public int NumRows { get; set; }
    }
    
    public class COLUMN {
        public COLUMN() { }
        
        public byte Flags { get; set; }
        public string Name { get; set; }
    }
    
    public class ROWS {
        public readonly List<ROW> Rows = [];
    }
    
    public class ROW {
        public int Type { get; set; } = -1;
        
        public object GetValue() {
            return Type switch {
                0 or 1 => Uint8,
                2 or 3 => Uint16,
                4 or 5 => Uint32,
                6 or 7 => Uint64,
                8 => Ufloat,
                0xA => Str,
                0xB => Data,
                _ => null
            };
        }
        
        public new Type GetType() {
            return Type switch {
                0 or 1 => Uint8.GetType(),
                2 or 3 => Uint16.GetType(),
                4 or 5 => Uint32.GetType(),
                6 or 7 => Uint64.GetType(),
                8 => Ufloat.GetType(),
                0xA => Str.GetType(),
                0xB => Data.GetType(),
                _ => null
            };
        }
        
        //column based datatypes
        public byte Uint8 { get; set; }
        public ushort Uint16 { get; set; }
        public uint Uint32 { get; set; }
        public ulong Uint64 { get; set; }
        public float Ufloat { get; set; }
        public string Str { get; set; }
        public byte[] Data { get; set; }
        public long Position { get; set; }
    }
    
    public class FileEntry {
        public object DirName { get; set; } // string
        public object FileName { get; set; } // string
        
        public object FileSize { get; set; }
        public long FileSizePos { get; set; }
        public Type FileSizeType { get; set; }
        
        public object ExtractSize { get; set; } // int
        public long ExtractSizePos { get; set; }
        public Type ExtractSizeType { get; set; }
        
        public ulong FileOffset { get; set; }
        public long FileOffsetPos { get; set; }
        public Type FileOffsetType { get; set; }
        
        public ulong Offset { get; set; }
        public object ID { get; set; } // int
        public object UserString { get; set; } // string
        public ulong UpdateDateTime { get; set; } = 0;
        public object LocalDir { get; set; } // string
        public string TOCName { get; set; }
        
        public bool Encrypted { get; set; }
        
        public string FileType { get; set; }
    }
}
