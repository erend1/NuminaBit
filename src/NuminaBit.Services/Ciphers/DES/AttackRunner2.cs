using System.Security.Cryptography;
using NuminaBit.Services.Ciphers.DES.Entities;
using NuminaBit.Services.Ciphers.DES.Interfaces;

namespace NuminaBit.Services.Ciphers.DES
{
    public class AttackRunner2(ICore des): IAttackRunner2
    {
        private readonly ICore _des = des;
        private static readonly int[] bitPositions = [7, 18, 24, 29];
        private static readonly ulong _hiddenKey = 0x133457799BBCDFF1UL; // static hidden key

        public ulong HiddenKey => _hiddenKey;

        /// <summary>
        /// Run a single Algorithm-1 experiment: generate 'pairs' random plaintexts, compute lhs for each,
        /// then decide guessed bit by majority (0 if majority of lhs==0).
        /// Returns TrialOutcome with counts and actual bit (XOR of K1[22] and K3[22]).
        /// </summary>
        public Task<TrialOutcome> RunAlgorithm1SingleAsync(ulong key64, int pairs)
        {
            return Task.Run(() =>
            {
                var ks = _des.BuildKeySchedule(key64);

                int countZero = 0;
                int countOne = 0;

                for (int i = 0; i < pairs; i++)
                {
                    ulong plain = Random64();
                    //var a = ToBinaryString(plain, 64);

                    // 3-round, no IP/FP
                    ulong cipher = _des.EncryptCustom(plain, ks, rounds: 3, withIP: false, withFP: false);
                    //var b = ToBinaryString(cipher, 64);

                    uint PH = (uint)(plain >> 32);
                    //var c = ToBinaryString(PH, 32);
                    uint PL = (uint)(plain & 0xFFFFFFFF);
                    //var d = ToBinaryString(PL, 32);

                    uint CH = (uint)(cipher >> 32);
                    //var e = ToBinaryString(CH, 32);

                    uint CL = (uint)(cipher & 0xFFFFFFFF);
                    //var f = ToBinaryString(CL, 32);

                    //var g = GetBit(PL, 15);
                    //var h = GetBit(CL, 15);

                    int lhs = ExtractBits(PH, bitPositions)
                            ^ ExtractBits(CH, bitPositions)
                            ^ GetBit(PL, 15)
                            ^ GetBit(CL, 15);

                    if (lhs == 0) countZero++; else countOne++;
                }

                // guessed bit: 0 if countZero > countOne, else 1 (tie -> decide 1)
                int guessed = (countZero > countOne) ? 0 : 1;

                // compute actual target bit = K1[22] xor K3[22]
                int actual = GetSubkeyBitXor(ks, 0, 2, 22); // k1 index=0, k3 index=2, pos=22

                var outcome = new TrialOutcome
                {
                    Pairs = pairs,
                    CountZero = countZero,
                    CountOne = countOne,
                    GuessedBit = guessed,
                    ActualBit = actual,
                    Success = (guessed == actual)
                };
                return outcome;
            });
        }

        /// <summary>
        /// Run multiple independent trials and return their outcomes.
        /// </summary>
        public async Task<List<TrialOutcome>> RunAlgorithm1MultipleAsync(ulong key64, int pairsPerTrial, int trials)
        {
            var list = new List<TrialOutcome>(trials);
            for (int t = 0; t < trials; t++)
            {
                var outc = await RunAlgorithm1SingleAsync(key64, pairsPerTrial);
                list.Add(outc);
            }
            return list;
        }

        // helper: compute XOR of the bit 'pos' (1..48) in subkey round a and round b
        private static int GetSubkeyBitXor(KeySchedule ks, int roundAIndexZeroBased, int roundBIndexZeroBased, int pos1based)
        {
            // ks.SubKeys elements are 48-bit stored in ulong
            ulong kA = ks.SubKeys[roundAIndexZeroBased];
            ulong kB = ks.SubKeys[roundBIndexZeroBased];
            int bitA = (int)((kA >> pos1based) & 1UL);
            int bitB = (int)((kB >> pos1based) & 1UL);
            return (bitA ^ bitB);
        }

        // ---------- utility methods ----------
        private static ulong Random64()
        {
            var buf = RandomNumberGenerator.GetBytes(8);
            return BitConverter.ToUInt64(buf, 0);
        }

        private static int ExtractBits(uint val, int[] positions)
        {
            int x = 0;
            foreach (var pos in positions)
                x ^= GetBit(val, pos);
            return x;
        }

        private static int GetBit2(uint val, int pos)
        {
            // pos: 1..32 MSB-first
            return (int)((val >> (32 - pos)) & 1u);
        }

        private static int GetBit(uint val, int pos)
        {
            // pos: 0..31 LSB-first
            return (int)((val >> pos) & 1UL);
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
    }
}
