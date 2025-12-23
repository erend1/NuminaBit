#ifndef PRIDE_H
#define PRIDE_H

#include "present_constants.h"

/* Key schedule:
   - key128: 16 bytes (k0 || k1), where k0 = key[0..7], k1 = key[8..15]
   - roundKeys: output array of 20 64-bit round keys (roundKeys[0]...roundKeys[19])
   - out_k0 and out_k2: pre/post-whitening 64-bit values (k2 = k0)
*/
void pride_key_schedule(const bit8 key128[16],
    bit64 roundKeys[20],
    bit64* out_k0, bit64* out_k2);

/* Encrypt / Decrypt single 64-bit blocks (ECB).
   They expect the roundKeys produced by pride_key_schedule and k0/k2. */
bit64 pride_encrypt_block(bit64 plaintext,
    const bit64 roundKeys[20],
    bit64 k0, bit64 k2);

bit64 pride_decrypt_block(bit64 ciphertext,
    const bit64 roundKeys[20],
    bit64 k0, bit64 k2);

#endif
