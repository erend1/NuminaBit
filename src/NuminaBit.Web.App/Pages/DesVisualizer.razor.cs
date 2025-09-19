using Radzen;
using System.Text;
using Radzen.Blazor;
using NuminaBit.Services.Ciphers.DES;
using Microsoft.AspNetCore.Components;

namespace NuminaBit.Web.App.Pages
{
    public partial class DesVisualizer
    {
        private string PlainHex { get; set; } = "0123456789ABCDEF";
        private string KeyHex { get; set; } = "133457799BBCDFF1";
        private bool ShowBinary { get; set; } = false;
        private int StepIndex { get; set; } = 0; // 0..16
        private string? FinalHex { get; set; }

        private DesRun? Snapshots { get; set; }
        private BitTag? Selected { get; set; }

        protected override void OnInitialized()
        {
            Reset();
        }

        void Reset()
        {
            StepIndex = 0;
            FinalHex = null;
            Selected = null;
            Snapshots = null;
        }

        void RandomPlain()
        {
            PlainHex = HexUtil.Random64Hex();
        }

        void RandomKey()
        {
            KeyHex = HexUtil.Random64Hex();
        }

        void RunAll()
        {
            var core = new DesCore();
            if (!HexUtil.TryParse64(PlainHex, out ulong p) || !HexUtil.TryParse64(KeyHex, out ulong k))
                return;

            Snapshots = core.EncryptWithSnapshots(p, k);
            StepIndex = 16;
            FinalHex = HexUtil.ToHex64(Snapshots.FinalCipher);
            StateHasChanged();
        }

        void StepOnce()
        {
            var core = new DesCore();
            if (!HexUtil.TryParse64(PlainHex, out ulong p) || !HexUtil.TryParse64(KeyHex, out ulong k))
                return;

            Snapshots ??= core.EncryptWithSnapshots(p, k);
            StepIndex = Math.Min(16, StepIndex + 1);
            if (StepIndex == 16)
                FinalHex = HexUtil.ToHex64(Snapshots.FinalCipher);
        }

        void OnBitClick(BitTag tag)
        {
            Selected = tag;
        }

        RadzenColumn[] inputRefs = new RadzenColumn[8];
        public string[] Characters { get; set; } = new string[8];

        void ShowTooltip(ElementReference element, int index)
        {
            var character = Characters[index];
            if (!string.IsNullOrEmpty(character))
            {
                // Get the UTF-8 bytes for the character
                byte[] bytes = Encoding.UTF8.GetBytes(character);

                // Construct the binary string from the bytes
                var binaryStrings = new List<string>();
                foreach (var b in bytes)
                {
                    binaryStrings.Add(Convert.ToString(b, 2).PadLeft(8, '0'));
                }
                string binaryOutput = string.Join(" ", binaryStrings);

                TooltipService.Open(element, $"Binary: {binaryOutput}", new TooltipOptions { Position = TooltipPosition.Top });
            }
        }
    }
}
