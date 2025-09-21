using NuminaBit.Services.Ciphers.DES.Interfaces;
using NuminaBit.Console.Services.Shared.Intefaces;

namespace NuminaBit.Console.Services.Shared
{
    public class AttackRunner2Examples(IAttackRunner2 attack): IAttackRunner2Examples
    {
        private readonly IAttackRunner2 _attack = attack;

        public void Example1()
        {
            _attack.RunAlgorithm1SingleAsync(0x133457799BBCDFF1UL, 1000).Wait();
        }
    }
}
