namespace NuminaBit.Services.Ciphers.DES.Interfaces
{
    public interface IAttackRunner
    {
        public ulong HiddenKey { get; }
        public Task<bool> RunAlgorithm1(ulong key64, int pairs);
    }
}
