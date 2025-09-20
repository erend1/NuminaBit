using NuminaBit.Services.Ciphers.DES.Entities;

namespace NuminaBit.Services.Ciphers.DES.Interfaces
{
    public interface IEquationBuilder
    {
        public MappingResult Build(int sboxIndex, int alpha, int beta);
        public string ToHumanEquation(MappingResult m);
    }
}
