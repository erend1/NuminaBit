// pride.c
#include <string.h>
#include <stdio.h>
#include "pride.h"
#include "present_constants.h"

/* reuse helpers (load64, save64, etc.) */
#include "utils.c"

/* PRIDE S-box (4-bit) — as in the paper (Appendix I / Section 5.3). */
static const bit8 PRIDE_SBOX[16] = {
    0x0, 0x4, 0x8, 0xF, 0x1, 0x5, 0xE, 0x9,
    0x2, 0x7, 0xA, 0xC, 0xB, 0xD, 0x6, 0x3
};

/* Compute inverse S-box (since S is an involution we could reuse, but doing general compute) */
static bit8 PRIDE_SBOX_INV[16];
static void init_sbox_inv(void)
{
    for (int i = 0;i < 16;i++) PRIDE_SBOX_INV[(int)PRIDE_SBOX[i]] = (bit8)i;
}

/* Nibble permutation P := P16_{1,1,1,1}
   We implement it as a nibble-level permutation:
   P maps nibble index i -> (i % 4) * 4 + (i / 4)
   (this groups all first bits of each S-box, then second bits, ... --
    this implementation matches the interleaving described in the paper).
*/
static const int P_NIBBLE_MAP[16] = {
    /* i -> (i%4)*4 + (i/4) */
    0, 4, 8, 12,
    1, 5, 9, 13,
    2, 6, 10,14,
    3, 7, 11,15
};
static int P_NIBBLE_MAP_INV[16];
static void init_pmap_inv(void)
{
    for (int i = 0;i < 16;i++) P_NIBBLE_MAP_INV[P_NIBBLE_MAP[i]] = i;
}

/* Helper: apply nibble permutation P to 64-bit (by nibble).
   We consider nibble 0 as the least-significant nibble (bits 0..3) and nibble 15 as most-significant nibble.
*/
static bit64 pLayer_nibble(bit64 state)
{
    bit64 out = 0;
    for (int i = 0; i < 16; ++i)
    {
        bit8 nib = (state >> (4 * i)) & NIBBLE_MASK;
        int pos = P_NIBBLE_MAP[i];
        out |= ((bit64)nib << (4 * pos));
    }
    return out;
}
static bit64 pLayer_nibble_inv(bit64 state)
{
    bit64 out = 0;
    for (int i = 0; i < 16; ++i)
    {
        bit8 nib = (state >> (4 * i)) & NIBBLE_MASK;
        int pos = P_NIBBLE_MAP_INV[i];
        out |= ((bit64)nib << (4 * pos));
    }
    return out;
}

/* The four 16x16 matrices L0..L3 from Appendix G. We store each row as a 16-bit mask:
   row_mask[r] has bit j = 1 if matrix entry at (r, j) == 1.
   NOTE: these matrices were transcribed from Appendix G of the PRIDE paper.
*/
static const uint16_t L0_rows[16] = {
    /* row 0..15 */
    /* row0: ones at 4,8,12 */   (1u << 4) | (1u << 8) | (1u << 12),
    /* row1: 5,9,13 */           (1u << 5) | (1u << 9) | (1u << 13),
    /* row2: 6,10,14 */          (1u << 6) | (1u << 10) | (1u << 14),
    /* row3: 7,11,15 */          (1u << 7) | (1u << 11) | (1u << 15),
    /* row4: 0,8,12 */           (1u << 0) | (1u << 8) | (1u << 12),
    /* row5: 1,9,13 */           (1u << 1) | (1u << 9) | (1u << 13),
    /* row6: 2,10,14 */          (1u << 2) | (1u << 10) | (1u << 14),
    /* row7: 3,11,15 */          (1u << 3) | (1u << 11) | (1u << 15),
    /* row8: 0,4,12 */           (1u << 0) | (1u << 4) | (1u << 12),
    /* row9: 1,5,13 */           (1u << 1) | (1u << 5) | (1u << 13),
    /* row10:2,6,14 */           (1u << 2) | (1u << 6) | (1u << 14),
    /* row11:3,7,15 */           (1u << 3) | (1u << 7) | (1u << 15),
    /* row12:0,4,8 */            (1u << 0) | (1u << 4) | (1u << 8),
    /* row13:1,5,9 */            (1u << 1) | (1u << 5) | (1u << 9),
    /* row14:2,6,10 */           (1u << 2) | (1u << 6) | (1u << 10),
    /* row15:3,7,11 */           (1u << 3) | (1u << 7) | (1u << 11)
};

