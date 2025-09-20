namespace NuminaBit.Services.Ciphers.DES.Entities
{
    public sealed record Expansion
    {
        public int[] Mapping { get; init; } = [];
        public Dictionary<int, int[]> InverseMapping { get; init; } = [];
    }
}
