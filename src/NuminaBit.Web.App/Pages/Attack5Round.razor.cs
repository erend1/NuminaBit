using NuminaBit.Services.Utils;
using NuminaBit.Services.Ciphers.DES.Entities;

namespace NuminaBit.Web.App.Pages
{
    public partial class Attack5Round
    {
        // inputs
        private CancellationTokenSource _cts = new();
        private string _keyHex = string.Empty;
        private string KeyHex { get => _keyHex; set { _keyHex = value; PrepareKeyView(); } }
        private bool UseHiddenKey { get; set; } = false;
        private int PairsPerTrial { get; set; } = 1000;
        private int TrialsCount { get; set; } = 10;
        private bool IsRunning { get; set; } = false;
        private double ProgressPct { get; set; } = 0;

        private static readonly int[] _pairCounts = [500, 750, 1000, 1500, 2000, 2500, 3000, 4000, 5000];
        private static readonly int[] _trialsOptions = [10, 25, 50, 75, 100, 250, 500, 750, 1000];

        // results
        private Stack<TrialOutcome> Outcomes { get; set; } = [];
        private List<KeyValuePair<int, double>> CumulativeSuccessPercent { get; set; } = [];

        // derived key view
        private string DisplayKeyHex => UseHiddenKey ? "HIDDEN" : KeyHex.ToUpperInvariant();

        private string KBinary = "";
        private string K1Binary = "";
        private string K2Binary = "";
        private string K4Binary = "";
        private string K5Binary = "";
        private int K1Bit42_43_45_46 = 0;
        private int K2Bit22 = 0;
        private int K4Bit22 = 0;
        private int K5Bit42_43_45_46 = 0;

        private double FinalSuccessPercent => CumulativeSuccessPercent.Count == 0 ? 0.0 : CumulativeSuccessPercent.Last().Value;

        protected override void OnInitialized()
        {
            KeyHex = HexUtil.Random64Hex();
            PrepareKeyView();
        }

        private void PrepareKeyView()
        {
            try
            {
                ulong key64 = UseHiddenKey ? _attack.HiddenKey : HexUtil.Parse64(KeyHex);
                var ks = _des.BuildKeySchedule(key64);
                var k1 = ks.SubKeys[0];
                var k2 = ks.SubKeys[1];
                var k4 = ks.SubKeys[3];
                var k5 = ks.SubKeys[4];
                KBinary = ToBinaryString(key64, 64);
                K1Binary = ToBinaryString(k1, 48);
                K2Binary = ToBinaryString(k2, 48);
                K4Binary = ToBinaryString(k4, 48);
                K5Binary = ToBinaryString(k5, 48);

                // Get the 22nd bit from the right (0-indexed, where bit 0 is LSB)
                var a = (int)((k1 >> 42) & 1UL);
                var b = (int)((k1 >> 43) & 1UL);
                var c = (int)((k1 >> 45) & 1UL);
                var d = (int)((k1 >> 46) & 1UL);
                K1Bit42_43_45_46 = a ^ b ^ c ^ d;
                K2Bit22 = (int)((k2 >> 22) & 1UL); 
                K4Bit22 = (int)((k4 >> 22) & 1UL);
                var e = (int)((k5 >> 42) & 1UL);
                var f = (int)((k5 >> 43) & 1UL);
                var g = (int)((k5 >> 45) & 1UL);
                var h = (int)((k5 >> 46) & 1UL);
                K5Bit42_43_45_46 = e ^ f ^ g ^ h;
                StateHasChanged();
            }
            catch
            {
                KBinary = K1Binary = K2Binary = K4Binary = K5Binary = "";
                K1Bit42_43_45_46 = K2Bit22 = K4Bit22 = K5Bit42_43_45_46 = 0;
            }
        }

        private async Task RunTrials()
        {
            _cts = new();
            IsRunning = true;
            Outcomes.Clear();
            CumulativeSuccessPercent.Clear();
            ProgressPct = 0;
            StateHasChanged();

            var token = _cts.Token; // just to avoid warning
            await Task.Delay(1);

            try
            {
                ulong key64 = UseHiddenKey ? _attack.HiddenKey : HexUtil.Parse64(KeyHex);

                for (int count = 0; count < TrialsCount; count++)
                {
                    var outcome = await _attack.ExecuteOn5RoundSingle(count, key64, PairsPerTrial, token);
                    Outcomes.Push(outcome);

                    // cumulative success %
                    double succCount = Outcomes.Count(o => o.Success);
                    double percent = (succCount / Outcomes.Count) * 100.0;
                    CumulativeSuccessPercent.Add(new KeyValuePair<int, double>(Outcomes.Count, percent));

                    ProgressPct = (double)((count * 100.0) / TrialsCount);

                    StateHasChanged();

                    await Task.Delay(1);
                }
            }
            finally
            {
                IsRunning = false;
                StateHasChanged();
            }
        }

        private void Cancel()
        {
            _cts.Cancel();
            IsRunning = false;
            StateHasChanged();
        }

        private void ClearResults()
        {
            Outcomes.Clear();
            CumulativeSuccessPercent.Clear();
            ProgressPct = 0;
            UseHiddenKey = false;
            IsRunning = false;
            KeyHex = HexUtil.Random64Hex();
            PrepareKeyView();
        }

        // helpers
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