static const uint16_t L1_rows[16] = {
    /* transcribed from Appendix G */
    (1u << 0) | (1u << 1) | (1u << 11),
    (1u << 1) | (1u << 2) | (1u << 12),
    (1u << 2) | (1u << 3) | (1u << 13),
    (1u << 3) | (1u << 4) | (1u << 14),
    (1u << 4) | (1u << 5) | (1u << 15),
    (1u << 5) | (1u << 6) | (1u << 8),
    (1u << 6) | (1u << 7) | (1u << 9),
    (1u << 0) | (1u << 7) | (1u << 10),
    (1u << 0) | (1u << 11) | (1u << 12),
    (1u << 1) | (1u << 12) | (1u << 13),
    (1u << 2) | (1u << 13) | (1u << 14),
    (1u << 3) | (1u << 14) | (1u << 15),
    (1u << 4) | (1u << 8) | (1u << 15),
    (1u << 5) | (1u << 8) | (1u << 9),
    (1u << 6) | (1u << 9) | (1u << 10),
    (1u << 7) | (1u << 10) | (1u << 11)
};

static const uint16_t L2_rows[16] = {
    /* transcribed from Appendix G */
    (1u << 4) | (1u << 5) | (1u << 15),
    (1u << 5) | (1u << 6) | (1u << 8),
    (1u << 6) | (1u << 7) | (1u << 9),
    (1u << 0) | (1u << 7) | (1u << 10),
    (1u << 0) | (1u << 1) | (1u << 11),
    (1u << 1) | (1u << 2) | (1u << 12),
    (1u << 2) | (1u << 3) | (1u << 13),
    (1u << 3) | (1u << 4) | (1u << 14),
    (1u << 4) | (1u << 8) | (1u << 15),
    (1u << 5) | (1u << 8) | (1u << 9),
    (1u << 6) | (1u << 9) | (1u << 10),
    (1u << 7) | (1u << 10) | (1u << 11),
    (1u << 0) | (1u << 11) | (1u << 12),
    (1u << 1) | (1u << 12) | (1u << 13),
    (1u << 2) | (1u << 13) | (1u << 14),
    (1u << 3) | (1u << 14) | (1u << 15)
};

static const uint16_t L3_rows[16] = {
    /* transcribed from Appendix G */
    (1u << 0) | (1u << 4) | (1u << 12),
    (1u << 1) | (1u << 5) | (1u << 13),
    (1u << 2) | (1u << 6) | (1u << 14),
    (1u << 3) | (1u << 7) | (1u << 15),
    (1u << 0) | (1u << 4) | (1u << 8),
    (1u << 1) | (1u << 5) | (1u << 9),
    (1u << 2) | (1u << 6) | (1u << 10),
    (1u << 3) | (1u << 7) | (1u << 11),
    (1u << 4) | (1u << 8) | (1u << 12),
    (1u << 5) | (1u << 9) | (1u << 13),
    (1u << 6) | (1u << 10) | (1u << 14),
    (1u << 7) | (1u << 11) | (1u << 15),
    (1u << 0) | (1u << 8) | (1u << 12),
    (1u << 1) | (1u << 9) | (1u << 13),
    (1u << 2) | (1u << 10) | (1u << 14),
    (1u << 3) | (1u << 11) | (1u << 15)
};

/* Apply a 16x16 matrix given by rows[] to a 16-bit input vector (bits 0..15 are inputs).
   Output is 16-bit where bit r = parity(rows[r] & input).
*/
static uint16_t apply_matrix_16(const uint16_t rows[16], uint16_t in)
{
    uint16_t out = 0;
    for (int r = 0; r < 16; ++r)
    {
        unsigned int v = rows[r] & in;
        /* parity of v: use builtin popcount */
        unsigned int bit = __builtin_popcount(v) & 1u;
        out |= (uint16_t)(bit << r);
    }
    return out;
}

/* Linear layer L:
   - compute state_p = P(state)  (nibble permutation)
   - extract bit-planes plane[j] (j=0..3): plane[j] is 16-bit vector where bit i = bit j of nibble i
   - apply Li to plane[j] using apply_matrix_16
   - reassemble nibbles from resulting planes
   - apply P^{-1} on reassembled state
*/
static bit64 linear_layer_L(bit64 state)
{
    bit64 sp = pLayer_nibble(state);

    /* extract planes */
    uint16_t plane[4] = { 0,0,0,0 };
    for (int i = 0;i < 16;i++)
    {
        bit8 nib = (sp >> (4 * i)) & 0xF;
        for (int b = 0;b < 4;b++)
        {
            if ((nib >> b) & 1) plane[b] |= (1u << i);
        }
    }

    /* apply Li (plane 0 -> L0, plane1->L1, ...) */
    uint16_t outp0 = apply_matrix_16(L0_rows, plane[0]);
    uint16_t outp1 = apply_matrix_16(L1_rows, plane[1]);
    uint16_t outp2 = apply_matrix_16(L2_rows, plane[2]);
    uint16_t outp3 = apply_matrix_16(L3_rows, plane[3]);

    /* reassemble nibble state (still in permuted positions) */
    bit64 sprime = 0;
    for (int i = 0;i < 16;i++)
    {
        bit8 nib = 0;
        nib |= ((outp0 >> i) & 1u) << 0;
        nib |= ((outp1 >> i) & 1u) << 1;
        nib |= ((outp2 >> i) & 1u) << 2;
        nib |= ((outp3 >> i) & 1u) << 3;
        sprime |= ((bit64)nib << (4 * i));
    }

    /* final inverse permutation */
    return pLayer_nibble_inv(sprime);
}

