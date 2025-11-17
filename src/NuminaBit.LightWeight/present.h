#ifndef PRESENT_H

#define PRESENT_H

#include <stdint.h>

/* Public API (library-style)
   - present_key_schedule: produce 32 round keys (64-bit each) from 80-bit key (10 bytes)
   - present_encrypt_block: encrypt single 64-bit block with precomputed round keys
   - present_decrypt_block: (optional) decrypt a block (simple implementation included)
*/

/* key80: 10 bytes, key80[0] = MSB byte (k79..k72), key80[9] = LSB byte (k7..k0) */
void present_key_schedule(const uint8_t key80[10], uint64_t roundKeys[32]);

/* Encrypt a single 64-bit block in place.
	state is a 64-bit word with bit 0 = least significant bit.
	roundKeys must be produced by present_key_schedule. */
uint64_t present_encrypt_block(uint64_t state, const uint64_t roundKeys[32]);

/* Decrypt a single 64-bit block in place (works with roundKeys from present_key_schedule).
	(Provided for completeness; it uses inverse S-box and inverse permutation.) */
uint64_t present_decrypt_block(uint64_t state, const uint64_t roundKeys[32]);

/* This method performs the single round in the encryption of present cipher. */
uint64_t present_single_round(uint64_t state, uint64_t roundKey);

#endif
