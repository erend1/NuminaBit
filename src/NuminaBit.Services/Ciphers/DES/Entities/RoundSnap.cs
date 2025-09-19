namespace NuminaBit.Services.Ciphers.DES.Entities
{
    public sealed record RoundSnap
    {
        public uint L { get; init; }
        public uint R { get; init; }
        public ulong ER { get; init; }    // 48
        public ulong EXorK { get; init; } // 48
        public uint SBoxOut { get; init; } // 32
        public uint PermOut { get; init; } // 32 (P sonrası)
        public uint LXorF { get; init; }   // L_{i-1} ⊕ F(R_{i-1},K_i)
    }
}
