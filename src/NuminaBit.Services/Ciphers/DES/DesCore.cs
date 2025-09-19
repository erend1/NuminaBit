using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuminaBit.Services.Ciphers.DES
{
    public sealed class DesCore
    {
        public DesRun EncryptWithSnapshots(ulong plain, ulong key64)
        {
            var run = new DesRun();

            // IP
            ulong ip = Permute(plain, Tables.IP, 64);
            run.IPOut = ip;
            uint L = (uint)(ip >> 32);
            uint R = (uint)(ip & 0xFFFFFFFF);

            // Key schedule
            var sched = BuildKeySchedule(key64);
            run.KeySchedule = sched;

            run.Rounds = new List<RoundSnap>(capacity: 17);
            run.Rounds.Add(new RoundSnap { L = L, R = R }); // round 0

            for (int r = 1; r <= 16; r++)
            {
                var sub = sched.SubKeys[r - 1];
                ulong ER = Permute(R, Tables.E, 32, 48);
                ulong EX = ER ^ sub; // 48-bit
                uint sOut = SBoxSub(EX);
                uint pOut = (uint)Permute(sOut, Tables.P, 32);
                uint newL = R;
                uint newR = L ^ pOut;

                run.Rounds.Add(new RoundSnap
                {
                    L = newL,
                    R = newR,
                    ER = ER,
                    EXorK = EX,
                    SBoxOut = sOut,
                    PermOut = pOut,
                    LXorF = newR
                });

                L = newL;
                R = newR;
            }

            // FP
            ulong preOut = ((ulong)R << 32) | L; // swap
            run.FPIn = preOut;
            ulong C = Permute(preOut, Tables.FP, 64);
            run.FinalCipher = C;
            return run;
        }

        public static KeySchedule BuildKeySchedule(ulong key64)
        {
            // Parity dahil 64-bit anahtar -> PC1 ile 56-bit (C,D)
            ulong pc1 = Permute(key64, Tables.PC1, 64); // 56-bit packed MSB-first
            uint C = (uint)((pc1 >> 28) & 0x0FFFFFFF);
            uint D = (uint)(pc1 & 0x0FFFFFFF);

            var ks = new KeySchedule();
            ks.PC1Out = pc1;
            ks.SubKeys = new ulong[16];

            for (int r = 0; r < 16; r++)
            {
                int s = Tables.SHIFTS[r];
                C = LeftShift28(C, s);
                D = LeftShift28(D, s);
                ulong cd = ((ulong)C << 28) | D; // 56-bit
                ulong sub = Permute(cd, Tables.PC2, 56, 48);
                ks.SubKeys[r] = sub;
            }
            return ks;
        }

        static uint LeftShift28(uint v, int s)
        {
            v &= 0x0FFFFFFF;
            return (uint)(((v << s) | (v >> (28 - s))) & 0x0FFFFFFF);
        }

        public static ulong Permute(ulong val, int[] table, int inWidth, int outWidth = -1)
        {
            if (outWidth < 0) outWidth = table.Length;
            ulong res = 0;
            for (int i = 0; i < table.Length; i++)
            {
                int src = inWidth - table[i]; // tablolar 1‑indexed MSB bazlı
                ulong bit = (val >> src) & 1UL;
                res |= bit << (outWidth - 1 - i);
            }
            return res;
        }

        public static ulong Permute(uint val, int[] table, int inWidth, int outWidth = -1)
            => Permute((ulong)val, table, inWidth, outWidth);

        static uint SBoxSub(ulong x48)
        {
            uint out32 = 0;
            for (int box = 0; box < 8; box++)
            {
                int shift = (7 - box) * 6;
                int six = (int)((x48 >> shift) & 0x3F);
                int row = ((six & 0x20) >> 4) | (six & 0x01); // b1b6
                int col = (six >> 1) & 0x0F;                 // b2..b5
                int s = Tables.SBOX[box, row, col];
                out32 |= (uint)(s & 0xF) << ((7 - box) * 4);
            }
            return out32;
        }
    }

    public static class HexUtil
    {
        static readonly Random Rng = new Random();
        public static bool TryParse64(string hex, out ulong value)
        {
            value = 0;
            hex = hex?.Trim()?.Replace(" ", "") ?? string.Empty;
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
            if (hex.Length != 16) return false;
            return ulong.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out value);
        }
        public static string ToHex64(ulong v) => v.ToString("X16");
        public static string Random64Hex()
        {
            Span<byte> b = stackalloc byte[8];
            Rng.NextBytes(b);
            ulong v = BitConverter.ToUInt64(b);
            return ToHex64(v);
        }
    }
}
