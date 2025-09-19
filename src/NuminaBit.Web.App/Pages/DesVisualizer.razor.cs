using NuminaBit.Services.Utils;
using NuminaBit.Web.App.Entities;
using NuminaBit.Services.Ciphers.DES.Entities;

namespace NuminaBit.Web.App.Pages
{
    public partial class DesVisualizer
    {
        private string PlainHex { get; set; } = "0123456789ABCDEF";
        private string KeyHex { get; set; } = "133457799BBCDFF1";
        private bool ShowBinary { get; set; } = false;
        private int StepIndex { get; set; } = 0; // 0..16
        private string? FinalHex { get; set; }

        private RunInfo? Snapshots { get; set; }
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
            if (!HexUtil.TryParse64(PlainHex, out ulong p) || !HexUtil.TryParse64(KeyHex, out ulong k))
                return;

            Snapshots = _des.EncryptWithSnapshots(p, k);
            StepIndex = 16;
            FinalHex = HexUtil.ToHex64(Snapshots.FinalCipher);
            StateHasChanged();
        }

        void StepOnce()
        {
            if (!HexUtil.TryParse64(PlainHex, out ulong p) || !HexUtil.TryParse64(KeyHex, out ulong k))
                return;

            Snapshots ??= _des.EncryptWithSnapshots(p, k);
            StepIndex = Math.Min(16, StepIndex + 1);
            if (StepIndex == 16)
                FinalHex = HexUtil.ToHex64(Snapshots.FinalCipher);
        }

        void OnBitClick(BitTag tag)
        {
            Selected = tag;
        }
    }
}