/* For decryption we need inverse matrices. In Appendix G L0 and L3 are involutions
   (their own inverse) and the rows for inverses of L1/L2 are provided in the paper
   (we transcribed using the same arrays where needed). For simplicity and correctness
   here we implement inverse by applying the transpose approach:
   For a binary invertible matrix M (16x16) with rows rows[], its inverse matrix's rows_inv[]
   satisfy rows_inv[r][c] such that M * M_inv = I. The easiest way here (safe and simple)
   is to compute M^{-1} at init-time (over F2) by Gaussian elimination on 16x16 bits.
   We'll build inverse rows for L0..L3 at init time.
*/

/* Gaussian elimination over F2 for 16x16 to compute inverse rows */
static void invert_matrix_16(const uint16_t rows[16], uint16_t inv_rows[16])
{
    /* We interpret rows[r] as 16-bit row vector (col 0..15). We build augmented matrix [A|I]. */
    uint32_t mat[16]; /* low 16 bits: A row; high 16 bits: I row (initially identity) */
    for (int r = 0;r < 16;r++)
    {
        mat[r] = ((uint32_t)rows[r]) | ((uint32_t)(1u << r) << 16);
    }

    /* Forward elimination */
    for (int col = 0; col < 16; ++col)
    {
        /* find pivot row with bit col = 1 at or below row=col */
        int pivot = -1;
        for (int r = col; r < 16; ++r) if ((mat[r] >> col) & 1u) { pivot = r; break; }
        if (pivot == -1)
        {
            /* not invertible (shouldn't happen for PRIDE matrices) */
            /* set identity fallback */
            for (int i = 0;i < 16;i++) inv_rows[i] = (1u << i);
            return;
        }
        /* swap pivot row into position col */
        if (pivot != col) { uint32_t tmp = mat[col]; mat[col] = mat[pivot]; mat[pivot] = tmp; }

        /* eliminate other rows */
        for (int r = 0;r < 16;r++) if (r != col && ((mat[r] >> col) & 1u)) mat[r] ^= mat[col];
    }

    /* now left 16x16 is identity; right 16x16 is inverse rows (row r stored in bits 16..31) */
    for (int r = 0;r < 16;r++) inv_rows[r] = (uint16_t)((mat[r] >> 16) & 0xFFFFu);
}

/* Precomputed inverse-matrix rows */
static uint16_t L0_inv_rows[16];
static uint16_t L1_inv_rows[16];
static uint16_t L2_inv_rows[16];
static uint16_t L3_inv_rows[16];
static int PRIDE_INIT_DONE = 0;
static void pride_init_once(void)
{
    if (PRIDE_INIT_DONE) return;
    init_sbox_inv();
    init_pmap_inv();
    invert_matrix_16(L0_rows, L0_inv_rows);
    invert_matrix_16(L1_rows, L1_inv_rows);
    invert_matrix_16(L2_rows, L2_inv_rows);
    invert_matrix_16(L3_rows, L3_inv_rows);
    PRIDE_INIT_DONE = 1;
}

/* linear_layer inverse (apply inverse matrices in reverse */
static bit64 linear_layer_L_inv(bit64 state)
{
    bit64 sp = pLayer_nibble(state);

    /* extract planes */
    uint16_t plane[4] = { 0,0,0,0 };
    for (int i = 0;i < 16;i++)
    {
        bit8 nib = (sp >> (4 * i)) & 0xF;
        for (int b = 0;b < 4;b++)
        {
            if ((nib >> b) & 1) plane[b] |= (1u << i);
        }
    }

    /* apply inverse Li (plane 0 -> L0^{-1}, ...) */
    uint16_t outp0 = apply_matrix_16(L0_inv_rows, plane[0]);
    uint16_t outp1 = apply_matrix_16(L1_inv_rows, plane[1]);
    uint16_t outp2 = apply_matrix_16(L2_inv_rows, plane[2]);
    uint16_t outp3 = apply_matrix_16(L3_inv_rows, plane[3]);

    /* reassemble nibble state (still in permuted positions) */
    bit64 sprime = 0;
    for (int i = 0;i < 16;i++)
    {
        bit8 nib = 0;
        nib |= ((outp0 >> i) & 1u) << 0;
        nib |= ((outp1 >> i) & 1u) << 1;
        nib |= ((outp2 >> i) & 1u) << 2;
        nib |= ((outp3 >> i) & 1u) << 3;
        sprime |= ((bit64)nib << (4 * i));
    }

    /* final inverse permutation -> original order: apply inverse of P on the sprime (but sprime is in permuted positions)
       To get the original pre-image of the permuted representation, we must apply P^{-1} here:
    */
    return pLayer_nibble_inv(sprime);
}

