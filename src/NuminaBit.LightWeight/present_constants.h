#ifndef PRESENT_CONSTANTS_H

#define PRESENT_CONSTANTS_H

#include <stdint.h> 

// ================================================================
//			PRESENT CIPHER NOTATIONS FOR READABILITY
// ================================================================

/* The type represents 8 bit integer. */
typedef uint8_t bit8;

/* The type represents 16 bit integer. */
typedef uint16_t bit16;

/* The type represents 32 bit integer. */
typedef uint32_t bit32;

/* The type represents 64 bit integer. */
typedef uint64_t bit64;

/* Represents the mask for the lower (least significant) 4 bits, or a "nibble" (0x0FU). */
#define NIBBLE_MASK 0x0FU

/* Represents the mask for the 8 bits (0xFFU). */
#define BIT8_MASK 0xFFU

/* Represents the 64 bit unsigned value 1 (1ULL). */
#define BIT64_ONE 1ULL

/* Represents the 8 bit unsigned value 1 (0x01U). */
#define BIT8_ONE 0x01U

#endif
