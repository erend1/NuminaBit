using NuminaBit.Services.Ciphers.DES.Entities;
using NuminaBit.Services.Ciphers.DES.Interfaces;

namespace NuminaBit.Services.Ciphers.DES
{
    public class EquationBuilder(ICore des): IEquationBuilder
    {
        //private readonly static string _xorString = " XOR ";
        private readonly static string _xorString = " ⊕ ";
        private readonly static string splitString = ", ";
        private readonly ICore _des = des;

        // get the 6 E positions for S-box index (1..8) from left to right
        private static int[] GetSPositionsForSbox(int sboxIndex)
        {
            int start = 48 - sboxIndex * 6; // 0-based start in array indexing (C# arrays 0..47)
            var res = new int[6];
            for (int j = 0; j < 6; j++)
                res[j] = start + j ; // E positions are 1..48 (we return 0-based positions)
            return res;
        }

        // get the 4 L positions for S-box index (3..0) from right to left
        private static int[] GetLPositionsForSbox(int sboxIndex)
        {
            int start = 32 - sboxIndex * 4; // 0-based start in array indexing (C# arrays 0..31)
            var res = new int[4];
            for (int j = 0; j < 4; j++)
                res[j] = start + j ; // L positions are 1..31 (we return 0-based positions)
            return res;
        }

        // Map E position -> R bit index using DesHelpers.E (E[pos-1] = rIndex)
        private List<int> GetEPositionsFromSbox(int[] sPositions, int alpha)
        {
            var E = _des.Expansion.Mapping; // E array of length 48 with values 1..32
            var outp = new int[sPositions.Length];
            for (int i = 0; i < sPositions.Length; i++)
            {
                int epos = sPositions[i];
                outp[sPositions.Length - i - 1] = E[epos] - 1; // R position 1..32
            }

            var ePositions = new List<int>();
            for (int i = 0; i < outp.Length; i++)
            {
                if (((alpha >> (5 - i)) & 1) == 1)
                {
                    ePositions.Add(outp[i]);
                }
            }
            return ePositions;
        }

        private static List<int> GetKPositionsFromSbox(int[] sPositions, int alpha)
        {
            var kPositions = new List<int>();
            var reverseSPositions = new int[sPositions.Length];
            Array.Copy(sPositions, reverseSPositions, sPositions.Length);
            Array.Reverse(reverseSPositions);
            for (int i = 0; i < sPositions.Length; i++)
            {
                if (((alpha >> (5 - i)) & 1) == 1)
                {
                    kPositions.Add(reverseSPositions[i]);
                }
            }
            return kPositions;
        }

        // Map L position -> F bit index using DesHelpers.E (E[pos-1] = rIndex)
        private List<int> GetFPositionsFromSbox(int[] lPositions, int beta)
        {
            var P = _des.Permutations.InvP; // E array of length 48 with values 1..32
            var outp = new int[lPositions.Length];
            for (int i = 0; i < lPositions.Length; i++)
            {
                int epos = lPositions[i];
                outp[lPositions.Length - i - 1] = P[epos] - 1; // R position 1..32
            }

            var fPositions = new List<int>();
            for (int i = 0; i < outp.Length; i++)
            {
                if (((beta >> (3 - i)) & 1) == 1)
                {
                    fPositions.Add(outp[i]);
                }
            }
            return fPositions;
        }

        /// <summary>
        /// S-box positions are 1..8, so it starts from 1.
        /// </summary>
        /// <param name="sboxIndex"></param>
        /// <param name="alpha"></param>
        /// <param name="beta"></param>
        /// <returns></returns>
        public MappingResult Build(int sboxIndex, int alpha, int beta)
        {
            var sPos = GetSPositionsForSbox(sboxIndex); // 1..48 pos
            var lPos = GetLPositionsForSbox(sboxIndex); // 1..48 pos

            var ePos = GetEPositionsFromSbox(sPos, alpha); // 1..32
            var kPos = GetKPositionsFromSbox(sPos, alpha); // 1..32
            var fPos = GetFPositionsFromSbox(lPos, beta); // 1..32

            // subkey positions are exactly the ePos inside 48-bit subkey
            return new MappingResult(sboxIndex, alpha, beta, sPos, lPos, ePos, kPos, fPos);
        }

        // Pretty-print into human-readable equation
        public string ToHumanEquation(MappingResult m)
        {
            // subkey positions
            var alphaSide = string.Join(_xorString, m.ExpansionPositions.Select(i => $"X[{i}]"));

            // subkey positions
            var betaSide = string.Join(_xorString, m.FunctionPositions.Select(i => $"F(X, K)[{i}]"));

            // subkey positions
            var kSide = string.Join(_xorString, m.SubkeyPositions.Select(i => $"K[{i}]"));

            return $"{alphaSide}{_xorString}{betaSide} = {kSide}";
        }

        public string Latexify(MappingResult m)
        {
            // subkey positions
            var alphaSide = $"X_{{{"i"}}}[{string.Join(splitString, m.ExpansionPositions).TrimEnd().TrimEnd(splitString[0])}]";

            // subkey positions
            var betaSide = $"F(X_{{{"i"}}}, K_{{{"i"}}})[{string.Join(splitString, m.FunctionPositions).TrimEnd().TrimEnd(splitString[0])}]";

            // subkey positions
            var kSide = $"K_{{{"i"}}}[{string.Join(splitString, m.SubkeyPositions).TrimEnd().TrimEnd(splitString[0])}]";

            return $" \\[ {alphaSide} {_xorString} {betaSide} = {kSide} \\]";
        }
    }
}
