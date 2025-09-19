namespace NuminaBit.Services.Ciphers.DES.Entities
{
    public sealed class KeySchedule
    {
        public static readonly int[] SHIFTS = [1, 1, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 2, 1];

        public ulong PC1Out { get; set; } // 56
        public ulong[] SubKeys { get; set; } = new ulong[16]; // her biri 48-bit

        public static int PerformShift(int value) => SHIFTS[value];
    }
}
