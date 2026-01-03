// Standard libraries
#include <string.h>

// Headers
#include "..\include\utils.h"
#include "..\include\present.h"
#include "..\include\block_ciphers_constants.h"

/* PRESENT S-box (4 bit values represented as 8 bit) 
    defined in its original paper for encryption. */
static const bit8 SBOX[16] = 
{
    0xC, 0x5, 0x6, 0xB,
    0x9, 0x0, 0xA, 0xD,
    0x3, 0xE, 0xF, 0x8,
    0x4, 0x7, 0x1, 0x2
};
// We define the SBOX explicitly as a constant 
// array for efficiency in lookups.

/* PRESENT Inverse-S-box (4 bit values represented as 8 bit) 
    defined in its original paper for decrytpion. */
static const bit8 SBOX_INV[16] = 
{
    0x5, 0xE, 0xF, 0x8,
    0xC, 0x1, 0x2, 0xD,
    0xB, 0x4, 0x6, 0x3,
    0x0, 0x7, 0x9, 0xA
};
// While the SBOX_INV could be computed programmatically, 
// we define it explicitly here for clarity.

/* Bit permutation table for PRESENT cipher.
   Bit i of the state is moved to bit position PBOX[i]. */
static const unsigned char PBOX[64] = {
     0, 16, 32, 48,  1, 17, 33, 49,
     2, 18, 34, 50,  3, 19, 35, 51,
     4, 20, 36, 52,  5, 21, 37, 53,
     6, 22, 38, 54,  7, 23, 39, 55,
     8, 24, 40, 56,  9, 25, 41, 57,
    10, 26, 42, 58, 11, 27, 43, 59,
    12, 28, 44, 60, 13, 29, 45, 61,
    14, 30, 46, 62, 15, 31, 47, 63
};

/* Inverse bit permutation table for PRESENT cipher.
   Bit i of the state is moved to bit position PBOX_INV[i]. */
static const unsigned char PBOX_INV[64] = {
     0,  4,  8, 12, 16, 20, 24, 28,
    32, 36, 40, 44, 48, 52, 56, 60,
     1,  5,  9, 13, 17, 21, 25, 29,
    33, 37, 41, 45, 49, 53, 57, 61,
     2,  6, 10, 14, 18, 22, 26, 30,
    34, 38, 42, 46, 50, 54, 58, 62,
     3,  7, 11, 15, 19, 23, 27, 31,
    35, 39, 43, 47, 51, 55, 59, 63
};
// While the PBOX_INV could also be computed programmatically, 
// we define it explicitly here for clarity.


/*  This method performs the bit permutation layer (pLayer) of the
    PRESENT cipher.
    Takes input as 64 bit integer and returns again 64 bit permuted integer.
    The permutation is performed using a static lookup table (PBOX),
    where bit i of the input state is moved to bit position PBOX[i]. */
static bit64 pLayer(bit64 state)
{
    bit64 out = 0;
    for (int i = 0; i < 64; i++)
    {
        if ((state >> i) & BIT64_ONE)
        {
            out |= (BIT64_ONE << PBOX[i]);
        }
    }
    return out;
}

/*  This method performs the inverse bit permutation layer (pLayerInv)
    of the PRESENT cipher.
    Takes input as 64 bit integer and returns again 64 bit permuted integer.
    The inverse permutation is performed using a static lookup table
    (PBOX_INV), where bit i of the input state is moved to bit position
    PBOX_INV[i]. */
static bit64 pLayerInv(bit64 state)
{
    bit64 out = 0;
    for (int i = 0; i < 64; i++)
    {
        if ((state >> i) & BIT64_ONE)
        {
            out |= (BIT64_ONE << PBOX_INV[i]);
        }
    }
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
		// Although nib represents a 4 bit value, 
        // we use bit8 type for easy indexing.
        bit8 nib = (state >> (4 * i)) & NIBBLE_MASK;
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
        // Although nib represents a 4 bit value, 
        // we use bit8 type for easy indexing.
        bit8 nib = (state >> (4 * i)) & NIBBLE_MASK;
        bit64 s = SBOX_INV[nib];
        out |= (s << (4 * i));
    }
    return out;
}

