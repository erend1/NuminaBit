namespace NuminaBit.Services.Ciphers.DES.Entities
{
    public sealed class RoundSnap
    {
        public uint L { get; set; }
        public uint R { get; set; }
        public ulong ER { get; set; }    // 48
        public ulong EXorK { get; set; } // 48
        public uint SBoxOut { get; set; } // 32
        public uint PermOut { get; set; } // 32 (P sonrası)
        public uint LXorF { get; set; }   // L_{i-1} ⊕ F(R_{i-1},K_i)
    }
}
