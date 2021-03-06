using System;
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Impostor.Shared.Innersloth
{
    public static class GameCode
    {
        private const string V2 = "QWXRTYLPESDFGHUJKZOCVBINMA";
        private static readonly int[] V2Map = {
            25,
            21,
            19,
            10,
            8,
            11,
            12,
            13,
            22,
            15,
            16,
            6,
            24,
            23,
            18,
            7,
            0,
            3,
            9,
            4,
            14,
            20,
            1,
            2,
            5,
            17
        };
        private static readonly RNGCryptoServiceProvider Random = new RNGCryptoServiceProvider();
        
        public static string IntToGameName(int input)
        {
            // V2.
            if (input < -1)
            {
                return IntToGameNameV2(input);
            }

            // V1.
            Span<byte> code = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(code, input);
#if NET452
            return Encoding.UTF8.GetString(code.Slice(0, 4).ToArray());
#else
            return Encoding.UTF8.GetString(code.Slice(0, 4));
#endif
        }

        private static string IntToGameNameV2(int input)
        {
            var a = input & 0x3FF;
            var b = (input >> 10) & 0xFFFFF;

            return new string(new []
            {
                V2[a % 26],
                V2[a / 26],
                V2[b % 26],
                V2[b / 26 % 26],
                V2[b / (26 * 26) % 26],
                V2[b / (26 * 26 * 26) % 26]
            });
        }

        public static int GameNameToInt(string code)
        {
            var upper = code.ToUpperInvariant();
            if (upper.Any(x => !char.IsLetter(x)))
            {
                return -1;
            }
            
            var len = code.Length;
            if (len == 6)
            {
                return GameNameToIntV2(upper);
            }

            if (len == 4)
            {
                return code[0] | ((code[1] | ((code[2] | (code[3] << 8)) << 8)) << 8);
            }
            
            return -1;
        }
        
        private static int GameNameToIntV2(string code)
        {
            var a = V2Map[code[0] - 65];
            var b = V2Map[code[1] - 65];
            var c = V2Map[code[2] - 65];
            var d = V2Map[code[3] - 65];
            var e = V2Map[code[4] - 65];
            var f = V2Map[code[5] - 65];

            var one = (a + 26 * b) & 0x3FF;
            var two = (c + 26 * (d + 26 * (e + 26 * f)));

            return (int) (one | ((two << 10) & 0x3FFFFC00) | 0x80000000);
        }

        public static int GenerateCode(int len)
        {
            if (len != 4 && len != 6)
            {
                throw new ArgumentException("should be 4 or 6", nameof(len));
            }
            
            // Generate random bytes.
#if NET452
            var data = new byte[len];
#else
            Span<byte> data = stackalloc byte[len];
#endif
            Random.GetBytes(data);
            
            // Convert to their char representation.
            Span<char> dataChar = stackalloc char[len];
            for (var i = 0; i < len; i++)
            {
                dataChar[i] = V2[V2Map[data[i] % 26]];
            }

#if NET452
            return GameNameToInt(new string(dataChar.ToArray()));
#else
            return GameNameToInt(new string(dataChar));
#endif
        }
    }
}