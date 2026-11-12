using System;
using System.Collections.Generic;
using System.Text;

namespace PixelPlay.Foundation.QrCodes
{
    /// <summary>Encodes UTF-8 text as a QR code using byte mode and low error correction.</summary>
    /// <remarks>Supports QR versions 1-L through 5-L, up to 106 ASCII bytes.</remarks>
    public sealed class QrCodeEncoder
    {
        private static readonly int[] DataCodewordsByVersion = { 19, 34, 55, 80, 108 };
        private static readonly int[] ErrorCodewordsByVersion = { 7, 10, 15, 20, 26 };
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        /// <summary>Encodes text into a standards-compliant QR matrix.</summary>
        public QrCodeMatrix EncodeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("QR content must not be null or empty.", nameof(text));

            byte[] payload;
            try
            {
                payload = StrictUtf8.GetBytes(text);
            }
            catch (EncoderFallbackException exception)
            {
                throw new ArgumentException("QR content contains invalid Unicode text.", nameof(text), exception);
            }

            bool needsUtf8Eci = false;
            for (int i = 0; i < payload.Length; i++)
            {
                if (payload[i] > 0x7f)
                {
                    needsUtf8Eci = true;
                    break;
                }
            }

            int version = FindVersion(payload.Length, needsUtf8Eci);
            if (version == 0)
                throw new ArgumentException("QR content exceeds this encoder's capacity (up to 106 ASCII bytes).", nameof(text));

            byte[] codewords = CreateCodewords(payload, version, needsUtf8Eci);
            return BuildMatrix(codewords, version);
        }

        private static int FindVersion(int byteCount, bool needsUtf8Eci)
        {
            int headerBits = 4 + 8 + (needsUtf8Eci ? 12 : 0);
            for (int version = 1; version <= DataCodewordsByVersion.Length; version++)
            {
                int capacityBits = DataCodewordsByVersion[version - 1] * 8;
                if (headerBits + byteCount * 8 <= capacityBits)
                    return version;
            }

            return 0;
        }

