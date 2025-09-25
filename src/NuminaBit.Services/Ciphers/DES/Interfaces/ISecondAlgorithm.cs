using static NuminaBit.Services.Ciphers.DES.SecondAlgorithmRunner;

namespace NuminaBit.Services.Ciphers.DES.Interfaces
{
    public interface ISecondAlgorithm
    {
        public ulong HiddenKey { get; }
        Task<Algorithm2Result> RunSingleAsync(ulong key64, int pairs, CancellationToken ct = default);
    }
}
