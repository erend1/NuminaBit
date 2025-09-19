using NuminaBit.Services.Ciphers.Shared.Entities;
using NuminaBit.Services.Ciphers.Shared.Interfaces;

namespace NuminaBit.Services.Ciphers.Shared
{
    public class LatCalculator: ILAT
    {
        /// <summary>
        /// Compute NS table: returns int[alpha(0..63), beta(0..15)] of counts (0..64).
        /// Sbox must be 64-length array mapping z (0..63) -> 4-bit output (0..15).
        /// </summary>
        public int[,] ComputeNsTable(int[] sbox)
        {
            if (sbox == null || sbox.Length != 64) throw new ArgumentException("sbox length must be 64");
            var ns = new int[64, 16];

            for (int a = 0; a < 64; a++)
            {
                for (int b = 0; b < 16; b++)
                {
                    int cnt = 0;
                    for (int z = 0; z < 64; z++)
                    {
                        int alphaDot = PopParity(a & z);
                        int s = sbox[z] & 0xF;
                        int betaDot = PopParity(b & s);
                        if (alphaDot == betaDot) cnt++;
                    }
                    ns[a, b] = cnt;
                }
            }
            return ns;
        }

        /// <summary>
        /// Get detail rows for a single (alpha,beta) cell: list of 64 rows describing z,S(z),alpha·z,beta·S(z),isMatch.
        /// </summary>
        public List<CellRow> GetCellRows(int[] sbox, int alpha, int beta)
        {
            var list = new List<CellRow>(64);
            for (int z = 0; z < 64; z++)
            {
                int alphaDot = PopParity(alpha & z);
                int s = sbox[z] & 0xF;
                int betaDot = PopParity(beta & s);
                bool match = alphaDot == betaDot;
                list.Add(new CellRow { Z = z, AlphaDot = alphaDot, SOut = s, BetaDot = betaDot, IsMatch = match });
            }
            return list;
        }

        // helper: parity (XOR of bits) of x (returns 0 or 1)
        private static int PopParity(int x)
        {
            // builtin popcount in .NET 7+ ? Use simple
            x ^= x >> 16;
            x ^= x >> 8;
            x ^= x >> 4;
            x &= 0xF;
            // parity table for nibble
            int[] P = { 0, 1, 1, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0, 1, 1, 0 };
            return P[x];
            // Alternatively use System.Numerics.BitOperations.PopCount and &1 in modern runtimes
        }
    }
}