/* This method converts top 8 bytes of 80 bit key register 
    (key[0..7]) to 64-bit round key. We construct the bit64 
    simply as follows from keys:
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

/* This method is the key schedule of PRESENT.
    The algorithm, produce 32 64 bit round keys from 80-bit
    key according to the scheme in the original paper. */
void present_key_schedule(const bit8 key80[10], bit64 roundKeys[32])
{
	// We shall work on a copy of the key register.
    bit8 k[10] = { 0 };
    memcpy(k, key80, 10);

	// Start counter from 1 to 32 for 32 rounds so that 
    // we can directly use it in the key update.
    for (int round = 1; round <= 32; round++) 
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

/* This method performs the single round in the encryption of PRESENT. */
bit64 present_single_round_encryption(bit64 state, bit64 roundKey)
{
	// Firstly, add round key.
    state ^= roundKey;

	// Apply substituion layer.
    state = sBoxLayer(state);

	// Apply permutation layer.
    state = pLayer(state);

	// Return updated state.
    return state;
}

/* This method performs the single round in the decryption of PRESENT. */
bit64 present_single_round_decryption(bit64 state, bit64 roundKey)
{
    // Apply inverse permutation layer.
    state = pLayerInv(state);

    // Apply inverse substituion layer.
    state = sBoxLayerInv(state);

    // Finally, add round key.
    state ^= roundKey;

    // Return updated state.
    return state;
}

/* This method performs the encryption of a single 64 bit block 
    given with initial state as plaintext and the 32 many round keys. */
bit64 present_encrypt_block(bit64 state, const bit64 roundKeys[32])
{
	// Perform 31 rounds.
    for (int r = 0; r < 31; r++)
        state = present_single_round_encryption(state, roundKeys[r]);

	// Perform last round without sBoxLayer and pLayer.
    state ^= roundKeys[31];

    // Return ciphertext.
    return state;
}

/* This method performs the decryption of a single 64 bit block
    given with initial state as plaintext and the 32 many round keys. */
bit64 present_decrypt_block(bit64 state, const bit64 roundKeys[32])
{
    // Perform inverse of final whitening.
    state ^= roundKeys[31];

	// Perform 31 inverse rounds.
    for (int r = 30; r >= 0; r--)
        state = present_single_round_decryption(state, roundKeys[r]);
    
	// Return plaintext.
    return state;
}

/* This method performs PRESENT encryption in CBC mode. 
    The plaintext is padded plaintext bytes. The length is padded 
    length, which must be multiple of 8. The roundKeys are 32 round keys.
    The iv is 8 byte IV. Fianlly, the ciphertext is output buffer. */
void present_cbc_encrypt(const bit8* plaintext, int length,
    const bit64 roundKeys[32], const bit8 iv[8], bit8* ciphertext)
{
	// In CBC mode the IV as 64-bit value can be set prev initially.
    bit64 prev = load64(iv);  

    for (int i = 0; i < length; i += 8)
    {
        // We load the plaintext block.
        bit64 block = load64(&plaintext[i]);

        // We apply CBC XOR (64 bit).
        bit64 state = block ^ prev;

        // We apply encrypt block.
        bit64 enc = present_encrypt_block(state, roundKeys);

        // We store the ciphertext to next round.
        save64(enc, &ciphertext[i]);

        // We finally update prev for next block.
        prev = enc;
    }
}

/* This method performs PRESENT decryption in CBC mode.
    The ciphertext is padded ciphertext bytes. The length is padded
    length, which must be multiple of 8. The roundKeys are 32 round keys.
    The iv is 8 byte IV. Finally, the plaintext is output buffer. */
void present_cbc_decrypt(const bit8* ciphertext, int length,
    const bit64 roundKeys[32], const bit8 iv[8], bit8* plaintext)
{
    // In CBC mode the IV as 64-bit value can be set prev initially.
    bit64 prev = load64(iv);

    for (int i = 0; i < length; i += 8)
    {
        // We load the ciphertext block.
        bit64 cblock = load64(&ciphertext[i]);

        // We apply decrypt block as reverse of encryption.
        bit64 dec = present_decrypt_block(cblock, roundKeys);

        // We apply CBC XOR (64 bit) to obtain plaintext.
        bit64 pblock = dec ^ prev;

        // We store the plaintext block.
        save64(pblock, &plaintext[i]);

        // We finally update prev for next ciphertext block.
        prev = cblock;
    }
}
