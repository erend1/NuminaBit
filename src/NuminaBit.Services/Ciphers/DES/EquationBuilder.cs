using NuminaBit.Services.Ciphers.DES.Entities;
using NuminaBit.Services.Ciphers.DES.Interfaces;

namespace NuminaBit.Services.Ciphers.DES
{
    public class EquationBuilder(IDES des): IEquationBuilder
    {
        private readonly static string _xorString2 = " ⊕ ";
        private readonly static string _xorString = " XOR ";
        private readonly IDES _des = des;

        // get the 6 E positions for S-box index (0..7)
        public int[] GetEPositionsForSbox(int sboxIndex)
        {
            // DesHelpers.E is expected as int[48] or int[]  (1-based values for R positions)
            var E = _des.Expansion.E; // length 48, values 1..32
                                  // The E expansion groups into 8 blocks of 6 consecutive positions for Sboxes (standard).
                                  // We pick the block (1-based E positions 6*i..6*i+5)
            int start = sboxIndex * 6; // 0-based start in array indexing (C# arrays 0..47)
            var res = new int[6];
            for (int j = 0; j < 6; j++)
                res[j] = start + j + 1; // E positions are 1..48 (we return 1-based positions)
            return res;
        }

        // Map E position -> R bit index using DesHelpers.E (E[pos-1] = rIndex)
        public int[] EPositionsToRPositions(int[] ePositions)
        {
            var E = _des.Expansion.E; // E array of length 48 with values 1..32
            var outp = new int[ePositions.Length];
            for (int i = 0; i < ePositions.Length; i++)
            {
                int epos = ePositions[i];
                outp[i] = E[epos - 1]; // R position 1..32
            }
            return outp;
        }

        // For Sbox index s (0..7), and beta bit index b (0..3), find which P-output position (1..32) that Sbox-output-bit goes to
        // Idea: create 32-bit word with only this Sbox's 4-bit chunk set to (1<<b) at its local position, then permute by P and extract the output bit index
        public int BetaBitToPOutputPos(int sboxIndex, int betaBitIndex)
        {
            // Build 32-bit sbox-output vector before permutation
            // In DES, S-box outputs are concatenated as S1(4bits) | S2(4bits) | ... | S8(4bits)
            // We'll set only that 4-bit chunk's beta bit to 1.
            int chunkIndex = sboxIndex; // 0..7
                                        // Pre-permutation 32-bit: bit numbering (1..32) with MSB=bit1
                                        // The 4-bit chunk occupies bits: chunkBase..chunkBase+3 (1-based)
            int chunkBase = 1 + chunkIndex * 4; // 1-based
            int localBitPos = chunkBase + betaBitIndex; // 1-based pos inside 32-bit
                                                        // set local bit offset inside chunk: betaBitIndex 0..3, choose mapping consistent with how SBoxOut is assembled in DesCore
                                                        // We'll assume bit 0 is MSB of the 4-bit nibble: i.e. if betaBitIndex==0 -> bit at chunkBase (msb)
                                                        // Set that bit in a 32-bit word
            uint pre = 1u << 32 - localBitPos;
            // Now apply P permutation: DesHelpers.P is expected int[32] where each entry is source bit index in pre (1..32)
            var P = _des.Permutations.P;
            for (int outPos = 1; outPos <= 32; outPos++)
            {
                int src = P[outPos - 1]; // which pre position maps to this output pos
                if (src == localBitPos) return outPos; // found the mapping
            }
            throw new InvalidOperationException("P-map not found");
        }

        // Build mapping result
        public MappingResult Build(int sboxIndex, int alpha, int beta)
        {
            var ePos = GetEPositionsForSbox(sboxIndex); // 1..48 pos
            var rPos = EPositionsToRPositions(ePos); // 1..32
            var betaMap = new List<(int, int)>();
            for (int b = 0; b < 4; b++)
            {
                if (((beta >> (3 - b)) & 1) == 1) // interpret beta MSB-first
                {
                    int pOut = BetaBitToPOutputPos(sboxIndex, b);
                    betaMap.Add((b, pOut));
                }
            }
            // subkey positions are exactly the ePos inside 48-bit subkey
            return new MappingResult(sboxIndex, alpha, beta, ePos, rPos, betaMap.ToArray(), ePos);
        }

        // Pretty-print into human-readable equation
        public string ToHumanEquation(MappingResult m)
        {
            Console.WriteLine(m);

            // collect alpha set bits -> corresponding R positions
            var alphaBits = new List<int>();
            for (int i = 0; i < 6; i++)
            {
                int bitval = (m.Alpha >> (5 - i)) & 1; // MSB-first over 6 bits
                if (bitval == 1) alphaBits.Add(m.RPositions[i]); // R position 1..32
            }
            var alphaSide = alphaBits.Count == 0 ? "0" : string.Join(_xorString, alphaBits.Select(i => $"R[{i}]"));

            // collect beta mapped F outputs
            var betaSide = m.BetaToPOutputs.Length == 0 ? "0"
                : string.Join(_xorString, m.BetaToPOutputs.Select(t => $"F[{t.pOutputPos}]"));

            // subkey positions
            var kSide = string.Join(_xorString, m.SubkeyPositions.Select(i => $"K[{i}]"));

            return $"{alphaSide} ⊕ {betaSide} = {kSide}";
        }
    }
}
