using NuminaBit.Services.Ciphers.DES.Entities;

namespace NuminaBit.Services.Ciphers.DES.Interfaces
{
    public interface IDES
    {
        public Permutations Permutations { get; }
        public Expansion Expansion { get; }
        public Substitutions Substitutions { get; }

        public ulong EncryptFast(ulong plain, KeySchedule ks);
        public RunInfo EncryptWithSnapshots(ulong plain, ulong key64);
        public KeySchedule BuildKeySchedule(ulong key64);

        public ulong Permute(ulong val, int[] table, int inWidth, int outWidth = -1);
        public ulong Permute(uint val, int[] table, int inWidth, int outWidth = -1);
    }
}
