using System.Security.Cryptography;
using NuminaBit.Services.Ciphers.DES.Interfaces;

namespace NuminaBit.Services.Ciphers.DES
{
    public class AttackRunner(ICore des): IAttackRunner
    {
        private readonly ICore _des = des;
        private static readonly int[] bitPositions = [7, 18, 24, 29];
        private static readonly ulong _hiddenKey = 0x133457799BBCDFF1UL; // static hidden key

        public ulong HiddenKey => _hiddenKey;

        public async Task<bool> RunAlgorithm1(ulong key64, int pairs)
        {
            return await Task.Run(() =>
            {
                var ks = _des.BuildKeySchedule(key64);
                int counter = 0;

                for (int i = 0; i < pairs; i++)
                {
                    ulong plain = Random64();
                    ulong cipher = _des.EncryptCustom(plain, ks, rounds: 3, withIP: false, withFP: false);

                    // Split plain/cipher
                    uint PH = (uint)(plain >> 32);
                    uint PL = (uint)(plain & 0xFFFFFFFF);
                    uint CH = (uint)(cipher >> 32);
                    uint CL = (uint)(cipher & 0xFFFFFFFF);

                    // Equation bits
                    int lhs = ExtractBits(PH, bitPositions)
                            ^ ExtractBits(CH, bitPositions)
                            ^ GetBit(PL, 15)
                            ^ GetBit(CL, 15);

                    counter += lhs == 0 ? 1 : -1;
                }

                // Decide XOR(K1[22], K3[22]) = 0 if majority 0, else 1
                return counter >= 0;
            });
        }

        private static ulong Random64()
        {
            var buf = RandomNumberGenerator.GetBytes(8);
            return BitConverter.ToUInt64(buf, 0);
        }

        private static int ExtractBits(uint val, int[] positions)
        {
            int x = 0;
            foreach (var pos in positions)
                x ^= GetBit(val, pos);
            return x;
        }

        private static int GetBit(uint val, int pos)
        {
            return (int)((val >> (32 - pos)) & 1); // assuming 1-indexed MSB
        }
    }

}
