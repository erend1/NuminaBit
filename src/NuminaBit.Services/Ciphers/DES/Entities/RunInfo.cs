namespace NuminaBit.Services.Ciphers.DES.Entities
{
    public sealed class RunInfo
    {
        public ulong IPOut { get; set; }
        public ulong FPIn { get; set; }
        public ulong FinalCipher { get; set; }
        public List<RoundSnap> Rounds { get; set; } = []; // 0..16
        public KeySchedule KeySchedule { get; set; } = new();
    }
}
