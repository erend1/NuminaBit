using System.Collections;

namespace NuminaBit.Services.Ciphers.DES.Entities
{
    public sealed class Substitutions : IReadOnlyList<int[,]>
    {
        private int[,] _s1 = new int[4, 16];
        private int[,] _s2 = new int[4, 16];
        private int[,] _s3 = new int[4, 16];
        private int[,] _s4 = new int[4, 16];
        private int[,] _s5 = new int[4, 16];
        private int[,] _s6 = new int[4, 16];
        private int[,] _s7 = new int[4, 16];
        private int[,] _s8 = new int[4, 16];

        public int[,] S1
        {
            get => _s1;
            set => _s1 = ValidateSBox(value, nameof(S1));
        }

        public int[,] S2
        {
            get => _s2;
            set => _s2 = ValidateSBox(value, nameof(S2));
        }

        public int[,] S3
        {
            get => _s3;
            set => _s3 = ValidateSBox(value, nameof(S3));
        }

        public int[,] S4
        {
            get => _s4;
            set => _s4 = ValidateSBox(value, nameof(S4));
        }

        public int[,] S5
        {
            get => _s5;
            set => _s5 = ValidateSBox(value, nameof(S5));
        }

        public int[,] S6
        {
            get => _s6;
            set => _s6 = ValidateSBox(value, nameof(S6));
        }

        public int[,] S7
        {
            get => _s7;
            set => _s7 = ValidateSBox(value, nameof(S7));
        }

        public int[,] S8
        {
            get => _s8;
            set => _s8 = ValidateSBox(value, nameof(S8));
        }

        public int[,] this[int index] => index switch
        {
            0 => S1,
            1 => S2,
            2 => S3,
            3 => S4,
            4 => S5,
            5 => S6,
            6 => S7,
            7 => S8,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        public int Count => 8;

        public IEnumerator<int[,]> GetEnumerator()
        {
            yield return S1;
            yield return S2;
            yield return S3;
            yield return S4;
            yield return S5;
            yield return S6;
            yield return S7;
            yield return S8;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Simple validation for a DES S-box: must be non-null and 4x16 dimensions.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private static int[,] ValidateSBox(int[,] value, string propertyName)
        {
            if (value == null)
                throw new ArgumentNullException(propertyName, "S-box cannot be null");

            if (value.GetLength(0) != 4 || value.GetLength(1) != 16)
                throw new ArgumentException(
                    $"S-box {propertyName} must have dimensions 4×16. " +
                    $"Received: {value.GetLength(0)}×{value.GetLength(1)}",
                    propertyName);

            return value;
        }

        /// <summary>
        /// Convert one DES S-box from 4x16 form to flat 64-length array.
        /// </summary>
        /// <param name="sbox2d">S-box as [4,16] array</param>
        /// <returns>Flat array length 64 where index = row*16 + col</returns>
        public static int[] ToFlat(int[,] sbox2d)
        {
            ValidateSBox(sbox2d, nameof(sbox2d));
            var flat = new int[64];
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 16; c++)
                {
                    flat[r * 16 + c] = sbox2d[r, c];
                }
            }
            return flat;
        }
    }
}
