using NuminaBit.Services.Ciphers.DES.Interfaces;
using NuminaBit.Console.Services.Shared.Intefaces;

namespace NuminaBit.Console.Services.Shared
{
    public class FirstAlgorithmExamples(IFirstAlgorithm attack): IFirstAlgorithmExamples
    {
        private readonly IFirstAlgorithm _attack = attack;

        public async void Example1()
        {
            var successes = 0;
            for (int i = 0; i < 100; i++)
            {
                var a = await _attack.ExecuteOn3RoundSingle(1, 0x133456798BBCDFF1UL, 100);
                System.Console.WriteLine(a.Success);
                if(a.Success)
                    successes++;
            }
            System.Console.WriteLine($"Done Example1 : {successes}");
            System.Console.ReadLine();
        }

        public async void Example2()
        {
            var successes = 0;
            for (int i = 0; i < 100; i++)
            {
                var a = await _attack.ExecuteOn5RoundSingle(2, 0x133456798BBCDFF1UL, 3000);
                System.Console.WriteLine(a.Success);
                if (a.Success)
                    successes++;
            }
            System.Console.WriteLine($"Done Example1 : {successes}");
            System.Console.ReadLine();
        }
    }
}
