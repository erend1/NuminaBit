using NuminaBit.Services.Utils;

namespace NuminaBit.Web.App.Pages
{
    public partial class Attack3Round
    {
        private string KeyHex { get; set; } = "133457799BBCDFF1";
        private bool UseHiddenKey { get; set; } = false;
        private int PairCount { get; set; } = 100;
        private string? ResultMessage { get; set; }
        private bool IsRunning { get; set; }

        private readonly int[] PairCounts = [100, 500, 1000, 5000];

        async Task RunAttack()
        {
            IsRunning = true;
            ResultMessage = null;
            StateHasChanged();

            try
            {
                ulong key = UseHiddenKey ? _attack.HiddenKey : HexUtil.TryParse64(KeyHex, out ulong p) ? p : throw new Exception("Invalid key hex");
                var res = await _attack.RunAlgorithm1(key, PairCount);
                ResultMessage = $"Guessed XOR(K1[22], K3[22]) = {(res ? 1 : 0)}";
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Error in attack");
                ResultMessage = "Error: " + ex.Message;
            }

            IsRunning = false;
            StateHasChanged();
        }
    }
}
