using NuminaBit.Services.Ciphers.DES.Interfaces;
using NuminaBit.Console.Services.Shared.Intefaces;

namespace NuminaBit.Console.Services.Shared
{
    public class AttackRunner2Examples(IAttackRunner2 attack): IAttackRunner2Examples
    {
        private readonly IAttackRunner2 _attack = attack;

        public async void Example1()
        {
            var successes = 0;
            for (int i = 0; i < 250; i++)
            {
                var a = await _attack.RunAlgorithm1SingleAsync(0x133456798BBCDFF1UL, 100);
                System.Console.WriteLine(a.Success);
                if(a.Success)
                    successes++;
            }
            System.Console.WriteLine($"Done Example1 : {successes}");
            System.Console.ReadLine();
        }
    }
}
