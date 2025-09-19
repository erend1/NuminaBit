namespace NuminaBit.Services.Ciphers.DES.Entities
{
    public sealed record MappingResult
    {
        public MappingResult(int sboxIndex, int alpha, int beta, int[] ePos1, int[] rPos, (int, int)[] values, int[] ePos2)
        {
            SBoxIndex = sboxIndex;
            Alpha = alpha;
            Beta = beta;
            EPositions = ePos1;
            RPositions = rPos;
            BetaToPOutputs = values;
            SubkeyPositions = ePos2;
        }

        public int SBoxIndex { get; init; }
        public int Alpha { get; init; } // 0..63
        public int Beta { get; init; } // 0..15
        public int[] EPositions { get; init; } = []; // 6 pos (1..48) for this Sbox in E expansion order
        public int[] RPositions { get; init; } = []; // corresponding R bit positions (1..32)
        public  (int betaBitIndex, int pOutputPos)[] BetaToPOutputs { get; init; } = []; // per β-bit set -> P output index (1..32)
        public int[] SubkeyPositions { get; init; } = []; // positions inside 48-bit subkey (1..48), same as EPositions
    }
}
