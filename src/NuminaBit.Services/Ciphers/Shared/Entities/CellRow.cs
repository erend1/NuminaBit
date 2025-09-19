using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuminaBit.Services.Ciphers.Shared.Entities
{
    public sealed class CellRow
    {
        public int Z { get; set; }
        public int AlphaDot { get; set; }
        public int SOut { get; set; }
        public int BetaDot { get; set; }
        public bool IsMatch { get; set; }
    }
}
