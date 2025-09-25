using NuminaBit.Services.Utils;
using NuminaBit.Services.Ciphers.DES;
using NuminaBit.Services.Ciphers.DES.Entities;
using System.Collections;

namespace NuminaBit.Web.App.Pages
{
    public partial class Attack3Round
    {
        // inputs
        private string _keyHex = string.Empty;
        private string KeyHex { get => _keyHex; set { _keyHex = value; PrepareKeyView(); } }
        private bool UseHiddenKey { get; set; } = false;
        private int PairsPerTrial { get; set; } = 25;
        private int TrialsCount { get; set; } = 50;
        private bool IsRunning { get; set; } = false;
        private double ProgressPct { get; set; } = 0;

        private static readonly int[] PairCounts = [5, 10, 15, 20, 25, 30, 40, 50, 75, 100, 200, 250];
        private static readonly int[] TrialsOptions = [10, 25, 50, 75, 100, 250, 500, 1000];

        // results
        private Stack<TrialOutcome> Outcomes { get; set; } = [];
        private List<KeyValuePair<int, double>> CumulativeSuccessPercent { get; set; } = [];

        // derived key view
        private string DisplayKeyHex => UseHiddenKey ? "HIDDEN" : KeyHex.ToUpperInvariant();

        private string KBinary = "";
        private string K1Binary = "";
        private string K3Binary = "";
        private int K1Bit22 = 0;
        private int K3Bit22 = 0;

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
                // K1 is ks.SubKeys[0]
                var k1 = ks.SubKeys[0];
                // K3 is ks.SubKeys[2]
                var k3 = ks.SubKeys[2];
                KBinary = ToBinaryString(key64, 64);
                K1Binary = ToBinaryString(k1, 48);
                K3Binary = ToBinaryString(k3, 48);
                // Get the 22nd bit from the right (0-indexed, where bit 0 is LSB)
                K1Bit22 = (int)((k1 >> 22) & 1UL); 
                K3Bit22 = (int)((k3 >> 22) & 1UL);
                StateHasChanged();
            }
            catch
            {
                KBinary = K1Binary = K3Binary = "";
                K1Bit22 = K3Bit22 = 0;
            }
        }

        private async Task RunTrials()
        {
            IsRunning = true;
            Outcomes.Clear();
            CumulativeSuccessPercent.Clear();
            ProgressPct = 0;
            StateHasChanged();

            await Task.Delay(1);

            ulong key64 = UseHiddenKey ? _attack.HiddenKey : HexUtil.Parse64(KeyHex);

            for (int count = 0; count < TrialsCount; count++)
            {
                var outcome = await _attack.ExecuteOn3RoundSingle(count, key64, PairsPerTrial);
                
                Outcomes.Push(outcome);

                // cumulative success %
                double succCount = Outcomes.Count(o => o.Success);
                double percent = (succCount / Outcomes.Count) * 100.0;
                CumulativeSuccessPercent.Add(new KeyValuePair<int, double>(Outcomes.Count, percent));

                ProgressPct = (double)((count * 100.0) / TrialsCount);

                StateHasChanged();
                
                await Task.Delay(1);
            }

            IsRunning = false;
            StateHasChanged();
        }

        private void ClearResults()
        {
            Outcomes.Clear();
            CumulativeSuccessPercent.Clear();
            ProgressPct = 0;
            IsRunning = false;
            UseHiddenKey = false;
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
