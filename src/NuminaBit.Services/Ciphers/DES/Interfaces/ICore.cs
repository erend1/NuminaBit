using NuminaBit.Services.Ciphers.DES.Entities;

namespace NuminaBit.Services.Ciphers.DES.Interfaces
{
    public interface ICore
    {
        Permutations Permutations { get; }
        Expansion Expansion { get; }
        Substitutions Substitutions { get; }

        ulong EncryptFast(ulong plain, KeySchedule ks);
        ulong EncryptCustom(ulong plain, KeySchedule ks,
            int rounds = 3, bool withIP = false, bool withFP = false);
        RunInfo EncryptWithSnapshots(ulong plain, ulong key64);
        KeySchedule BuildKeySchedule(ulong key64);

        ulong Permute(ulong val, int[] table, int inWidth, int outWidth = -1);
        ulong Permute(uint val, int[] table, int inWidth, int outWidth = -1);
        uint PerformF(uint roundText, ulong roundKey);
    }
}
