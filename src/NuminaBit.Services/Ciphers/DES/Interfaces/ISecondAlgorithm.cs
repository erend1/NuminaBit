using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NuminaBit.Services.Ciphers.DES.SecondAlgorithmRunner;

namespace NuminaBit.Services.Ciphers.DES.Interfaces
{
    public interface ISecondAlgorithm
    {
        public ulong HiddenKey { get; }

        Task<Algorithm2Result> RunSingleAsync(ulong key64, int pairs, int topKPerEq, int maxCombined, int maxExhaustive);
    }
}
