namespace NuminaBit.Services.Ciphers.DES.Entities
{
    public sealed record Permutations
    {
        public int[] Initial { get; init; } = [];
        public int[] Final { get; init; } = [];
        public int[] PC1 { get; init; } = [];
        public int[] PC2 { get; init; } = [];
        public int[] P { get; init; } = [];
    }
}
