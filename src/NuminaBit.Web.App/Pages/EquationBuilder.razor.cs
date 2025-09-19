using NuminaBit.Web.App.Entities;
using NuminaBit.Services.Ciphers.DES.Entities;

namespace NuminaBit.Web.App.Pages
{
    public partial class EquationBuilder
    {
        // S-box options.
        private readonly static List<SBoxItem> SBoxOptions = [
            new("S1",0), new("S2",1), new("S3",2), new("S4",3),
            new("S5",4), new("S6",5), new("S7",6), new("S8",7)
        ];

        // State
        private int SelectedSBoxIndex { get; set; } = 4;

        private string alphaBin = "100100";
        private string betaBin = "0001";
        private MappingResult? mapping;
        private string equationText = "";
        private string latexEquation = "";

        private List<int> alphaRList = [];
        private List<int> betaFList = [];

        private void Clear()
        {
            mapping = null;
            equationText = "";
            latexEquation = "";
            alphaRList.Clear(); betaFList.Clear();
        }

        private void Build()
        {
            try
            {
                if (!TryParseBin(alphaBin, 6, out int alpha)) { Log.LogWarning("alpha parse"); return; }
                if (!TryParseBin(betaBin, 4, out int beta)) { Log.LogWarning("beta parse"); return; }

                mapping = _builder.Build(SelectedSBoxIndex, alpha, beta);
                // format pretty
                equationText = _builder.ToHumanEquation(mapping);
                // build lists
                alphaRList = [];
                for (int i = 0; i < 6; i++) if (((alpha >> (5 - i)) & 1) == 1) alphaRList.Add(mapping.RPositions[i]);
                betaFList = [.. mapping.BetaToPOutputs.Select(t => t.pOutputPos)];
                // latexify
                var leftParts = new List<string>();
                if (alphaRList.Count > 0) leftParts.AddRange(alphaRList.Select(i => $"R_{{{i}}}"));
                if (betaFList.Count > 0) leftParts.AddRange(betaFList.Select(i => $"F_{{{i}}}"));
                var left = leftParts.Count > 0 ? string.Join(" \\oplus ", leftParts) : "0";
                var right = string.Join(" \\oplus ", mapping.SubkeyPositions.Select(i => $"K_{{{i}}}"));
                latexEquation = $"{left} = {right}";
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Build failed");
            }
        }

        private static bool TryParseBin(string s, int bits, out int val)
        {
            val = 0;
            s = s?.Trim() ?? "";
            if (s.Length != bits) return false;
            foreach (char c in s)
            {
                if (c != '0' && c != '1') return false;
                val = (val << 1) | (c == '1' ? 1 : 0);
            }
            return true;
        }
    }
}
