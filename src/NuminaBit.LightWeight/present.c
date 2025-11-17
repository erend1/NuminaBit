// Headers
#include "present.h"
#include "present_constants.h"

// Libraries
#include <string.h>

/* PRESENT S-box (4 bit values represented as 8 bit) defined in its original paper for encryption. */
static const bit8 SBOX[16] = 
{
    0xC, 0x5, 0x6, 0xB,
    0x9, 0x0, 0xA, 0xD,
    0x3, 0xE, 0xF, 0x8,
    0x4, 0x7, 0x1, 0x2
};
// We define the SBOX explicitly as a constant array for efficiency in lookups.

/* PRESENT Inverse-S-box (4 bit values represented as 8 bit) defined in its original paper for decrytpion. */
static const bit8 SBOX_INV[16] = 
{
    0x5, 0xE, 0xF, 0x8,
    0xC, 0x1, 0x2, 0xD,
    0xB, 0x4, 0x6, 0x3,
    0x0, 0x7, 0x9, 0xA
};
// While the SBOX_INV could be computed programmatically, 
// we define it explicitly here for clarity.

/*  This method performs the bit permutation denoted as P(i). 
    Takes input as 64 bit integer and returns again 64 bit permuted integer. 
    The method uses the compact formula (instead of a large lookup defined in the original paper) which is defined as follows:
        for i = 0..62 do P(i) = (16*i) % 63, and set P(63) = 63. */
static bit64 pLayer(bit64 state)
{
    bit64 out = 0;
    for (int i = 0; i < 63; i++) 
    {
        if ((state >> i) & BIT64_ONE) 
        {
            int pos = (16 * i) % 63;
            out |= (BIT64_ONE << pos);
        }
    }
    /* bit 63 maps to itself */
    if ((state >> 63) & BIT64_ONE) 
        out |= (BIT64_ONE << 63);
    return out;
}

/* This method performs the substitution denoted as S(w_i).
   Applies 4 bit S-box (SBOX lookup defined above) to each 
   16 many nibbles defined as follows:
        w_i = b_{4∗i+3} || b_{4∗i+2} || b_{4∗i+1}|| b_{b4∗i}. */
static bit64 sBoxLayer(bit64 state)
{
    bit64 out = 0;
    for (int i = 0; i < 16; i++) 
    {
		// Although nib represents a 4 bit value, we use bit8 type for easy indexing.
        bit8 nib = (state >> (4 * i)) & BIT4_FULL_MASK;
        bit64 s = SBOX[nib];
        out |= (s << (4 * i));
    }
    return out;
}

/* This method performs the inverse substitution denoted as S^[-1}(w_i).
   Applies 4 bit Inv-S-box (SBOX_INV lookup defined above) to each
   16 many nibbles defined as follows:
       w_i = b_{4∗i+3} || b_{4∗i+2} || b_{4∗i+1}|| b_{b4∗i}. */
static bit64 sBoxLayerInv(bit64 state)
{
    bit64 out = 0;
    for (int i = 0; i < 16; i++) 
    {
        // Although nib represents a 4 bit value, we use bit8 type for easy indexing.
        bit8 nib = (state >> (4 * i)) & BIT4_FULL_MASK;
        bit64 s = SBOX_INV[nib];
        out |= (s << (4 * i));
    }
    return out;
}

/* This method converts top 8 bytes of 80 bit key register (key[0..7]) to 64-bit round key.
   We construct the bit64 simply as follows from keys
       bit64 = key[0] | key[1] | key[2] | ... | key[7] */
static bit64 key_bytes_to_bit64(const bit8 k[10])
{
    bit64 r = 0;
    for (int i = 0; i < 8; i++) 
        r = (r << 8) | k[i];
    return r;
}

