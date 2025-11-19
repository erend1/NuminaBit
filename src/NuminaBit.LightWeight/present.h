#ifndef PRESENT_H

#define PRESENT_H

#include "present_constants.h"

/* Public API (library-style)
   - present_key_schedule: produce 32 round keys (64-bit each) from 80-bit key (10 bytes)
   - present_encrypt_block: encrypt single 64-bit block with precomputed round keys
   - present_decrypt_block: (optional) decrypt a block (simple implementation included)
*/

/* key80: 10 bytes, key80[0] = MSB byte (k79..k72), key80[9] = LSB byte (k7..k0) */
void present_key_schedule(const bit8 key80[10], bit64 roundKeys[32]);

/* This method performs the single round in the encryption of present cipher. */
bit64 present_single_round(bit64 state, bit64 roundKey);

/* Encrypt a single 64-bit block in place.
	state is a 64-bit word with bit 0 = least significant bit.
	roundKeys must be produced by present_key_schedule. */
bit64 present_encrypt_block(bit64 state, const bit64 roundKeys[32]);

/* This method performs PRESENT encryption in CBC mode. The plaintext is padded plaintext bytes.
   The length is padded length, which must be multiple of 8. The roundKeys are 32 round keys.
   The iv is 8-byte IV. Finally, the ciphertext is output buffer. */
void present_cbc_encrypt(const bit8* plaintext, int length,
	const bit64 roundKeys[32], const bit8 iv[8], bit8* ciphertext);

/* Decrypt a single 64-bit block in place (works with roundKeys from present_key_schedule).
	(Provided for completeness; it uses inverse S-box and inverse permutation.) */
bit64 present_decrypt_block(bit64 state, const bit64 roundKeys[32]);

void present_cbc_decrypt(const bit8* ciphertext, int length,
	const bit64 roundKeys[32],
	const bit8 iv[8],
	bit8* plaintext);

#endif
