using NuminaBit.Services.Ciphers.DES.Entities;

namespace NuminaBit.Services.Ciphers.DES.Interfaces
{
    public interface IEquationBuilder
    {
        public int[] GetEPositionsForSbox(int sboxIndex);
        public int[] EPositionsToRPositions(int[] ePositions);
        public int BetaBitToPOutputPos(int sboxIndex, int betaBitIndex);
        public MappingResult Build(int sboxIndex, int alpha, int beta);
        public string ToHumanEquation(MappingResult m);
    }
}