/* This method rotates the 80 bit key register k left by 61 bits. 
   Although we could do this by converting array to bit64, 
   for the sake of the input output of the key schedule 
   we simply procede by updating the array elements. 
   In the method, we use the following formula 
   that exaclty computes the rotated entries:
   K[i] = ((K[(i+7)%10] << 5) | (K[(i+8)%10] >> 3))
*/
static void rotate_key_left61(bit8 k[10])
{
    bit8 tmp[10] = { 0 };
    for (int i = 0; i < 10; i++) 
    {
        bit8 a = k[(i + 7) % 10];
        bit8 b = k[(i + 8) % 10];
        tmp[i] = ((a << 5) | (b >> 3));
    }
    memcpy(k, tmp, 10);
}

/* This method is the key schedule of PRESENT cipher (80 bit key size version). 
   Tha algorithm, produce 32 64 bit round keys from 80-bit key according to the scheme in the original paper. */
void present_key_schedule(const bit8 key80[10], bit64 roundKeys[32])
{
	// We shall work on a copy of the key register.
    bit8 k[10] = { 0 };
    memcpy(k, key80, 10);

	// Start counter from 1 to 32 for 32 rounds so that we can directly use it in the key update.
    for (int round = 1; round <= 32; ++round) 
    {
        // We simply set round key is leftmost 64 bits 
        // of the current state of the registery in the round.
        roundKeys[round - 1] = key_bytes_to_bit64(k);

        /* Key update procedure for next round:
           1) rotate left 61 bits
           2) apply S-box to leftmost 4 bits
           3) XOR round counter into bits k19..k15 */

		// 1) We rotate left 61 bits.
        rotate_key_left61(k);

        // 2) We apply S-box to leftmost 4 bits (k_79, k_78, k_77, k_76).
        bit8 top_nibble = (k[0] >> 4) & NIBBLE_MASK;
        top_nibble = SBOX[top_nibble];
        k[0] = (top_nibble << 4) | (k[0] & NIBBLE_MASK);

        // 3) XOR round counter i (round) into bits k19..k15.
        bit8 rc = (bit8)round;
        k[7] ^= (rc >> 1) & NIBBLE_MASK;
        k[8] ^= (rc & BIT8_ONE) << 7;
    }
}

/* This method performs the single round in the encryption of present cipher. */
bit64 present_single_round(bit64 state, bit64 roundKey)
{
	// Add round key.
    state ^= roundKey;
	// Apply substituion layer.
    state = sBoxLayer(state);
	// Apply permutation layer.
    state = pLayer(state);
	// Return updated state.
    return state;
}

/* This method performs the encryption of a single 64 bit block 
    given with initial state as plaintext and the 32 many round keys. */
bit64 present_encrypt_block(bit64 state, const bit64 roundKeys[32])
{
	// perform 31 rounds
    for (int r = 0; r < 31; r++)
        state = present_single_round(state, roundKeys[r]);
	// Perform last round (without sBoxLayer and pLayer)
    state ^= roundKeys[31];
    // Return ciphertext.
    return state;
}

/* Decryption (inverse operations) */
static bit64 pLayerInv(bit64 state)
{
    /* inverse permutation: compute forward mapping and invert */
    bit64 out = 0;
    for (int i = 0; i < 63; i++) 
    {
        /* forward: i -> (16*i)%63, so inverse is solve j such that (16*j)%63 == i
           but easiest: for j=0..62, if bit j is set in input then put at (16*j)%63 in out.
           For inverse we reverse: if bit at position p in input, find j such that (16*j)%63 == p
        */
        /* Simpler: iterate over all source positions and move them to original pos */
    }
    /* Simpler method: brute-force invert */
    bit64 tmp = 0;
    for (int i = 0; i < 63; i++) {
        if ((state >> ((16 * i) % 63)) & BIT64_ONE) {
            tmp |= (BIT64_ONE << i);
        }
    }
    if ((state >> 63) & BIT64_ONE) tmp |= (BIT64_ONE << 63);
    return tmp;
}

bit64 present_decrypt_block(bit64 state, const bit64 roundKeys[32])
{
    /* inverse of final whitening */
    state ^= roundKeys[31];
    for (int r = 30; r >= 0; --r) {
        state = pLayerInv(state);
        state = sBoxLayerInv(state);
        state ^= roundKeys[r];
    }
    return state;
}