        private static byte[] CreateCodewords(byte[] payload, int version, bool needsUtf8Eci)
        {
            int dataCodewordCount = DataCodewordsByVersion[version - 1];
            var bits = new List<bool>(dataCodewordCount * 8);

            if (needsUtf8Eci)
            {
                AppendBits(bits, 0x7, 4);  // ECI mode
                AppendBits(bits, 26, 8);   // UTF-8 assignment number
            }

            AppendBits(bits, 0x4, 4); // Byte mode
            AppendBits(bits, payload.Length, 8);
            for (int i = 0; i < payload.Length; i++)
                AppendBits(bits, payload[i], 8);

            int capacityBits = dataCodewordCount * 8;
            int terminatorBits = Math.Min(4, capacityBits - bits.Count);
            AppendBits(bits, 0, terminatorBits);
            while (bits.Count % 8 != 0)
                bits.Add(false);

            var data = new byte[dataCodewordCount];
            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i])
                    data[i / 8] |= (byte)(1 << (7 - (i % 8)));
            }

            int nextPad = 0;
            for (int i = bits.Count / 8; i < data.Length; i++)
            {
                data[i] = nextPad == 0 ? (byte)0xec : (byte)0x11;
                nextPad ^= 1;
            }

            byte[] errorCorrection = ComputeReedSolomonRemainder(data, ErrorCodewordsByVersion[version - 1]);
            var result = new byte[data.Length + errorCorrection.Length];
            Array.Copy(data, 0, result, 0, data.Length);
            Array.Copy(errorCorrection, 0, result, data.Length, errorCorrection.Length);
            return result;
        }

        private static void AppendBits(List<bool> bits, int value, int length)
        {
            for (int i = length - 1; i >= 0; i--)
                bits.Add(((value >> i) & 1) != 0);
        }

        private static byte[] ComputeReedSolomonRemainder(byte[] data, int degree)
        {
            byte[] generator = BuildReedSolomonGenerator(degree);
            var remainder = new byte[degree];
            for (int i = 0; i < data.Length; i++)
            {
                byte factor = (byte)(data[i] ^ remainder[0]);
                for (int j = 0; j < degree - 1; j++)
                    remainder[j] = remainder[j + 1];
                remainder[degree - 1] = 0;

                for (int j = 0; j < degree; j++)
                    remainder[j] ^= Multiply(generator[j + 1], factor);
            }

            return remainder;
        }

        private static byte[] BuildReedSolomonGenerator(int degree)
        {
            var generator = new byte[] { 1 };
            byte root = 1;
            for (int i = 0; i < degree; i++)
            {
                var next = new byte[generator.Length + 1];
                for (int j = 0; j < generator.Length; j++)
                {
                    next[j] ^= generator[j];
                    next[j + 1] ^= Multiply(generator[j], root);
                }

                generator = next;
                root = Multiply(root, 2);
            }

            return generator;
        }

        private static byte Multiply(byte left, byte right)
        {
            int product = 0;
            int a = left;
            int b = right;
            while (b != 0)
            {
                if ((b & 1) != 0)
                    product ^= a;
                a <<= 1;
                if ((a & 0x100) != 0)
                    a ^= 0x11d;
                b >>= 1;
            }

            return (byte)product;
        }

        private static QrCodeMatrix BuildMatrix(byte[] codewords, int version)
        {
            int size = version * 4 + 17;
            var functionModules = new bool[size, size];
            var baseModules = new bool[size, size];
            DrawFunctionPatterns(baseModules, functionModules, version);

            bool[,] bestModules = null;
            int bestPenalty = int.MaxValue;
            for (int mask = 0; mask < 8; mask++)
            {
                bool[,] candidate = (bool[,])baseModules.Clone();
                DrawCodewords(candidate, functionModules, codewords, mask);
                DrawFormatBits(candidate, functionModules, mask);
                int penalty = CalculatePenalty(candidate);
                if (penalty < bestPenalty)
                {
                    bestPenalty = penalty;
                    bestModules = candidate;
                }
            }

            return new QrCodeMatrix(bestModules, version);
        }

        private static void DrawFunctionPatterns(bool[,] modules, bool[,] functionModules, int version)
        {
            int size = modules.GetLength(0);
            DrawFinderPattern(modules, functionModules, 3, 3);
            DrawFinderPattern(modules, functionModules, size - 4, 3);
            DrawFinderPattern(modules, functionModules, 3, size - 4);

            if (version >= 2)
                DrawAlignmentPattern(modules, functionModules, size - 7, size - 7);

            for (int i = 0; i < size; i++)
            {
                if (!functionModules[6, i])
                    SetFunctionModule(modules, functionModules, 6, i, i % 2 == 0);
                if (!functionModules[i, 6])
                    SetFunctionModule(modules, functionModules, i, 6, i % 2 == 0);
            }

            DrawFormatBits(modules, functionModules, 0);
            SetFunctionModule(modules, functionModules, 8, size - 8, true);
        }

        private static void DrawFinderPattern(bool[,] modules, bool[,] functionModules, int centerX, int centerY)
        {
            int size = modules.GetLength(0);
            for (int dy = -4; dy <= 4; dy++)
            {
                for (int dx = -4; dx <= 4; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    if (x < 0 || x >= size || y < 0 || y >= size)
                        continue;

                    int distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    SetFunctionModule(modules, functionModules, x, y, distance != 2 && distance != 4);
                }
            }
        }

        private static void DrawAlignmentPattern(bool[,] modules, bool[,] functionModules, int centerX, int centerY)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    int distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    SetFunctionModule(modules, functionModules, centerX + dx, centerY + dy, distance != 1);
                }
            }
        }

        private static void DrawFormatBits(bool[,] modules, bool[,] functionModules, int mask)
        {
            int data = (1 << 3) | mask; // Error correction level L, then mask index.
            int remainder = data;
            for (int i = 0; i < 10; i++)
                remainder = (remainder << 1) ^ (((remainder >> 9) & 1) != 0 ? 0x537 : 0);
            int bits = ((data << 10) | remainder) ^ 0x5412;
            int size = modules.GetLength(0);

            for (int i = 0; i <= 5; i++)
                SetFunctionModule(modules, functionModules, 8, i, GetBit(bits, i));
            SetFunctionModule(modules, functionModules, 8, 7, GetBit(bits, 6));
            SetFunctionModule(modules, functionModules, 8, 8, GetBit(bits, 7));
            SetFunctionModule(modules, functionModules, 7, 8, GetBit(bits, 8));
            for (int i = 9; i < 15; i++)
                SetFunctionModule(modules, functionModules, 14 - i, 8, GetBit(bits, i));

            for (int i = 0; i < 8; i++)
                SetFunctionModule(modules, functionModules, size - 1 - i, 8, GetBit(bits, i));
            for (int i = 8; i < 15; i++)
                SetFunctionModule(modules, functionModules, 8, size - 15 + i, GetBit(bits, i));
        }

        private static void DrawCodewords(bool[,] modules, bool[,] functionModules, byte[] codewords, int mask)
        {
            int size = modules.GetLength(0);
            int bitIndex = 0;
            for (int right = size - 1; right >= 1; right -= 2)
            {
                if (right == 6)
                    right = 5;

                for (int vertical = 0; vertical < size; vertical++)
                {
                    bool upward = ((right + 1) & 2) == 0;
                    int y = upward ? size - 1 - vertical : vertical;
                    for (int offset = 0; offset < 2; offset++)
                    {
                        int x = right - offset;
                        if (functionModules[x, y])
                            continue;

                        bool value = bitIndex < codewords.Length * 8 && GetBit(codewords[bitIndex >> 3], 7 - (bitIndex & 7));
                        modules[x, y] = value ^ IsMasked(mask, x, y);
                        bitIndex++;
                    }
                }
            }
        }

        private static bool IsMasked(int mask, int x, int y)
        {
            switch (mask)
            {
                case 0: return (x + y) % 2 == 0;
                case 1: return y % 2 == 0;
                case 2: return x % 3 == 0;
                case 3: return (x + y) % 3 == 0;
                case 4: return (y / 2 + x / 3) % 2 == 0;
                case 5: return x * y % 2 + x * y % 3 == 0;
                case 6: return (x * y % 2 + x * y % 3) % 2 == 0;
                case 7: return ((x + y) % 2 + x * y % 3) % 2 == 0;
                default: throw new ArgumentOutOfRangeException(nameof(mask));
            }
        }

        private static int CalculatePenalty(bool[,] modules)
        {
            int size = modules.GetLength(0);
            int penalty = 0;

            for (int y = 0; y < size; y++)
                penalty += ScoreLine(size, x => modules[x, y]);
            for (int x = 0; x < size; x++)
                penalty += ScoreLine(size, y => modules[x, y]);

            for (int y = 0; y < size - 1; y++)
            {
                for (int x = 0; x < size - 1; x++)
                {
                    bool color = modules[x, y];
                    if (modules[x + 1, y] == color && modules[x, y + 1] == color && modules[x + 1, y + 1] == color)
                        penalty += 3;
                }
            }

            int darkCount = 0;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (modules[x, y]) darkCount++;
            penalty += Math.Abs(darkCount * 20 - size * size * 10) / (size * size) * 10;
            return penalty;
        }

        private static int ScoreLine(int size, Func<int, bool> getModule)
        {
            int score = 0;
            int runLength = 1;
            bool previous = getModule(0);
            for (int i = 1; i < size; i++)
            {
                bool current = getModule(i);
                if (current == previous)
                {
                    runLength++;
                    if (runLength == 5)
                        score += 3;
                    else if (runLength > 5)
                        score++;
                }
                else
                {
                    runLength = 1;
                    previous = current;
                }
            }

            for (int i = 0; i <= size - 7; i++)
            {
                if (!getModule(i) || getModule(i + 1) || !getModule(i + 2) ||
                    !getModule(i + 3) || !getModule(i + 4) || getModule(i + 5) || !getModule(i + 6))
                    continue;

                if (HasFourLightModulesBefore(i, getModule))
                    score += 40;
                if (HasFourLightModulesAfter(i + 7, size, getModule))
                    score += 40;
            }

            return score;
        }

        private static bool HasFourLightModulesBefore(int start, Func<int, bool> getModule)
        {
            if (start < 4)
                return false;
            for (int i = start - 4; i < start; i++)
                if (getModule(i)) return false;
            return true;
        }

        private static bool HasFourLightModulesAfter(int start, int size, Func<int, bool> getModule)
        {
            if (start + 4 > size)
                return false;
            for (int i = start; i < start + 4; i++)
                if (getModule(i)) return false;
            return true;
        }

        private static void SetFunctionModule(bool[,] modules, bool[,] functionModules, int x, int y, bool value)
        {
            modules[x, y] = value;
            functionModules[x, y] = true;
        }

        private static bool GetBit(int value, int index) => ((value >> index) & 1) != 0;
    }
}
