#ifndef PRESENT_H

#define PRESENT_H

#include "block_ciphers_constants.h"

/* This method is the key schedule of PRESENT.
	The algorithm, produce 32 64 bit round keys from 80-bit
	key according to the scheme in the original paper. */
void present_key_schedule(const bit8 key80[10], bit64 roundKeys[32]);

/* This method performs the single round in the encryption of present cipher. */
bit64 present_single_round_encryption(bit64 state, bit64 roundKey);

/* This method performs the single round in the decryption of PRESENT. */
bit64 present_single_round_decryption(bit64 state, bit64 roundKey);

/* This method performs the encryption of a single 64 bit block
	given with initial state as plaintext and the 32 many round keys. */
bit64 present_encrypt_block(bit64 state, const bit64 roundKeys[32]);

/* This method performs the decryption of a single 64 bit block
	given with initial state as plaintext and the 32 many round keys. */
bit64 present_decrypt_block(bit64 state, const bit64 roundKeys[32]);

/* This method performs PRESENT encryption in CBC mode.
	The plaintext is padded plaintext bytes. The length is padded
	length, which must be multiple of 8. The roundKeys are 32 round keys.
	The iv is 8 byte IV. Fianlly, the ciphertext is output buffer. */
void present_cbc_encrypt(const bit8* plaintext, int length,
	const bit64 roundKeys[32], const bit8 iv[8], bit8* ciphertext);

/* This method performs PRESENT decryption in CBC mode.
	The ciphertext is padded ciphertext bytes. The length is padded
	length, which must be multiple of 8. The roundKeys are 32 round keys.
	The iv is 8 byte IV. Finally, the plaintext is output buffer. */
void present_cbc_decrypt(const bit8* ciphertext, int length,
	const bit64 roundKeys[32], const bit8 iv[8], bit8* plaintext);

#endif
