namespace NuminaBit.Services.Ciphers.DES.Entities
{
    public sealed record MappingResult
    {
        public MappingResult(int sboxIndex, int alpha, int beta, 
            int[] sPos, int[] lPos, List<int> ePos, List<int> kPos, List<int> fPos)
        {
            SBoxIndex = sboxIndex;

            Alpha = alpha;
            Beta = beta;

            SBoxInputPositions = sPos;
            SBoxOutputPositions = lPos;

            ExpansionPositions = ePos;
            SubkeyPositions = kPos;
            FunctionPositions = fPos;
        }

        public int SBoxIndex { get; init; }
        public int Alpha { get; init; } // 0..63
        public int Beta { get; init; } // 0..15
        public int[] SBoxInputPositions { get; init; } = [];
        public int[] SBoxOutputPositions { get; init; } = [];
        public List<int> ExpansionPositions { get; init; } = [];
        public List<int> SubkeyPositions { get; init; } = [];
        public List<int> FunctionPositions { get; init; } = [];
    }
}
