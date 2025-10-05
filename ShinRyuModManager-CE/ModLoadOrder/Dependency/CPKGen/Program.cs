using System.Diagnostics;
using Serilog;

namespace CriPakTools {
    public static class Program {
        /*private static void Main(string[] args) {
            Console.WriteLine("Yakuza CPK Repack - Based on CriPakTools\n");
            Modify(args[0], args[1], args[2]);
        }
        
        public static byte[] FileToByteArray(string fileName) {
            using var fs = File.OpenRead(fileName);
            using var binaryReader = new BinaryReader(fs);
            
            var fileData = binaryReader.ReadBytes((int)fs.Length);
            
            return fileData;
        }*/
        
        public static void Modify(string inputCpk, string replaceDir, string outputCpk) {
            GC.Collect();
            Console.WriteLine("Yakuza CPK Repack - Based on CriPakTools\n");
            
            var cpk = new CPK(new Tools());
            cpk.ReadCPK(inputCpk);
            
            GC.Collect();

            using var oldFile = new BinaryReader(new BufferedStream(File.OpenRead(inputCpk)));
            
            var files = Directory.GetFiles(replaceDir, "*.");
            var filesNames = new HashSet<string>();
            
            foreach (var str in files.Select(Path.GetFileNameWithoutExtension))
                filesNames.Add(str);
            
            var fileInfo = new FileInfo(inputCpk);
            var time = Stopwatch.StartNew();

            using var newCpk = new BinaryWriter(new BufferedStream(File.OpenWrite(outputCpk)));

            var entries = cpk.FileTable.OrderBy(x => x.FileOffset).ToList();

            foreach (var entry in entries) {
                if (entry.FileType == "CONTENT") {
                    cpk.UpdateFileEntry(entry);
                } else {
                    if (entry.FileType == "FILE" && (ulong)newCpk.BaseStream.Position < cpk.ContentOffset) {
                        var padLength = cpk.ContentOffset - (ulong)newCpk.BaseStream.Position;
                        
                        newCpk.Write(new byte[padLength], 0, (int)padLength);
                    }

                    if (entry.FileSize == null || entry.FileName == null) {
                        throw new NullReferenceException("Critical properties of the file entry are not initialized.");
                    }

                    if (!filesNames.Contains(entry.FileName.ToString())) {
                        oldFile.BaseStream.Seek((long)entry.FileOffset, SeekOrigin.Begin);
                        
                        entry.FileOffset = (ulong)newCpk.BaseStream.Position;
                        cpk.UpdateFileEntry(entry);

                        
                        var chunk = ReadBytes(newCpk.BaseStream, int.Parse(entry.FileSize.ToString()!));
                        newCpk.Write(chunk);
                    } else {
                        var newbie = File.ReadAllBytes(Path.Combine(replaceDir, entry.FileName.ToString()!));

                        entry.FileOffset = (ulong)newCpk.BaseStream.Position;
                        entry.FileSize = Convert.ChangeType(newbie.Length, entry.FileSizeType);
                        entry.ExtractSize = Convert.ChangeType(newbie.Length, entry.FileSizeType);
                        cpk.UpdateFileEntry(entry);
                        
                        newCpk.Write(newbie);
                    }

                    if ((newCpk.BaseStream.Position % 0x800) > 0) {
                        var padding = (int)(0x800 - (newCpk.BaseStream.Position % 0x800));
                        
                        newCpk.Write(new byte[padding], 0, padding);
                    }
                }
            }
            
            cpk.WriteCPK(newCpk);
            cpk.WriteITOC(newCpk);
            cpk.WriteTOC(newCpk);
            cpk.WriteETOC(newCpk);
            cpk.WriteGTOC(newCpk);
            
            Log.Information("Writing {FileName} took {TotalElapsedTime}", fileInfo.Name, time.Elapsed.TotalSeconds);
        }

        private static byte[] ReadBytes(Stream stream, int count) {
            var buffer = new byte[count];
            var bytesRead = stream.Read(buffer, 0, count);
            
            if (bytesRead != count) {
                throw new EndOfStreamException();
            }
            
            return buffer;
        }
    }
}
