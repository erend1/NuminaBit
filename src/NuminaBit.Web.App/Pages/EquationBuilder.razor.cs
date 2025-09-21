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

        private string alphaBin = "100000";
        private string betaBin = "1111";

        private MappingResult? mapping;

        private string equationText = "";
        private string latexEquation = "";

        private void Clear()
        {
            mapping = null;
            equationText = "";
            latexEquation = "";
        }

        private void Build()
        {
            try
            {
                if (!TryParseBin(alphaBin, 6, out int alpha)) 
                { 
                    Log.LogWarning("alpha could not be parsed"); 
                    return; 
                }
                if (!TryParseBin(betaBin, 4, out int beta)) 
                { 
                    Log.LogWarning("beta could not be parsed"); 
                    return; 
                }

                // Build mapping with S-box index (1..8), alpha (0..63), beta (0..15)
                mapping = _builder.Build(SelectedSBoxIndex + 1, alpha, beta);

                Console.WriteLine($"Mapping Result: {mapping}");

                // format pretty
                equationText = _builder.ToHumanEquation(mapping);

                // latexify
                latexEquation = _builder.Latexify(mapping);
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