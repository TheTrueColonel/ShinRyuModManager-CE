﻿using System.Security.Cryptography;

namespace Utils;

public class GameHash {
    public static bool ValidateFile(string path, Game game) {
        try {
            //Xbox doesnt like being read!
            if (GamePath.IsXbox(new FileInfo(path).Directory.FullName)) {
                return true;
            }
            
            using var md5Hash = MD5.Create();
            using var file = File.OpenRead(path);
            var gameHash = GetGameHash(game);
            
            var computedHash = md5Hash.ComputeHash(file);
            
            return gameHash.Count <= 0 || gameHash.Any(arr => arr.SequenceEqual(computedHash));
        } catch {
            return true;
        }
    }
    
    private static List<byte[]> GetGameHash(Game game) {
        return game switch {
            Game.Yakuza0 => [
                new byte[] { 168, 70, 120, 237, 170, 16, 229, 118, 232, 54, 167, 130, 194, 37, 220, 14 }, // Steam ver.
                new byte[] { 32, 44, 24, 38, 67, 27, 82, 26, 205, 131, 3, 24, 44, 150, 150, 84 }          // GOG ver.
            ],
            Game.YakuzaKiwami => [
                new byte[] { 142, 39, 38, 133, 251, 26, 47, 181, 222, 56, 98, 207, 178, 123, 175, 8 },    // Steam ver.
                new byte[] { 114, 65, 77, 21, 216, 176, 138, 129, 56, 13, 182, 66, 10, 202, 126, 150 }    // GOG ver.
            ],
            Game.YakuzaKiwami2 => [
                new byte[] { 143, 2, 192, 39, 60, 179, 172, 44, 242, 201, 155, 226, 50, 192, 204, 0 },    // Steam ver.
                new byte[] { 193, 175, 140, 27, 230, 27, 94, 96, 67, 221, 175, 168, 32, 228, 240, 101 }   // GOG ver.
            ],
            Game.Yakuza3 => [
                new byte[] { 172, 112, 65, 90, 116, 185, 119, 107, 139, 148, 48, 80, 40, 13, 107, 113 },  // Steam ver.
                new byte[] { 89, 69, 173, 134, 170, 154, 242, 60, 218, 44, 38, 126, 35, 19, 173, 104 }    // GOG ver.
            ],
            Game.Yakuza4 => [
                new byte[] { 41, 89, 36, 15, 180, 25, 237, 66, 222, 176, 78, 130, 33, 146, 77, 132 },     // Steam ver.
                new byte[] { 104, 219, 147, 29, 124, 200, 240, 165, 188, 22, 150, 130, 1, 28, 224, 84 }   // GOG ver.
            ],
            Game.Yakuza5 => [
                new byte[] { 51, 96, 128, 207, 98, 131, 90, 216, 213, 88, 198, 186, 60, 99, 176, 201 },   // Steam ver.
                new byte[] { 241, 225, 30, 149, 210, 236, 3, 215, 58, 238, 223, 99, 16, 195, 221, 19 }    // GOG ver.
            ],
            Game.Yakuza6 => [
                new byte[] { 176, 204, 180, 91, 160, 163, 81, 217, 243, 92, 5, 157, 214, 129, 217, 7 },   //Steam ver.
                new byte[] { 3, 74, 1, 122, 239, 147, 32, 206, 253, 233, 202, 68, 39, 125, 126, 187 }     // GOG ver.
            ],
            Game.YakuzaLikeADragon => [
                new byte[] { 188, 204, 133, 1, 251, 100, 190, 56, 10, 122, 164, 173, 244, 134, 246, 5 },  //Steam ver.
                new byte[] { 95, 8, 201, 91, 31, 191, 26, 183, 208, 76, 54, 13, 206, 163, 188, 44, }      // GOG ver.
            ],
            _ => []
        };
    }
}
