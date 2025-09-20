using NuminaBit.Services.Utils;
using NuminaBit.Services.Ciphers.DES;
using NuminaBit.Services.Ciphers.DES.Entities;

namespace NuminaBit.Web.App.Pages
{
    public partial class Attack3Round2
    {
        // inputs
        private string KeyHex { get; set; } = "133457799BBCDFF1";
        private bool UseHiddenKey { get; set; } = false;
        private int PairsPerTrial { get; set; } = 500;
        private int TrialsCount { get; set; } = 20;
        private bool IsRunning { get; set; } = false;
        private double ProgressPct { get; set; } = 0;

        private readonly int[] PairCounts = [100, 200, 500, 1000, 5000];
        private readonly int[] TrialsOptions = [5, 10, 20, 50, 100];

        // results
        private List<TrialOutcome> Outcomes { get; set; } = new();
        private List<KeyValuePair<int, double>> CumulativeSuccessPercent { get; set; } = new();

        // derived key view
        private string DisplayKeyHex => UseHiddenKey ? "HIDDEN" : KeyHex.ToUpperInvariant();
        private string KeyBinaryFull => GetKeyBinaryFull();
        private string KeyBinaryShort => KeyBinaryFull?.Substring(0, Math.Min(64, KeyBinaryFull.Length)) ?? "";
        private string KeyBinaryTooltip => KeyBinaryFull + "\n\nUTF8 (attempt): " + KeyToUtf8Preview();

        private string K1Binary = "";
        private string K3Binary = "";
        private int K1Bit22 = 0;
        private int K3Bit22 = 0;

        private double FinalSuccessPercent => CumulativeSuccessPercent.Count == 0 ? 0.0 : CumulativeSuccessPercent.Last().Value;

        protected override void OnInitialized()
        {
            PrepareKeyView();
        }

        void PrepareKeyView()
        {
            try
            {
                ulong key64 = UseHiddenKey ? _attack.HiddenKey : HexUtil.Parse64(KeyHex);
                var ks = new Core().BuildKeySchedule(key64);
                // K1 is ks.SubKeys[0], K3 is ks.SubKeys[2]
                var k1 = ks.SubKeys[0];
                var k3 = ks.SubKeys[2];
                K1Binary = ToBinaryString(k1, 48);
                K3Binary = ToBinaryString(k3, 48);
                K1Bit22 = (int)((k1 >> (48 - 22)) & 1UL);
                K3Bit22 = (int)((k3 >> (48 - 22)) & 1UL);
            }
            catch
            {
                K1Binary = K3Binary = "";
                K1Bit22 = K3Bit22 = 0;
            }
        }

        async Task RunTrials()
        {
            IsRunning = true;
            Outcomes.Clear();
            CumulativeSuccessPercent.Clear();
            ProgressPct = 0;
            StateHasChanged();

            ulong key64 = UseHiddenKey ? _attack.HiddenKey : HexUtil.Parse64(KeyHex);

            for (int t = 0; t < TrialsCount; t++)
            {
                var outcome = await _attack.RunAlgorithm1SingleAsync(key64, PairsPerTrial);
                Outcomes.Add(outcome);

                // cumulative success %
                double succCount = Outcomes.Count(o => o.Success);
                double percent = (succCount / Outcomes.Count) * 100.0;
                CumulativeSuccessPercent.Add(new KeyValuePair<int, double>(Outcomes.Count, percent));

                ProgressPct = (double)Outcomes.Count / TrialsCount * 100.0;
                StateHasChanged();
            }

            // refresh key view (in case hidden toggle)
            PrepareKeyView();

            IsRunning = false;
            StateHasChanged();
        }

        void ClearResults()
        {
            Outcomes.Clear();
            CumulativeSuccessPercent.Clear();
            ProgressPct = 0;
        }

        // helpers
        private string ToBinaryString(ulong v, int width)
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

        private string GetKeyBinaryFull()
        {
            try
            {
                ulong key64 = UseHiddenKey ? _attack.HiddenKey : HexUtil.Parse64(KeyHex);
                return ToBinaryString(key64, 64);
            }
            catch { return ""; }
        }

        private string KeyToUtf8Preview()
        {
            try
            {
                // try interpret key hex as bytes -> utf8 (print ASCII-ish)
                var bytes = HexUtil.ToBytes(KeyHex);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch { return ""; }
        }
    }
}
