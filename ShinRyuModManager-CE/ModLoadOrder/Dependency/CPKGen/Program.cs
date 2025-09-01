using System.Diagnostics;

namespace CriPakTools {
    public static class Program {
        /*private static void Main(string[] args) {
            Console.WriteLine("Yakuza CPK Repack - Based on CriPakTools\n");
            Modify(args[0], args[1], args[2]);
        }*/
        
        public static byte[] FileToByteArray(string fileName) {
            using var fs = File.OpenRead(fileName);
            using var binaryReader = new BinaryReader(fs);
            
            var fileData = binaryReader.ReadBytes((int)fs.Length);
            
            return fileData;
        }
        
        public static void Modify(string inputCpk, string replaceDir, string outputCpk) {
            GC.Collect();
            Console.WriteLine("Yakuza CPK Repack - Based on CriPakTools\n");
            
            var cpk = new CPK(new Tools());
            cpk.ReadCPK(inputCpk);
            
            GC.Collect();
            
            var oldFile = new BinaryReader(File.OpenRead(inputCpk));
            
            var files = Directory.GetFiles(replaceDir, "*.");
            var filesNames = new HashSet<string>();
            
            foreach (var str in files.Select(Path.GetFileNameWithoutExtension))
                filesNames.Add(str);
            
            var fi = new FileInfo(inputCpk);
            
            var time = new Stopwatch();
            time.Start();
            
            var newCpk = new BinaryWriter(new FileStream(outputCpk, FileMode.Create));
            
            var entries = cpk.FileTable.OrderBy(x => x.FileOffset).ToList();
            
            foreach (var entry in entries) {
                if (entry.FileType != "CONTENT") {
                    if (entry.FileType == "FILE") {
                        // I'm too lazy to figure out how to update the ContextOffset position so this works :)
                        if ((ulong)newCpk.BaseStream.Position < cpk.ContentOffset) {
                            var padLength = cpk.ContentOffset - (ulong)newCpk.BaseStream.Position;
                            
                            for (ulong z = 0; z < padLength; z++) {
                                newCpk.Write((byte)0);
                            }
                        }
                    }
                    
                    if (!filesNames.Contains(entry.FileName.ToString())) {
                        oldFile.BaseStream.Seek((long)entry.FileOffset, SeekOrigin.Begin);
                        
                        entry.FileOffset = (ulong)newCpk.BaseStream.Position;
                        cpk.UpdateFileEntry(entry);
                        
                        var chunk = oldFile.ReadBytes(int.Parse(entry.FileSize.ToString()!));
                        newCpk.Write(chunk);
                    } else {
                        var newbie = FileToByteArray((Path.Combine(replaceDir, entry.FileName.ToString()!)));
                        // File.ReadAllBytes(Path.Combine(replaceDir, entries[i].FileName.ToString()));
                        
                        entry.FileOffset = (ulong)newCpk.BaseStream.Position;
                        entry.FileSize = Convert.ChangeType(newbie.Length, entry.FileSizeType);
                        entry.ExtractSize = Convert.ChangeType(newbie.Length, entry.FileSizeType);
                        cpk.UpdateFileEntry(entry);
                        newCpk.Write(newbie);
                    }
                    
                    GC.Collect();
                    
                    if ((newCpk.BaseStream.Position % 0x800) <= 0)
                        continue;
                    
                    var curPos = newCpk.BaseStream.Position;
                    
                    for (var j = 0; j < (0x800 - (curPos % 0x800)); j++) {
                        newCpk.Write((byte)0);
                    }
                } else {
                    // Content is special.... just update the position
                    cpk.UpdateFileEntry(entry);
                }
            }
            
            cpk.WriteCPK(newCpk);
            cpk.WriteITOC(newCpk);
            cpk.WriteTOC(newCpk);
            cpk.WriteETOC(newCpk);
            cpk.WriteGTOC(newCpk);
            
            newCpk.Close();
            oldFile.Close();
            
            Console.WriteLine("Done in " + time.Elapsed.TotalSeconds);
        }
    }
}
