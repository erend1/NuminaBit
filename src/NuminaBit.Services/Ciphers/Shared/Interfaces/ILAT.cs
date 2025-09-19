using NuminaBit.Services.Ciphers.Shared.Entities;

namespace NuminaBit.Services.Ciphers.Shared.Interfaces
{
    public interface ILAT
    {
        /// <summary>
        /// Compute NS table: returns int[alpha(0..63), beta(0..15)] of counts (0..64).
        /// Sbox must be 64-length array mapping z (0..63) -> 4-bit output (0..15).
        /// </summary>
        int[,] ComputeNsTable(int[] sbox);

        /// <summary>
        /// Get detail rows for a single (alpha,beta) cell: list of 64 rows describing z,S(z),alpha·z,beta·S(z),isMatch.
        /// </summary>
        List<CellRow> GetCellRows(int[] sbox, int alpha, int beta);
    }
}
