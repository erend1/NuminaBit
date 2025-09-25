using System.Security.Cryptography;
using NuminaBit.Services.Ciphers.DES.Entities;
using NuminaBit.Services.Ciphers.DES.Interfaces;

namespace NuminaBit.Services.Ciphers.DES
{
    public class SecondAlgorithmRunner(ICore core): ISecondAlgorithm
    {
        private readonly ICore _core = core;
        private static readonly int[] textBitPositions_12_16 = [12, 16];
        private static readonly int[] textBitPositions_7_18_24 = [7, 18, 24];
        private static readonly int[] textBitPositions_7_18_24_29 = [7, 18, 24, 29];

        private static readonly ulong _hiddenKey = 0x133457799BBCDFF1UL; // static hidden key

        // Hidden key for UI experiments (can be changed or moved to config)
        public ulong HiddenKey => _hiddenKey;

        // Default parameters you can tune from UI
        public int TopKPerEquation { get; set; } = 128; // number of top candidates to keep for each equation (KA, KB)
        public int MaxCombinedCandidates { get; set; } = 4096; // limit for topKa x topKb combinations to try
        public int MaxExhaustiveTrialsPerCandidate { get; set; } = 100000; // limit of attempts when searching remaining bits (for demo)

        // Public result record for a single run
        public record Algorithm2Result(
            int Pairs,
            int TopK,
            int CombinedCandidatesTried,
            bool Found,
            ulong? FoundKey64,
            int AttemptsUsed,
            List<(int key12, long score)> TopKA,
            List<(int key12, long score)> TopKB
        );

        /*
        /// <summary>
        /// Run Algorithm 2 once with the given parameters.
        /// </summary>
        public async Task<Algorithm2Result> RunSingleAsync(ulong key64, int pairs, int topKPerEq, int maxCombined, int maxExhaustive)
        {
            return await Task.Run(() =>
            {
                TopKPerEquation = topKPerEq;
                MaxCombinedCandidates = maxCombined;
                MaxExhaustiveTrialsPerCandidate = maxExhaustive;

                // Build key schedule for the given real key (for verification only)
                var realKs = _core.BuildKeySchedule(key64);

                // 1) Generate plaintext/ciphertext pairs (3..8? We need 8-round experiment)
                var ksForEncrypt = _core.BuildKeySchedule(key64); // same key used to create ciphertexts
                var pairsList = new List<(ulong P, ulong C)>(pairs);
                for (int i = 0; i < pairs; i++)
                {
                    ulong p = Random64();
                    // Use 8-round encryption, without IP/FP (we follow paper's mapping scheme)
                    ulong c = _core.EncryptCustom(p, ksForEncrypt, rounds: 8, withIP: false, withFP: false);
                    pairsList.Add((p, c));
                }

                // 2) Data counting phase: build TA and TB counters (size 2^13 = 8192)
                int TA_SIZE = 1 << 13;
                var TA = new int[TA_SIZE];
                var TB = new int[TA_SIZE];

                foreach (var (P, C) in pairsList)
                {
                    int tA = ComputeTIndexA(P, C); // 13-bit index for equation (4) mapping
                    int tB = ComputeTIndexB(P, C); // 13-bit index for equation (5) mapping
                    TA[tA]++;
                    TB[tB]++;
                }

                // 3) Key counting phase: compute KA (size 2^12) and KB (size 2^12)
                int KA_SIZE = 1 << 12;
                var KA = new long[KA_SIZE];
                var KB = new long[KA_SIZE];

                // For each 12-bit candidate k, sum appropriate TA entries where left side of eq=0
                // We need function LeftSideEq4(tIndex, kCandidate) -> bool. Equivalent for eq5.
                for (int k = 0; k < KA_SIZE; k++)
                {
                    long sumA = 0;
                    for (int t = 0; t < TA_SIZE; t++)
                    {
                        if (LeftSideEquation4IsZero(t, k)) sumA += TA[t];
                    }
                    KA[k] = sumA;
                }
                for (int k = 0; k < KA_SIZE; k++)
                {
                    long sumB = 0;
                    for (int t = 0; t < TA_SIZE; t++)
                    {
                        if (LeftSideEquation5IsZero(t, k)) sumB += TB[t];
                    }
                    KB[k] = sumB;
                }

                // 4) Sort and take top-K for each
                var topKA = KA.Select((v, idx) => (key12: idx, score: v)).OrderByDescending(x => x.score).Take(TopKPerEquation).ToList();
                var topKB = KB.Select((v, idx) => (key12: idx, score: v)).OrderByDescending(x => x.score).Take(TopKPerEquation).ToList();

                // 5) Combine top candidates to form 26-bit candidates (cartesian). Limit to maxCombined.
                var combined = new List<(int ka, int kb, long score)>(TopKPerEquation * TopKPerEquation);
                foreach (var a in topKA)
                    foreach (var b in topKB)
                        combined.Add((a.key12, b.key12, a.score + b.score));
                combined = combined.OrderByDescending(x => x.score).Take(MaxCombinedCandidates).ToList();

                bool found = false;
                ulong? foundKey = null;
                int attempts = 0;

                // 6) For each combined candidate, attempt to recover remaining key bits (brute force limited).
                foreach (var cand in combined)
                {
                    // cand defines some of the 26 bits (we need a mapping function combine->partialKeyMask)
                    // We'll create a partial key (mask, value) representation here. Implementation detail:
                    // For simplicity in prototype: treat candidate pair as "known partial subkey bits" and
                    // brute force the remaining subkeys by randomized trials up to MaxExhaustiveTrialsPerCandidate.
                    attempts++;
                    if (attempts > MaxCombinedCandidates) break;

                    // Attempt limited randomized exhaustive:
                    int trials = 0;
                    var rand = new Random();
                    while (trials < MaxExhaustiveTrialsPerCandidate)
                    {
                        trials++;
                        // Construct a full 56-bit candidate key (this is heuristic for prototype)
                        // For safety: we generate candidate key by randomizing bits and then we will check
                        // if it matches the  pairs (by encrypting with it and verifying equation).
                        ulong testerKey = RandomU64(); // random 64-bit; we will mask/adjust parity if needed
                        var testerKs = _core.BuildKeySchedule(testerKey);

                        // verify: apply the key to pairs and test whether equations match (fast test)
                        bool ok = VerifyCandidateOnPairs(testerKs, pairsList);
                        if (ok)
                        {
                            found = true;
                            foundKey = testerKey;
                            break;
                        }
                    }

                    if (found) break;
                }

                return new Algorithm2Result(
                    pairs,
                    TopKPerEquation,
                    combined.Count,
                    found,
                    foundKey,
                    attempts,
                    topKA,
                    topKB
                );
            });
        }
        */

