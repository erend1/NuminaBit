using NuminaBit.Web.App.Entities;
using Microsoft.AspNetCore.Components;
using NuminaBit.Services.Ciphers.DES.Entities;
using Microsoft.AspNetCore.Components.Rendering;

namespace NuminaBit.Web.App.Components.DesVisualizer
{
    public partial class KeySchedulePanel : ComponentBase
    {
        [Parameter] public KeySchedule? Sched { get; set; }
        [Parameter] public bool ShowBinary { get; set; }
        [Parameter] public EventCallback<BitTag> OnBitClick { get; set; }
        [Parameter] public BitTag? Selected { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            int seq = 0;
            if (Sched is null)
            {
                b.AddContent(seq++, "Henüz hesaplanmadı.");
                return;
            }

            b.OpenElement(seq++, "div");
            b.AddAttribute(seq++, "class", "space-y-2");
            b.AddMarkupContent(seq++, Row("PC-1", Sched.PC1Out, 56));

            for (int i = 0; i < 16; i++)
            {
                b.AddMarkupContent(seq++, Row($"K{i + 1}", Sched.SubKeys[i], 48));
            }
            b.CloseElement();
        }

        private static string Row(string label, ulong bits, int width)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"<div class=\"row-line\"><div class=\"row-label\">{label}</div><div class=\"row-bits\">");
            for (int i = 0; i < width; i++)
            {
                int idx = width - 1 - i;
                int bit = (bits & 1UL << idx) != 0 ? 1 : 0;
                sb.Append($"<span class=\"bit\">{bit}</span>");
            }
            sb.Append("</div></div>");
            return sb.ToString();
        }
    }
}
