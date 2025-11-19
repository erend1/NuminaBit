#include <stdio.h>
#include "present_constants.h"

// ================================================================
//			        PRESENT HELPER METHODS
// ================================================================

// This method is a helper to print hex.
static void print_u64_hex(bit64 v)
{
    printf("%016llx", (unsigned long long)v);
}

// This method prints byte array in hex.
static void print_hex(const bit8* data, int len)
{
    for (int i = 0; i < len; i++)
        printf("%02x", data[i]);
}

/* This method removes Turkish-specific characters for ASCII convertion. */
static char normalize_char(char c)
{
    switch (c)
    {
        case 0x00E7: return 'c';  // 'ç'
        case 0x00C7: return 'C';  // 'Ç'
        case 0x011F: return 'g';  // 'ğ'
        case 0x011E: return 'G';  // 'Ğ'
        case 0x0131: return 'i';  // 'ı'
        case 0x0130: return 'I';  // 'İ'
        case 0x00F6: return 'o';  // 'ö'
        case 0x00D6: return 'O';  // 'Ö'
        case 0x015F: return 's';  // 'ş'
        case 0x015E: return 'S';  // 'Ş'
        case 0x00FC: return 'u';  // 'ü'
        case 0x00DC: return 'U';  // 'Ü'
        default: return c;
    }
}

/* This method simply converts a single char to its ASCII byte representation. */
static bit8 get_ascii_char(char c)
{
    return (bit8) c;
}

/* This method takes input as array of char object 
    and an byte array with max length threshold to convert ASCII 
    chars by normalizing the input and append to byte array. */
static int ascii_to_bytes(const char* text, bit8 out[], int maxlen)
{
    int count = 0;
    while (*text && count < maxlen)
    {
        out[count++] = get_ascii_char(normalize_char(*text));
        text++;
    }
    return count;
}

/* This method performs the standard padding to the data. */
static int standard_padding(uint8_t* data, int length)
{
	// Add 1 bit to indicate end of data.
    data[length] = BIT8_ONE << 7;
    length++;

    // Add 0 until next 8-byte boundary.
    while (length % 8 != 0)
    {
        data[length] = 0;
        length++;
    }

    return length;
}

// This method is a simple helper to convert 8 bytes from array to 64 bit value.
static bit64 load64(const bit8* p)
{
    bit64 v = 0;
    for (int i = 0; i < 8; i++)
        v = (v << 8) | p[i];
    return v;
}

// This method is a simple helper to stores 64 bit in 8 bytes.
static void save64(bit64 v, bit8* out)
{
    for (int i = 7; i >= 0; i--)
    {
        out[i] = v & BIT8_MASK;
        v = v >> 8;
    }
}
