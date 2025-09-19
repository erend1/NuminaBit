namespace NuminaBit.Services.Ciphers.DES.Entities
{
    public sealed class Permutations
    {
        public int[] Initial { get; set; } = [];
        public int[] Final { get; set; } = [];
        public int[] PC1 { get; set; } = [];
        public int[] PC2 { get; set; } = [];
        public int[] P { get; set; } = [];
    }
}
