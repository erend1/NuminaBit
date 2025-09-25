using NuminaBit.Services.Ciphers.DES.Interfaces;
using NuminaBit.Console.Services.Shared.Intefaces;

namespace NuminaBit.Console.Services.Shared
{
    public class SecondAlgorithmExamples(ISecondAlgorithm attack): ISecondAlgorithmExamples
    {
        private readonly ISecondAlgorithm _attack = attack;

        public async Task Example1()
        {
            var successes = 0;
            for (int i = 0; i < 100; i++)
            {
                var a = await _attack.RunSingleAsync(0x133456798BBCDFF1UL, 1 >> 20, 128, 2048, 10000);
            }
            System.Console.WriteLine($"Done Example1 : {successes}");
            System.Console.ReadLine();
        }

        public async Task Example2()
        {
            await Task.Delay(1);

                     int GetBit(uint val, int pos)
        {
            // pos: 0..31 LSB-first
            return (int)((val >> pos) & 1UL);
        }

         string ToBinaryString(ulong v, int width)
        {
            var sb = new System.Text.StringBuilder(width);
            for (int i = 0; i < width; i++)
            {
                int bit = (int)((v >> (width - 1 - i)) & 1UL);
                sb.Append(bit == 1 ? '1' : '0');
                if ((i + 1) % 8 == 0 && i < width - 1) sb.Append(' ');
            }
            return sb.ToString();
        }

            int a = 21;
            var aq = ToBinaryString((ulong) a, 64);
            var b = GetBit(21, 0);
            var c = GetBit(21, 1);
            var d = GetBit(21, 2);
            var e = GetBit(21, 3);
            var f = GetBit(21, 4);
        }
    }
}
