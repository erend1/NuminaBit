namespace NuminaBit.Services.Ciphers.DES.Entities
{
    public sealed record RunInfo
    {
        public ulong IPOut { get; init; }
        public ulong FPIn { get; init; }
        public ulong FinalCipher { get; init; }
        public List<RoundSnap> Rounds { get; init; } = []; // 0..16
        public KeySchedule KeySchedule { get; init; } = new();
    }
}
