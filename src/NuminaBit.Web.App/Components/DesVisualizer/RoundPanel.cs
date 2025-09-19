using NuminaBit.Web.App.Entities;
using Microsoft.AspNetCore.Components;
using NuminaBit.Services.Ciphers.DES.Entities;
using Microsoft.AspNetCore.Components.Rendering;

namespace NuminaBit.Web.App.Components.DesVisualizer
{
    public partial class RoundPanel : ComponentBase
    {
        [Parameter] public RunInfo? Snapshots { get; set; }
        [Parameter] public int StepIndex { get; set; }
        [Parameter] public EventCallback<BitTag> OnBitClick { get; set; }
        [Parameter] public BitTag? Selected { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            int seq = 0;
            if (Snapshots is null)
            {
                b.AddContent(seq++, "Henüz çalışma yapılmadı.");
                return;
            }

            int r = Math.Clamp(StepIndex, 0, 16);
            var rs = Snapshots.Rounds[r];

            b.OpenElement(seq++, "div");
            b.AddAttribute(seq++, "class", "round-panel");
            b.AddMarkupContent(seq++, $"<div class=\"round-title\">Tur {r}</div>");

            // Kolonlar: L,R | E(R) | XOR K | SBox | P | L⊕F
            b.AddMarkupContent(seq++, BitsCol($"L{r}", rs.L, 32));
            b.AddMarkupContent(seq++, BitsCol($"R{r}", rs.R, 32));

            if (r > 0)
            {
                b.AddMarkupContent(seq++, BitsCol("E(R)", rs.ER, 48));
                b.AddMarkupContent(seq++, BitsCol("K", Snapshots.KeySchedule.SubKeys[r - 1], 48));
                b.AddMarkupContent(seq++, BitsCol("E⊕K", rs.EXorK, 48));
                b.AddMarkupContent(seq++, BitsCol("SboxOut", rs.SBoxOut, 32));
                b.AddMarkupContent(seq++, BitsCol("P", rs.PermOut, 32));
                b.AddMarkupContent(seq++, BitsCol($"L{r - 1}⊕F", rs.LXorF, 32));
            }

            b.CloseElement();
        }

        private static string BitsCol(string label, ulong val, int width)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"<div class=\"col\"><div class=\"col-label\">{label}</div><div class=\"col-bits\">");
            for (int i = 0; i < width; i++)
            {
                int idx = width - 1 - i;
                int bit = (val & 1UL << idx) != 0 ? 1 : 0;
                sb.Append($"<span class=\"bit\">{bit}</span>");
            }
            sb.Append("</div></div>");
            return sb.ToString();
        }
    }
}