        /// <summary>
        /// Run Algorithm 2 once with the given parameters.
        /// </summary>
        public async Task<Algorithm2Result> RunSingleAsync(ulong key64, int pairs, CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                // Build key schedule for the given real key (for verification only)
                var realKs = _core.BuildKeySchedule(key64);

                var pairsHalf = pairs / 2;

                // 1) Generate plaintext/ciphertext pairs (3..8? We need 8-round experiment)
                var ksForEncrypt = _core.BuildKeySchedule(key64); // same key used to create ciphertexts
                var pairsList = new List<(ulong P, ulong C)>(pairs);

                var realK12 = IntArrayToInt12(
                [
                    GetBit(realKs.SubKeys[0], 18),
                    GetBit(realKs.SubKeys[0], 19),
                    GetBit(realKs.SubKeys[0], 20),
                    GetBit(realKs.SubKeys[0], 21),
                    GetBit(realKs.SubKeys[0], 22),
                    GetBit(realKs.SubKeys[0], 23),
                    GetBit(realKs.SubKeys[7], 42),
                    GetBit(realKs.SubKeys[7], 43),
                    GetBit(realKs.SubKeys[7], 44),
                    GetBit(realKs.SubKeys[7], 45),
                    GetBit(realKs.SubKeys[7], 46),
                    GetBit(realKs.SubKeys[7], 47)
                ]);

                // 2) Data counting phase: build TA and TB counters (size 2^13 = 8192)
                int TA_SIZE = 1 << 13;
                int TB_SIZE = 1 << 17;
                var TA = new int[TA_SIZE];
                var TB = new int[TB_SIZE];

                for (int i = 0; i < pairs; i++)
                {
                    ulong P = Random64();
                    // Use 8-round encryption, without IP/FP (we follow paper's mapping scheme)
                    ulong C = _core.EncryptCustom(P, ksForEncrypt, rounds: 8, withIP: false, withFP: false);

                    pairsList.Add((P, C));

                    // Use positions (example selection; adapt to exact paper indices used in your other pages)
                    // We'll map onto PL (lower 32 bits of P) and CL (lower 32 bits of C)
                    uint PL = (uint)(P & 0xFFFFFFFF);
                    uint PH = (uint)(P >> 32);
                    uint CL = (uint)(C & 0xFFFFFFFF);
                    uint CH = (uint)(C >> 32);

                    int tA = ComputeTIndexA(PL, PH, CL, CH); // 13-bit index for equation (4) mapping
                    int tB = ComputeTIndexB(PL, PH, CL, CH); // 14-bit index for equation (5) mapping
                    TA[tA]++;
                    TB[tB]++;
                }

                // 3) Key counting phase: compute KA (size 2^12) and KB (size 2^12)
                int KA_SIZE = 1 << 12;
                var KA = new long[KA_SIZE];

                // For each 12-bit candidate k, sum appropriate TA entries where left side of eq=0
                // We need function LeftSideEq4(tIndex, kCandidate) -> bool. Equivalent for eq5.
                for (int k = 0; k < KA_SIZE; k++)
                {
                    long sumA = 0;
                    for (int t = 0; t < TA_SIZE; t++)
                    {
                        if (TA[t] != 0 && LeftSideEquation4IsZero(t, k)) sumA += TA[t];
                    }
                    KA[k] = sumA;
                }

                // 4) Sort and take top-K for each
                var topKA = KA.Select((v, idx) => (key12: idx, score: v)).OrderByDescending(x => x.score).Take(TopKPerEquation).ToList();

                var TAMax = KA.Max();
                var TAMaxIndexes = KA.Select((v, idx) => (key12: idx, score: v)).Where(x => x.score == TAMax).Select(x => x.key12).ToList();
                var TAMaxIndex = TAMaxIndexes.FirstOrDefault();

                var TAMin = KA.Min();
                var TAMinIndexes = KA.Select((v, idx) => (key12: idx, score: v)).Where(x => x.score == TAMin).Select(x => x.key12).ToList();
                var TAMinIndex = TAMinIndexes.FirstOrDefault();

                var avargemax = TAMaxIndexes.Average();
                var avargemin = TAMinIndexes.Average();
                if (Math.Abs(TAMax - pairsHalf) > Math.Abs(TAMin - pairsHalf))
                {
                    var guessedKey = TAMaxIndex;

                    var correct = guessedKey == realK12;
                    var a = ToBinaryString((ulong)guessedKey, 12);
                    var b = ToBinaryString((ulong)ksForEncrypt.SubKeys[0], 48); 
                    var c = ToBinaryString((ulong)ksForEncrypt.SubKeys[2], 48); 
                    var d = ToBinaryString((ulong)ksForEncrypt.SubKeys[3], 48); 
                    var e = ToBinaryString((ulong)ksForEncrypt.SubKeys[4], 48); 
                    var f = ToBinaryString((ulong)ksForEncrypt.SubKeys[5], 48);
                    var g = ToBinaryString((ulong)ksForEncrypt.SubKeys[6], 48);
                    var h = ToBinaryString((ulong)ksForEncrypt.SubKeys[7], 48);

                    var actual = GetBit(ksForEncrypt.SubKeys[2], 22)
                        ^ GetBit(ksForEncrypt.SubKeys[3], 44)
                        ^ GetBit(ksForEncrypt.SubKeys[4], 22)
                        ^ GetBit(ksForEncrypt.SubKeys[6], 22);

                    var guessOfRightHandSide = 0;
                }
                else
                {
                    var guessedKey = TAMinIndex;

                    var correct = guessedKey == realK12;

                    var a = ToBinaryString((ulong)guessedKey, 12);
                    var b = ToBinaryString((ulong)ksForEncrypt.SubKeys[0], 48);
                    var c = ToBinaryString((ulong)ksForEncrypt.SubKeys[2], 48);
                    var d = ToBinaryString((ulong)ksForEncrypt.SubKeys[3], 48);
                    var e = ToBinaryString((ulong)ksForEncrypt.SubKeys[4], 48);
                    var f = ToBinaryString((ulong)ksForEncrypt.SubKeys[5], 48);
                    var g = ToBinaryString((ulong)ksForEncrypt.SubKeys[6], 48);
                    var h = ToBinaryString((ulong)ksForEncrypt.SubKeys[7], 48);

                    var actual = GetBit(ksForEncrypt.SubKeys[2], 22)
                        ^ GetBit(ksForEncrypt.SubKeys[3], 44)
                        ^ GetBit(ksForEncrypt.SubKeys[4], 22)
                        ^ GetBit(ksForEncrypt.SubKeys[6], 22);

                    var guessOfRightHandSide = 1;
                }

                // 3) Key counting phase: compute KA (size 2^12) and KB (size 2^12)
                int KB_SIZE = 1 << 18;
                var KB = new long[KB_SIZE];

                // For each 12-bit candidate k, sum appropriate TA entries where left side of eq=0
                // We need function LeftSideEq4(tIndex, kCandidate) -> bool. Equivalent for eq5.
                for (int k = 0; k < KB_SIZE; k++)
                {
                    long sumB = 0;
                    for (int t = 0; t < TB_SIZE; t++)
                    {
                        if (TB[t] != 0 && LeftSideEquation5IsZero(t, k)) sumB += TB[t];
                    }
                    KB[k] = sumB;
                }

                // 4) Sort and take top-K for each
                var topKB = KB.Select((v, idx) => (key12: idx, score: v)).OrderByDescending(x => x.score).Take(TopKPerEquation).ToList();

                var TBMax = KB.Max();
                var TBMaxIndexes = KB.Where(x => x == TBMax).Select((v, idx) => idx).ToList();
                var TBMin = KB.Min();
                var TBMinIndexes = KB.Where(x => x == TBMin).Select((v, idx) => idx).ToList();

                if (Math.Abs(TBMax - pairsHalf) > Math.Abs(TBMin - pairsHalf))
                {

                }
                else
                {

                }

                return new Algorithm2Result(
                    pairs,
                    0,
                    0,
                    false,
                    0,
                    0,
                    topKA,
                    topKB
                );
            }, ct);
        }

        #region === Helper / mapping functions (IMPORTANT: prototype, based on Matsui paper snippets) ===

        // Compute t-index for equation (4) using the 13 effective text bits described in paper
        // This function must extract the 13 relevant bits from P and C and pack into a 13-bit int.
        // The exact bit locations are taken from the Matsui excerpt; you can refine to match your indexing.
        private static int ComputeTIndexA(uint PL, uint PH, uint CL, uint CH)
        {
            // Example selection (please adapt these indices to match exact paper mapping)
            int[] positions = [
                // Use PL 11..15 mapped to indices (1-indexed)
                GetBit(PL, 11),
                GetBit(PL, 12),
                GetBit(PL, 13),
                GetBit(PL, 14),
                GetBit(PL, 15),
                GetBit(PL, 16),
                GetBit(CL, 0),
                GetBit(CL, 27),
                GetBit(CL, 28),
                GetBit(CL, 29),
                GetBit(CL, 30),
                GetBit(CL, 31),
                GetBits(PH, textBitPositions_7_18_24) ^ GetBit(CH, 15) ^ GetBits(CL, textBitPositions_7_18_24_29)
            ];

            return (int) IntArrayToInt13(positions);
        }

        private static int ComputeTIndexB(uint PL, uint PH, uint CL, uint CH)
        {
            // Example selection (please adapt these indices to match exact paper mapping)
            int[] positions = [
                // Use PL 11..15 mapped to indices (1-indexed)
                GetBit(PL, 11),
                GetBit(PL, 12),
                GetBit(PL, 13),
                GetBit(PL, 14),
                GetBit(PL, 15),
                GetBit(PL, 16),
                GetBit(CL, 15),
                GetBit(CL, 16),
                GetBit(CL, 17),
                GetBit(CL, 18),
                GetBit(CL, 19),
                GetBit(CL, 20),
                GetBit(CL, 21),
                GetBit(CL, 22),
                GetBit(CL, 23),
                GetBit(CL, 24),
                GetBits(CH, textBitPositions_7_18_24) ^ GetBits(PL, textBitPositions_7_18_24_29) ^ GetBit(PH, 15)
            ];

            return (int) IntArrayToInt17(positions);
        }

        // For a TA t index and 12-bit key candidate, determine if left side of equation(4) equals zero.
        // This requires reconstructing the left side using t and the candidate key; here we provide prototype logic.
        private bool LeftSideEquation4IsZero(int text, int key)
        {
            int[] textBits = [
                GetBit((uint)text, 0),
                GetBit((uint)text, 1),
                GetBit((uint)text, 2),
                GetBit((uint)text, 3),
                GetBit((uint)text, 4),
                GetBit((uint)text, 5),
                GetBit((uint)text, 6),
                GetBit((uint)text, 7),
                GetBit((uint)text, 8),
                GetBit((uint)text, 9),
                GetBit((uint)text, 10),
                GetBit((uint)text, 11),
                GetBit((uint)text, 12)
            ];

            int[] keyBits = [
                GetBit((uint)key, 0),
                GetBit((uint)key, 1),
                GetBit((uint)key, 2),
                GetBit((uint)key, 3),
                GetBit((uint)key, 4),
                GetBit((uint)key, 5),
                GetBit((uint)key, 6),
                GetBit((uint)key, 7),
                GetBit((uint)key, 8),
                GetBit((uint)key, 9),
                GetBit((uint)key, 10),
                GetBit((uint)key, 11)
            ];

            var PL = ExtractPLTA(textBits);
            var CL = ExtractCLTA(textBits);

            var K1 = ExtractK1TA(keyBits);
            var K8 = ExtractK8TA(keyBits);

            var parity = GetBits(_core.PerformF((uint) PL, (ulong) K1), textBitPositions_7_18_24)
                ^ GetBit(_core.PerformF((uint) CL, (ulong) K8), 15)
                ^ textBits[^1];

            return parity == 0;
        }

        private bool LeftSideEquation5IsZero(int text, int key)
        {
            int[] textBits = [
                GetBit((uint)text, 0),
                GetBit((uint)text, 1),
                GetBit((uint)text, 2),
                GetBit((uint)text, 3),
                GetBit((uint)text, 4),
                GetBit((uint)text, 5),
                GetBit((uint)text, 6),
                GetBit((uint)text, 7),
                GetBit((uint)text, 8),
                GetBit((uint)text, 9),
                GetBit((uint)text, 10),
                GetBit((uint)text, 11),
                GetBit((uint)text, 12),
                GetBit((uint)text, 13),
                GetBit((uint)text, 14),
                GetBit((uint)text, 15),
                GetBit((uint)text, 16)
            ];

            int[] keyBits = [
                GetBit((uint)key, 0),
                GetBit((uint)key, 1),
                GetBit((uint)key, 2),
                GetBit((uint)key, 3),
                GetBit((uint)key, 4),
                GetBit((uint)key, 5),
                GetBit((uint)key, 6),
                GetBit((uint)key, 7),
                GetBit((uint)key, 8),
                GetBit((uint)key, 9),
                GetBit((uint)key, 10),
                GetBit((uint)key, 11),
                GetBit((uint)key, 12),
                GetBit((uint)key, 13),
                GetBit((uint)key, 14),
                GetBit((uint)key, 15),
                GetBit((uint)key, 16),
                GetBit((uint)key, 17)
            ];

            var PL = ExtractPLTA(textBits);
            var CL = ExtractCLTB(textBits);

            var K1 = ExtractK1TA(keyBits);
            var K8 = ExtractK8TB(keyBits);

            var parity = GetBits(_core.PerformF((uint)CL, (ulong) K8), textBitPositions_7_18_24)
                ^ GetBit(_core.PerformF((uint)PL, (ulong) K1), 15)
                ^ textBits[^1];

            return parity == 0;
        }

        /*
        // Verify whether a candidate KeySchedule satisfies the main equations on the pairs (fast test)
        private bool VerifyCandidateOnPairs(KeySchedule candidateKs, List<(ulong P, ulong C)> pairs)
        {
            // We test the primary 8-round equation on a small subset (say first 16 pairs) for speed
            int testCount = Math.Min(16, pairs.Count);
            for (int i = 0; i < testCount; i++)
            {
                var (P, C) = pairs[i];

                // Compute left-hand aggregates using candidate key schedule (with F functions computed)
                // We'll implement the equation as outlined in the paper (approx)
                // For prototype, just encrypt with candidateKs and compare something indicative:
                ulong ctest = _core.EncryptCustom(P, candidateKs, rounds: 8, withIP: false, withFP: false);
                // If ciphertexts not equal, candidate likely wrong (fast reject)
                // Note: real algorithm 2 doesn't compare full ciphertexts; this is a heuristic
                if (ctest != C) return false;
            }
            return true;
        }
        */

        #endregion

        // ---------- utility methods ----------
        private static ulong Random64()
        {
            var buf = RandomNumberGenerator.GetBytes(8);
            return BitConverter.ToUInt64(buf, 0);
        }

        private static int GetBits(uint val, int[] positions)
        {
            int x = 0;
            foreach (var pos in positions)
                x ^= GetBit(val, pos);
            return x;
        }

        private static int GetBits(ulong val, int[] positions)
        {
            int x = 0;
            foreach (var pos in positions)
                x ^= GetBit(val, pos);
            return x;
        }

        private static int GetBit(uint val, int pos)
        {
            // pos: 1..32 MSB-first
            return (int)((val >> pos) & 1U);
        }

        private static int GetBit(ulong val, int pos)
        {
            // pos: 0..31 LSB-first
            return (int)((val >> pos) & 1UL);
        }

        private static uint ExtractPLTA(int[] bits)
        {
            int[] PL = new int[32]; 
            Array.Fill(PL, 0);
            for (int i = 0; i < 6; i++)
            {
                PL[i + 11] = bits[i];
            }
            return IntArrayToInt32(PL);
        }

        private static uint ExtractCLTA(int[] bits)
        {
            int[] CL = new int[32];
            Array.Fill(CL, 0);
            CL[0] = bits[6];
            for (int i = 7; i < bits.Length - 1; i++)
            {
                CL[i + 20] = bits[i];
            }
            return IntArrayToInt32(CL);
        }

        private static uint ExtractCLTB(int[] bits)
        {
            int[] CL = new int[32];
            Array.Fill(CL, 0);
            for (int i = 6; i < bits.Length - 1; i++)
            {
                CL[i + 9] = bits[i];
            }
            return IntArrayToInt32(CL);
        }

        private static ulong ExtractK1TA(int[] bits)
        {
            int[] K1 = new int[48];
            Array.Fill(K1, 0);
            for (int i = 0; i < 6; i++)
            {
                K1[i + 18] = bits[i];
            }
            return IntArrayToInt48(K1);
        }

        private static ulong ExtractK8TA(int[] bits)
        {
            int[] K8 = new int[48];
            Array.Fill(K8, 0);
            for (int i = 6; i < bits.Length; i++)
            {
                K8[i + 36] = bits[i];
            }
            return IntArrayToInt48(K8);
        }

        private static ulong ExtractK8TB(int[] bits)
        {
            int[] K8 = new int[48];
            Array.Fill(K8, 0);
            for (int i = 6; i < bits.Length; i++)
            {
                K8[i + 18] = bits[i];
            }
            return IntArrayToInt48(K8);
        }

        private static uint IntArrayToInt12(int[] bits)
        {
            uint idx = 0;
            for (int i = 0; i < bits.Length; i++)
                idx = (idx << 1) | (uint) (bits[bits.Length - 1 - i] & 1);
            var result = idx & ((1 << 12) - 1);

            //var a = ToBinaryString((ulong)result, 12);

            return result;
        }

        private static uint IntArrayToInt13(int[] bits)
        {
            uint idx = 0;
            for (int i = 0; i < bits.Length; i++)
                idx = (idx << 1) | (uint)(bits[bits.Length - 1 - i] & 1);
            var result = idx & ((1 << 13) - 1);

            //var a = ToBinaryString((ulong)result, 13);

            return result;
        }


        private static uint IntArrayToInt17(int[] bits)
        {
            uint idx = 0;
            for (int i = 0; i < bits.Length; i++)
                idx = (idx << 1) | (uint) (bits[bits.Length - 1 - i] & 1);
            var result = idx & ((1 << 17) - 1);

            //var a = ToBinaryString((ulong)result, 17);

            return result;
        }

        private static uint IntArrayToInt32(int[] bits)
        {
            uint idx = 0;
            for (int i = 0; i < bits.Length; i++)
                idx = (idx << 1) | (uint) (bits[bits.Length - 1 - i] & 1);
            var result = idx & ~( (uint) 0u);

            //var a = ToBinaryString((ulong)result, 32);

            return result;
        }

        private static ulong IntArrayToInt48(int[] bits)
        {
            ulong idx = 0;
            for (int i = 0; i < bits.Length; i++)
                idx = (idx << 1) | (ulong)(bits[bits.Length - 1 - i] & 1);
            var result = idx & ~(0u);

            //var a = ToBinaryString((ulong)result, 48);

            return result;
        }

        private static ulong IntArrayToInt64(int[] bits)
        {
            ulong idx = 0;
            for (int i = 0; i < bits.Length; i++)
                idx = (idx << 1) | (ulong)(bits[bits.Length - 1 - i] & 1);
            var result = idx & ~(0u);

            //var a = ToBinaryString((ulong)result, 64);

            return result;
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