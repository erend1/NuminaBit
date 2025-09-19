using NuminaBit.Services.Ciphers.DES.Entities;
using NuminaBit.Services.Ciphers.Shared.Entities;

namespace NuminaBit.Web.App.Pages
{
    public partial class LatAnalyzer
    {
        private sealed record SBoxState(string Text, int Value);

        // S-box options.
        private readonly static List<SBoxState> SBoxOptions = [
            new("S1",0), new("S2",1), new("S3",2), new("S4",3),
            new("S5",4), new("S6",5), new("S7",6), new("S8",7),
            new("Custom", 8)
        ];

        // State
        private int SelectedSBoxIndex { get; set; } = 0;
        private string SelectedSBoxValue => (SBoxOptions.FirstOrDefault(x => x.Value == SelectedSBoxIndex) ?? SBoxOptions[0]).Text;

        // S-box as 64-length int array (each value 0..15)
        private readonly int[] SboxTable = new int[64];
        private int[,]? NsTable; // [alpha 0..63, beta 0..15]
        private CellDetailModel? SelectedCell;
        private bool ShowNSMinus32 { get; set; } = false;

        protected override void OnInitialized()
        {
            ResetToDefault();
        }

        private void ResetToDefault()
        {
            // load S1..S8 from your DesHelpers.SBOX array if available; fallback sample identity
            try
            {
                var sBoxFlat = Substitutions.ToFlat(_des.Substitutions[0]);

                if (sBoxFlat != null)
                {
                    Array.Copy(sBoxFlat, SboxTable, Math.Min(sBoxFlat.Length, 64));
                    SelectedSBoxIndex = 0;
                }
            }
            catch
            {
                // fallback: identity mapping 0..15 repeated
                for (int i = 0; i < 64; i++) SboxTable[i] = i & 0xF;
                SelectedSBoxIndex = 8;
            }
            NsTable = null;
            SelectedCell = null;
        }

        private void LoadFromHelpers()
        {
            try
            {
                if (SelectedSBoxIndex >= 0 && SelectedSBoxIndex < 8)
                {
                    var sBoxFlat = Substitutions.ToFlat(_des.Substitutions[SelectedSBoxIndex]);
                    if (sBoxFlat != null)
                    {
                        Array.Copy(sBoxFlat, SboxTable, 64);
                    }
                }
                NsTable = null;
                SelectedCell = null;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "LoadFromHelpers failed");
            }
        }

        private void OnSboxCellChange(int idx, string? val)
        {
            if (int.TryParse(val, out int v) && v >= 0 && v <= 15)
            {
                SboxTable[idx] = v;
            }
            else
            {
                // ignore invalid
            }
            NsTable = null;
            SelectedCell = null;
        }

        private Task ComputeLAT()
        {
            try
            {
                NsTable = _lat.ComputeNsTable(SboxTable);
                SelectedCell = null;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "ComputeLAT failed");
            }

            return Task.CompletedTask;
        }

        private void OpenCellDetail(int alpha, int beta)
        {
            try
            {
                if (NsTable == null)
                    return;

                var rows = _lat.GetCellRows(SboxTable, alpha, beta);
                SelectedCell = new CellDetailModel
                {
                    Alpha = alpha,
                    Beta = beta,
                    Count = NsTable[alpha, beta],
                    Rows = rows
                };
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "ComputeLAT failed");
            }
        }

        private static string To4Bits(int v) => Convert.ToString(v & 0xF, 2).PadLeft(4, '0');
    }
}
