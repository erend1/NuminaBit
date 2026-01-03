#ifndef UTILS_H
#define UTILS_H

#include "block_ciphers_constants.h"

// Prints a 64-bit value in hex
void print_u64_hex(bit64 v);

// Prints a byte array in hex
void print_hex(const bit8* data, int len);

// Normalizes Turkish characters and ASCII conversion
int ascii_to_bytes(const char* text, bit8 out[], int maxlen);

// Adds padding (1 followed by 0s)
int standard_padding(uint8_t* data, int length);

// Load/Save helpers
bit64 load64(const bit8* p);
void save64(bit64 v, bit8* out);

#endif