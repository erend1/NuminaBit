using Microsoft.JSInterop;
using NuminaBit.Web.App.Entities;
using Microsoft.AspNetCore.Components;
using NuminaBit.Services.Ciphers.DES.Entities;
using Microsoft.AspNetCore.Components.Rendering;

namespace NuminaBit.Web.App.Components.DesVisualizer
{
    public partial class OverviewPanel : ComponentBase
    {
        [Parameter] public RunInfo? Snapshots { get; set; }
        [Parameter] public bool ShowBinary { get; set; }
        [Parameter] public EventCallback<BitTag> OnBitClick { get; set; }
        [Parameter] public BitTag? Selected { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            var run = Snapshots;
            if (run is null)
            {
                b.OpenElement(0, "div");
                b.AddContent(1, "Henüz çalıştırılmadı.");
                b.CloseElement();
                return;
            }

            int seq = 0;
            b.OpenElement(seq++, "div");
            b.AddAttribute(seq++, "class", "space-y-2");

            // IP, L0, R0
            b.AddMarkupContent(seq++, Row("IP", run.IPOut, 64, OnBitClick, Selected));
            b.AddMarkupContent(seq++, Row("L0", run.Rounds[0].L, 32, OnBitClick, Selected));
            b.AddMarkupContent(seq++, Row("R0", run.Rounds[0].R, 32, OnBitClick, Selected));

            for (int i = 1; i <= 16; i++)
            {
                var rs = run.Rounds[i];
                b.AddMarkupContent(seq++, Row($"L{i}", rs.L, 32, OnBitClick, Selected));
                b.AddMarkupContent(seq++, Row($"R{i}", rs.R, 32, OnBitClick, Selected));
            }

            // FP ve nihai
            b.AddMarkupContent(seq++, Row("FP", run.FPIn, 64, OnBitClick, Selected));
            b.AddMarkupContent(seq++, Row("C", run.FinalCipher, 64, OnBitClick, Selected));

            b.CloseElement();
        }

        private static string Row(string label, ulong bits, int width, EventCallback<BitTag> onClick, BitTag? sel)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"<div class=\"row-line\"><div class=\"row-label\">{label}</div><div class=\"row-bits\">");
            for (int i = 0; i < width; i++)
            {
                int idx = width - 1 - i; // MSB solda
                int bit = (bits & 1UL << idx) != 0 ? 1 : 0;
                var tag = new BitTag(label, idx);
                var key = System.Text.Json.JsonSerializer.Serialize(tag);
                var selClass = sel.HasValue && sel.Value.Equals(tag) ? " bit-sel" : string.Empty;
                sb.Append($"<span class=\"bit{selClass}\" onclick=\"Blazor.emit('bitClick',{key})\">{bit}</span>");
            }
            sb.Append("</div></div>");
            return sb.ToString();
        }

        [JSInvokable("bitClick")]
        public static Task JsBitClick(BitTag tag) => s_last?.OnBitClick.InvokeAsync(tag) ?? Task.CompletedTask;

        static OverviewPanel? s_last;
        protected override void OnAfterRender(bool firstRender)
        {
            s_last = this;
        }
    }
}
