using NuminaBit.Services.Ciphers.DES.Interfaces;
using NuminaBit.Console.Services.Shared.Intefaces;

namespace NuminaBit.Console.Services.Shared
{
    public class EquationBuilderExamples(IEquationBuilder builder): IEquationBuilderExamples
    {
        private readonly IEquationBuilder _builder = builder;

        public void Example1()
        {
            int sboxIndex = 5; // S5
            int alpha = 16; // example alpha
            int beta = 15; // example beta
            var mapping = _builder.Build(sboxIndex, alpha, beta);
            System.Console.WriteLine($"Mapping Result: {mapping}");
            System.Console.WriteLine(_builder.ToHumanEquation(mapping));
        }

        public void Example2()
        {
            int sboxIndex = 1; // S5
            int alpha = 27; // example alpha
            int beta = 4; // example beta
            var mapping = _builder.Build(sboxIndex, alpha, beta);
            System.Console.WriteLine($"Mapping Result: {mapping}");
            System.Console.WriteLine(_builder.ToHumanEquation(mapping));
        }
    }
}
