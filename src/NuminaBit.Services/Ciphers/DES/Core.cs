using NuminaBit.Services.Ciphers.DES.Entities;
using NuminaBit.Services.Ciphers.DES.Constants;
using NuminaBit.Services.Ciphers.DES.Interfaces;

namespace NuminaBit.Services.Ciphers.DES
{
    public sealed class Core: IDES
    {
        private readonly static Permutations _permutations = new()
        {
            Initial = Tables.IP,
            Final = Tables.FP,

            PC1 = Tables.PC1,
            PC2 = Tables.PC2,

            P = Tables.P,
            InvP = Tables.P_INV
        };
        public Permutations Permutations => _permutations;

        private readonly static Expansion _expansion = new()
        {
            Mapping = Tables.E,
            InverseMapping = Tables.E_INV
        };
        public Expansion Expansion => _expansion;

        private readonly static Substitutions _substitutions = new()
        {
            S1 = GetSBox(0),
            S2 = GetSBox(1),
            S3 = GetSBox(2),
            S4 = GetSBox(3),
            S5 = GetSBox(4),
            S6 = GetSBox(5),
            S7 = GetSBox(6),
            S8 = GetSBox(7)
        };
        public Substitutions Substitutions => _substitutions;

        public ulong EncryptFast(ulong plain, KeySchedule ks)
        {
            // IP
            ulong ip = Permute(plain, Permutations.Initial, 64);
            uint L = (uint)(ip >> 32);
            uint R = (uint)(ip & 0xFFFFFFFF);

            for (int r = 0; r < 16; r++)
            {
                ulong sub = ks.SubKeys[r];
                ulong ER = Permute(R, Expansion.Mapping, 32, 48);
                ulong EX = ER ^ sub;
                uint sOut = SBoxSub(EX);
                uint pOut = (uint)Permute(sOut, Permutations.P, 32);
                uint newL = R;
                uint newR = L ^ pOut;
                L = newL;
                R = newR;
            }

            ulong preOut = ((ulong)R << 32) | L;
            return Permute(preOut, Permutations.Final, 64);
        }

        public RunInfo EncryptWithSnapshots(ulong plain, ulong key64)
        {
            // IP
            ulong ip = Permute(plain, Permutations.Initial, 64);
            uint L = (uint)(ip >> 32);
            uint R = (uint)(ip & 0xFFFFFFFF);

            // Key schedule
            var sched = BuildKeySchedule(key64);

            // 16 Rounds of DES
            var rounds = new List<RoundSnap>(capacity: 17)
            {
                new() { L = L, R = R } // round 0
            };
            for (int r = 1; r <= 16; r++)
            {
                var sub = sched.SubKeys[r - 1];
                ulong ER = Permute(R, Expansion.Mapping, 32, 48);
                ulong EX = ER ^ sub; // 48-bit
                uint sOut = SBoxSub(EX);
                uint pOut = (uint)Permute(sOut, Permutations.P, 32);
                uint newL = R;
                uint newR = L ^ pOut;

                rounds.Add(new RoundSnap
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
            ulong C = Permute(preOut, Permutations.Final, 64);

            // Prepare result
            var run = new RunInfo
            {
                IPOut = ip,
                KeySchedule = sched,
                Rounds = rounds,
                FinalCipher = C,
                FPIn = preOut
            };

            return run;
        }

        public KeySchedule BuildKeySchedule(ulong key64)
        {
            // Parity dahil 64-bit anahtar -> PC1 ile 56-bit (C,D)
            ulong pc1 = Permute(key64, Permutations.PC1, 64); // 56-bit packed MSB-first
            uint C = (uint)((pc1 >> 28) & 0x0FFFFFFF);
            uint D = (uint)(pc1 & 0x0FFFFFFF);

            var ks = new KeySchedule
            {
                PC1Out = pc1,
                SubKeys = new ulong[16]
            };

            for (int r = 0; r < 16; r++)
            {
                int s = KeySchedule.PerformShift(r);
                C = LeftShift28(C, s);
                D = LeftShift28(D, s);
                ulong cd = ((ulong)C << 28) | D; // 56-bit
                ulong sub = Permute(cd, Permutations.PC2, 56, 48);
                ks.SubKeys[r] = sub;
            }
            return ks;
        }

        public ulong Permute(ulong val, int[] table, int inWidth, int outWidth = -1)
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

        public ulong Permute(uint val, int[] table, int inWidth, int outWidth = -1)
            => Permute((ulong)val, table, inWidth, outWidth);

        private static uint LeftShift28(uint v, int s)
        {
            v &= 0x0FFFFFFF;
            return (uint)(((v << s) | (v >> (28 - s))) & 0x0FFFFFFF);
        }

        private uint SBoxSub(ulong x48)
        {
            uint out32 = 0;
            for (int box = 0; box < 8; box++)
            {
                int shift = (7 - box) * 6;
                int six = (int)((x48 >> shift) & 0x3F);
                int row = ((six & 0x20) >> 4) | (six & 0x01); // b1b6
                int col = (six >> 1) & 0x0F;                 // b2..b5
                int s = Substitutions[box][row, col];
                out32 |= (uint)(s & 0xF) << ((7 - box) * 4);
            }
            return out32;
        }

        private static int[,] GetSBox(int box)
        {
            var sbox = new int[4, 16];
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 16; col++)
                    sbox[row, col] = Tables.SBOX[box, row, col];
            return sbox;
        }
    }
}
