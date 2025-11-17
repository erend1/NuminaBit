#ifndef PRESENT_CONSTANTS_H

#define PRESENT_CONSTANTS_H

#include <stdint.h> 

// ====================================================================
//					PRESENT CONSTANTS FOR READABILITY
// ====================================================================

/* The type represents 8 bit integer. */
typedef uint8_t bit8;

/* The type represents 16 bit integer. */
typedef uint16_t bit16;

/* The type represents 32 bit integer. */
typedef uint32_t bit32;

/* The type represents 64 bit integer. */
typedef uint64_t bit64;

/* Simple macro for rotation of X to 8 bit left. */
#define ROTL8(x,n) (bit8)(((x) << (n)) | ((x) >> (8 - (n))))

/* Represents the maximum 64 bit unsigned value (0xFULL). */
#define U64_FULL_MASK 0xFFFFFFFFFFFFFFFFULL

/* Represents the full mask for a single 8 bit byte (0xFULL). */
#define BIT4_FULL_MASK 0xFULL

/* Represents the mask for the lower (least significant) 4 bits, or a "nibble" (0x0FU). */
#define NIBBLE_MASK 0x0FU

/* Represents the 64 bit unsigned value 1 (1ULL). */
#define BIT64_ONE 1ULL

/* Represents the 8 bit unsigned value 1 (0x01U). */
#define BIT8_ONE 0x01U

// Bonus: Other common constants you might need in PRESENT
#define U64_ZERO 0ULL
#define BITS_IN_NIBBLE 4U
#define BITS_IN_BYTE 8U
#define BLOCK_SIZE_BITS 64U

#endif
