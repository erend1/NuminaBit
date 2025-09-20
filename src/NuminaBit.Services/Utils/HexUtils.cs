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

        public static ulong Parse64(string hex)
        {
            return TryParse64(hex, out ulong v) ? v : throw new ArgumentException("Invalid hex value.");
        }

        public static string ToHex64(ulong v) => v.ToString("X16");

        public static string ToBinaryString(ulong v, int bits)
        {
            if (bits < 1 || bits > 64) throw new ArgumentOutOfRangeException(nameof(bits), "Bits must be between 1 and 64");
            char[] arr = new char[bits];
            for (int i = 0; i < bits; i++)
            {
                arr[bits - 1 - i] = ((v & (1UL << i)) != 0) ? '1' : '0';
            }
            return new string(arr);
        }

        public static string Random64Hex()
        {
            Span<byte> b = stackalloc byte[8];
            Rng.NextBytes(b);
            ulong v = BitConverter.ToUInt64(b);
            return ToHex64(v);
        }

        public static byte[] ToBytes(string hex)
        {
            hex = hex?.Trim()?.Replace(" ", "") ?? string.Empty;
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
            if (hex.Length % 2 != 0) throw new ArgumentException("Hex string must have an even length", nameof(hex));
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                string byteValue = hex.Substring(i * 2, 2);
                bytes[i] = Convert.ToByte(byteValue, 16);
            }
            return bytes;
        }
    }
}
