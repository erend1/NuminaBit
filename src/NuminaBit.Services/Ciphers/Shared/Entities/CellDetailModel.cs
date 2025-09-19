namespace NuminaBit.Services.Ciphers.Shared.Entities
{
    public sealed class CellDetailModel
    {
        public int Alpha { get; set; }
        public int Beta { get; set; }
        public int Count { get; set; }
        public List<CellRow> Rows { get; set; } = [];
    }
}
