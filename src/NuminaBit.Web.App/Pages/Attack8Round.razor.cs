using NuminaBit.Services.Ciphers.DES.Interfaces;
using NuminaBit.Services.Utils;

namespace NuminaBit.Web.App.Pages
{
    public partial class Attack8Round
    {
        // inputs
        private string KeyHex { get; set; } = "133457799BBCDFF1";
        private bool UseHiddenKey { get; set; } = true;
        private int Pairs { get; set; } = 1 << 16; // default
        private int TopK { get; set; } = 128;
        private string MaxCombined { get; set; } = 4096.ToString();
        private string ExhaustiveLimit { get; set; } = 100000.ToString();

        private bool IsRunning { get; set; } = false;

        private Stack<Alg2ResultView> Results = new();
        private List<KeyValuePair<int, double>> CumulativeFound = new();

        private List<(int key12, long score)>? LastTopKA;
        private List<(int key12, long score)>? LastTopKB;

        // options
        int[] PairOptions = new int[] { 1024, 1 << 12, 1 << 14, 1 << 16 }; // safe defaults
        int[] TopKOptions = new int[] { 16, 32, 64, 128, 256 };

        protected override void OnInitialized()
        {
        }

        async Task RunAlg2()
        {
            IsRunning = true;
            StateHasChanged();

            try
            {
                ulong key = UseHiddenKey ? _attack.HiddenKey : HexUtil.Parse64(KeyHex);
                var maxCombined = int.Parse(MaxCombined);
                var exhaustiveLimit = int.Parse(ExhaustiveLimit);
                var res = await _attack.RunSingleAsync(key, Pairs, TopK, maxCombined, exhaustiveLimit);

                // convert result
                Results.Push(new Alg2ResultView
                {
                    Pairs = res.Pairs,
                    TopK = res.TopK,
                    CombinedTry = res.CombinedCandidatesTried,
                    Found = res.Found,
                    FoundKeyHex = res.FoundKey64.HasValue ? $"0x{res.FoundKey64.Value:X16}" : "-",
                    Attempts = res.AttemptsUsed
                });

                LastTopKA = res.TopKA.ToList();
                LastTopKB = res.TopKB.ToList();

                // update cumulative found %
                double foundCount = Results.Count(r => r.Found);
                double perc = (foundCount / Results.Count) * 100.0;
                CumulativeFound.Add(new KeyValuePair<int, double>(Results.Count, perc));
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Alg2 run failed");
            }

            IsRunning = false;
            StateHasChanged();
        }

        void ClearAll()
        {
            Results.Clear();
            CumulativeFound.Clear();
            LastTopKA = LastTopKB = null;
        }

        class Alg2ResultView
        {
            public int Pairs { get; set; }
            public int TopK { get; set; }
            public int CombinedTry { get; set; }
            public bool Found { get; set; }
            public string FoundKeyHex { get; set; } = "-";
            public int Attempts { get; set; }
        }
    }
}
