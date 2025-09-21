using NuminaBit.Services.Ciphers.DES.Entities;
using NuminaBit.Services.Ciphers.DES.Constants;
using NuminaBit.Services.Ciphers.DES.Interfaces;

namespace NuminaBit.Services.Ciphers.DES
{
    public sealed class Core: ICore
    {
        private readonly static Permutations _permutations = new()
        {
            Initial = Tables.FP,
            Final = Tables.IP,

            PC1 = Tables.PC1,
            PC2 = Tables.PC2,

            P = Tables.P_INV,
            InvP = Tables.P
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

        public ulong EncryptCustom(ulong plain, KeySchedule ks, 
            int rounds = 3, bool withIP = false, bool withFP = false)
        {
            // IP
            ulong ip = withIP ? Permute(plain, Permutations.Initial, 64) : plain;

            //var aa = ToBinaryString(ip, 64);

            uint L = (uint)(ip >> 32);
            uint R = (uint)(ip & 0xFFFFFFFF);
            //var a = ToBinaryString(L, 32);
            //var b = ToBinaryString(R, 32);

            for (int r = 0; r < rounds; r++)
            {
                ulong sub = ks.SubKeys[r];
                //var a = ToBinaryString(sub, 48);
                ulong ER = Permute2(R, Expansion.Mapping, 32, 48);
                //var b = ToBinaryString(ER, 48);
                ulong EX = ER ^ sub;
                //var c = ToBinaryString(EX, 48);
                uint sOut = SBoxSub(EX);
                //var d = ToBinaryString(sOut, 32);
                uint pOut = (uint)Permute2(sOut, Permutations.P, 32);
                //var e = ToBinaryString(pOut, 32);
                uint newL = R;
                uint newR = L ^ pOut;
                L = newL;
                R = newR;
            }

            //var c = ToBinaryString(L, 32);
            //var d = ToBinaryString(R, 32);

            //ulong preOut = ((ulong)R << 32) | L;

            ulong preOut = ((ulong)L << 32) | R;


            //var e = ToBinaryString(preOut, 64);

            return withFP ? Permute(preOut, Permutations.Final, 64) : preOut;
        }

        private static string ToBinaryString(ulong v, int width)
        {
            var sb = new System.Text.StringBuilder(width);
            for (int i = 0; i < width; i++)
            {
                int bit = (int)((v >> (width - 1 - i)) & 1UL);
                sb.Append(bit == 1 ? '1' : '0');
                if ((i + 1) % 8 == 0 && i < width - 1) sb.Append(' ');
            }
            return sb.ToString();
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
            //var a = ToBinaryString(key64, 64);
            // Parity dahil 64-bit anahtar -> PC1 ile 56-bit (C,D)
            ulong pc1 = Permute(key64, Permutations.PC1, 64); // 56-bit packed MSB-first
            uint C = (uint)((pc1 >> 28) & 0x0FFFFFFF);
            uint D = (uint)(pc1 & 0x0FFFFFFF);

            //var b = ToBinaryString(C, 28);
            //var c = ToBinaryString(D, 28);

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
                //var e = ToBinaryString(sub, 48);
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

        public ulong Permute2(ulong val, int[] table, int inWidth, int outWidth = -1)
        {
            if (outWidth < 0)
                outWidth = table.Length;

            //var a = ToBinaryString(val, table.Length);

            ulong res = 0;
            for (int i = 0; i < table.Length; i++)
            {
                // 1. Tablo değerleri 1'den başladığı için 1 çıkar.
                // 2. Bitleri sağdan sola (0-indexed) al.
                int src_pos = table[i] - 1;

                //var b = ToBinaryString((ulong)src_pos, 5);

                // Girdi bitini al
                ulong bit = (val >> src_pos) & 1UL;

                // Alınan biti sağdan sola doğru, döngü sırasına göre yerleştir
                res |= bit << i;

                //var c = ToBinaryString(res, 32);
            }
            //var d = ToBinaryString(res, 32);
            return res;
        }

        public ulong Permute(uint val, int[] table, int inWidth, int outWidth = -1)
            => Permute2((ulong)val, table, inWidth, outWidth);

        private static uint LeftShift28(uint v, int s)
        {
            v &= 0x0FFFFFFF;
            return (uint)(((v << s) | (v >> (28 - s))) & 0x0FFFFFFF);
        }

        private uint SBoxSub(ulong x48)
        {
            uint out32 = 0;
            //var a = ToBinaryString(x48, 48);
            for (int box = 0; box < 8; box++)
            {
                int shift = (7 - box) * 6;
                int six = (int)((x48 >> shift) & 0x3F);
                //var b = ToBinaryString((ulong)six, 6);

                //int row = ((six & 0x20) >> 4) | (six & 0x01); // b1b6
                //int col = (six >> 1) & 0x0F;                 // b2..b5

                int row = six / 16;
                int col = six % 16;

                int s = Substitutions[box][row, col];
                //var c = ToBinaryString((ulong)s, 4);

                //out32 |= (uint)(s & 0xF) << ((7 - box) * 4);
                // 4-bit'lik çıktıyı doğru yere yerleştir
                // S1 çıktısı (box=0) en sola (28 bit kaydırarak) yerleşir.
                int shift_out = (7 - box) * 4;
                out32 |= (uint)(s & 0xF) << shift_out;
            }
            //var d = ToBinaryString((ulong)out32, 32);

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
