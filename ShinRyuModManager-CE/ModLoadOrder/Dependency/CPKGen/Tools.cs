using System.Text;

namespace CriPakTools {
    public class Tools {
        public static string ReadCString(BinaryReader br, int maxLength = -1, long lOffset = -1, Encoding enc = null) {
            var max = maxLength == -1 ? 255 : maxLength;
            var fTemp = br.BaseStream.Position;
            var i = 0;
            
            string result;
            
            if (lOffset > -1) {
                br.BaseStream.Seek(lOffset, SeekOrigin.Begin);
            }
            
            do {
                var bTemp = br.ReadByte();
                
                if (bTemp == 0)
                    break;
                
                i += 1;
            } while (i < max);
            
            if (maxLength == -1)
                max = i + 1;
            else
                max = maxLength;
            
            if (lOffset > -1) {
                br.BaseStream.Seek(lOffset, SeekOrigin.Begin);
                
                result = enc == null
                    ? Encoding.GetEncoding("SJIS").GetString(br.ReadBytes(i))
                    : enc.GetString(br.ReadBytes(i));
                
                br.BaseStream.Seek(fTemp, SeekOrigin.Begin);
            } else {
                br.BaseStream.Seek(fTemp, SeekOrigin.Begin);
                
                result = enc == null
                    ? Encoding.GetEncoding("SJIS").GetString(br.ReadBytes(i))
                    : enc.GetString(br.ReadBytes(i));
                
                br.BaseStream.Seek(fTemp + max, SeekOrigin.Begin);
            }
            
            return result;
        }
        
        public static void DeleteFileIfExists(string sPath) {
            if (File.Exists(sPath))
                File.Delete(sPath);
        }
        
        public static string GetPath(string input) {
            return Path.GetDirectoryName(input) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(input);
        }
        
        public static byte[] GetData(BinaryReader br, long offset, int size) {
            var backup = br.BaseStream.Position;
            
            br.BaseStream.Seek(offset, SeekOrigin.Begin);
            
            var result = br.ReadBytes(size);
            
            br.BaseStream.Seek(backup, SeekOrigin.Begin);
            
            return result;
        }
    }
}