/* S-box layer: apply 16 independent 4-bit S-boxes (nibble-wise) */
static bit64 sbox_layer(bit64 state)
{
    bit64 out = 0;
    for (int i = 0;i < 16;i++)
    {
        bit8 nib = (state >> (4 * i)) & NIBBLE_MASK;
        bit8 s = PRIDE_SBOX[nib];
        out |= ((bit64)s << (4 * i));
    }
    return out;
}
static bit64 sbox_layer_inv(bit64 state)
{
    bit64 out = 0;
    for (int i = 0;i < 16;i++)
    {
        bit8 nib = (state >> (4 * i)) & NIBBLE_MASK;
        bit8 s = PRIDE_SBOX_INV[nib];
        out |= ((bit64)s << (4 * i));
    }
    return out;
}

/* Key schedule:
   key128: 16 bytes -> k0 = key[0..7], k1 = key[8..15]
   subkey for round i (1-based) is fi(k1): bytes:
     rk[0] = k1[0]
     rk[1] = (k1[1] + 193*i) mod 256
     rk[2] = k1[2]
     rk[3] = (k1[3] + 165*i) mod 256
     rk[4] = k1[4]
     rk[5] = (k1[5] + 81*i) mod 256
     rk[6] = k1[6]
     rk[7] = (k1[7] + 197*i) mod 256
   This matches the paper's fi definition and constants. (Appendix / Section 5.4)
*/
void pride_key_schedule(const bit8 key128[16], bit64 roundKeys[20],
    bit64* out_k0, bit64* out_k2)
{
    pride_init_once();

    bit8 k0_bytes[8], k1_bytes[8];
    memcpy(k0_bytes, key128, 8);
    memcpy(k1_bytes, key128 + 8, 8);

    /* pre/post whitening */
    *out_k0 = load64(k0_bytes);
    *out_k2 = *out_k0; /* k2 = k0 */

    for (int round = 1; round <= 20; ++round)
    {
        bit8 rk[8];
        rk[0] = k1_bytes[0];
        rk[1] = (bit8)((k1_bytes[1] + (193u * round)) & 0xFFu);
        rk[2] = k1_bytes[2];
        rk[3] = (bit8)((k1_bytes[3] + (165u * round)) & 0xFFu);
        rk[4] = k1_bytes[4];
        rk[5] = (bit8)((k1_bytes[5] + (81u * round)) & 0xFFu);
        rk[6] = k1_bytes[6];
        rk[7] = (bit8)((k1_bytes[7] + (197u * round)) & 0xFFu);
        roundKeys[round - 1] = load64(rk);
    }
}

/* PRIDE encryption:
   Overall structure (as in paper):
     state = plaintext XOR k0  (pre-whitening)
     state = P(state)
     for i = 1..20:
       state ^= roundKey_i
       state = SboxLayer(state)
       if i != 20:
         state = L(state)   // linear layer absent in final round
     state = P^{-1}(state)
     ciphertext = state XOR k2  (post-whitening)
*/
bit64 pride_encrypt_block(bit64 plaintext,
    const bit64 roundKeys[20],
    bit64 k0, bit64 k2)
{
    pride_init_once();

    bit64 state = plaintext ^ k0;
    state = pLayer_nibble(state);

    for (int r = 0; r < 20; ++r)
    {
        state ^= roundKeys[r];
        state = sbox_layer(state);
        if (r != 19) state = linear_layer_L(state);
    }

    state = pLayer_nibble_inv(state);
    state ^= k2;
    return state;
}

/* Decryption: reverse order, using inverse linear layers and inverse permutation.
   Because S-box is involutive (and we built SBOX_INV), we can apply inverse easily.
*/
bit64 pride_decrypt_block(bit64 ciphertext,
    const bit64 roundKeys[20],
    bit64 k0, bit64 k2)
{
    pride_init_once();

    bit64 state = ciphertext ^ k2;
    state = pLayer_nibble(state); /* NOTE: symmetric: apply P (because encryption final had P^{-1}) */

    for (int r = 19; r >= 0; --r)
    {
        if (r != 19) state = linear_layer_L_inv(state);
        state = sbox_layer_inv(state);
        state ^= roundKeys[r];
    }

    state = pLayer_nibble_inv(state);
    state ^= k0;
    return state;
}
