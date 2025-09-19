namespace NuminaBit.Services.Utils
{
    public static class HexUtil
    {
        private static readonly Random Rng = new();
        public static bool TryParse64(string hex, out ulong value)
        {
            value = 0;
            hex = hex?.Trim()?.Replace(" ", "") ?? string.Empty;
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
            if (hex.Length != 16) return false;
            return ulong.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out value);
        }
        public static string ToHex64(ulong v) => v.ToString("X16");
        public static string Random64Hex()
        {
            Span<byte> b = stackalloc byte[8];
            Rng.NextBytes(b);
            ulong v = BitConverter.ToUInt64(b);
            return ToHex64(v);
        }
    }
}
