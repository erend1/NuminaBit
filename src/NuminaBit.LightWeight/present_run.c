#include <stdio.h>
#include <stdint.h>
#include <string.h>

#include "utils.c"
#include "present.h"

// Test vectors from Appendix I of paper.
static void run_paper_test_vectors(void)
{
    /* each test vector: plaintext (8 bytes), key(10 bytes), expected ciphertext (8 bytes) */
    struct tv { bit8 pt[8]; bit8 key[10]; bit8 ct[8]; };

    struct tv tests[4] = {
        /* plaintext = 0x0000000000000000, key = 0x00000000000000000000 */
        {{0,0,0,0,0,0,0,0}, {0,0,0,0,0,0,0,0,0,0}, {0x55,0x79,0xC1,0x38,0x7B,0x22,0x84,0x45}},
        /* plaintext = 0, key = 0xFFFFFFFFFFFFFFFFFFFF (80-bit all ones) */
        {{0,0,0,0,0,0,0,0}, {0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}, {0xE7,0x2C,0x46,0xC0,0xF5,0x94,0x50,0x49}},
        /* plaintext = 0xFFFFFFFFFFFFFFFF, key = 0x00000000000000000000 */
        {{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}, {0,0,0,0,0,0,0,0,0,0}, {0xA1,0x12,0xFF,0xC7,0x2F,0x68,0x41,0x7B}},
        /* both all ones */
        {{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}, {0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}, {0x33,0x33,0xDC,0xD3,0x21,0x32,0x10,0xD2}}
    };

    int count = sizeof(tests) / sizeof(tests[0]);
    printf("Running PRESENT-80 official test vectors (%d vectors)\n", count);

    for (int i = 0; i < count; ++i) {
        bit64 rk[32];
        present_key_schedule(tests[i].key, rk);

        /* load plaintext into bit64 (big-endian bytes -> MSB first)
           match the library expectation: state is assembled as big-endian:
           state = pt[0]<<56 | pt[1]<<48 | ... pt[7]<<0
        */
        bit64 pt = 0;
        for (int b = 0; b < 8; ++b) pt = (pt << 8) | tests[i].pt[b];

        bit64 ct = present_encrypt_block(pt, rk);

        /* expected from vector */
        bit64 expected = 0;
        for (int b = 0; b < 8; ++b) expected = (expected << 8) | tests[i].ct[b];

        printf("Test %2d: ", i + 1);
        printf(" Plaintext = ");
        print_u64_hex(pt);
        printf(" Key = ");
        for (int k = 0; k < 10; ++k) 
            printf("%02X", tests[i].key[k]);
        printf(" -> Output = ");
        print_u64_hex(ct);
        printf(" Expected = ");
        print_u64_hex(expected);
        printf(" %s\n", (ct == expected) ? "Status = OK" : "Status = FAIL");
    }
    printf("\n");
}

// Test vectors from our homework question 1.
static void run_homework_test_vectors(void)
{
    bit64 plaintext = 0;
	bit8 key[10] = { 0 };
    bit64 roundKeys[32] = { 0 };
    present_key_schedule(key, roundKeys);

    printf("\n----------\n");
    printf("Running PRESENT-80 homework 0 test vectors with each 32 rounds.");
    printf("\n----------\n");

	bit64 state = plaintext;
    for (int r = 0; r < 31; r++) 
    {
        state = present_single_round(state, roundKeys[r]);
        printf("Round %2d: ", r + 1);
        printf("\n");
        printf("Key: ");
        print_u64_hex(roundKeys[r]);
        printf("\n");
        printf("State: ");
        print_u64_hex(state);
        printf("\n----------\n");
    }
    // Perform last round (without sBoxLayer and pLayer)
    printf("Last Round");
    printf("\n");
    printf("Key: ");
    print_u64_hex(roundKeys[31]);
    printf("\n");
    printf("Ciphertext: ");
    bit64 ciphertext = state ^ roundKeys[31];
    print_u64_hex(ciphertext);    
}

// Testing ASCII convertion and padding 
// method froum our homework question 2.
static void run_ascii_padding_example(void) 
{
    printf("\n----------\n");
    printf("ASCII and Padding Test");
    printf("\n----------\n");

    // Example full name
    const char* fullname = u8"Cihangir Tezcan";

    // Convert to ASCII bytes
    bit8 plain[256] = { 0 };
    int len = ascii_to_bytes(fullname, plain, 256);

    printf("Original text: ");
    printf(fullname);
    printf("\n----------\n");

    printf("ASCII: ");
    print_hex(plain, len);
    printf("\n----------\n");

	len = standard_padding(plain, len);
    printf("Padded: ");
    print_hex(plain, len);
    printf("\n----------\n");

    // For the key example, we simply take 
    // "Cihangir Tezcan" first 10 bytes so we have 80 bit key.
    printf("Key in hex: ");
    bit8 key80[10] = {
        normalize_char('C'), // 1
        normalize_char('i'), // 2
        normalize_char('h'), // 3
        normalize_char('a'), // 4
        normalize_char('n'), // 5
        normalize_char('g'), // 6
        normalize_char('i'), // 7
        normalize_char('r'), // 8
        normalize_char(' '), // 9
        normalize_char('T'), // 10
    };
    print_hex(key80, 10);
}

// Test method from our homework question 2.
static void run_present_cbc_example(void)
{
    printf("\n----------\n");
    printf("PRESENT CBC Mode With Full Name");
    printf("\n----------\n");

	// Our name with Turkish characters.
    const char* fullname = "Hüseyin Eren Demirtaş";

    // We first convert to ASCII bytes.
    bit8 plain[256];
    int len = ascii_to_bytes(fullname, plain, sizeof(plain));

    printf("Original text: ");
    printf(fullname);
    printf("\n----------\n");

    printf("ASCII: ");
    print_hex(plain, len);
    printf("\n----------\n");

    len = standard_padding(plain, len);
    printf("Padded: ");
    print_hex(plain, len);
    printf("\n----------\n");

    // For the key example, we simply take 
    // "Hüseyin Eren Demirtaş" first 10 bytes so we have 80 bit key.
    printf("Key hex: ");
    bit8 key80[10] = {
        normalize_char('H'), // 1
		normalize_char('ü'), // 2
		normalize_char('s'), // 3
		normalize_char('e'), // 4
		normalize_char('y'), // 5
		normalize_char('i'), // 6
		normalize_char('n'), // 7
		normalize_char(' '), // 8
		normalize_char('E'), // 9
		normalize_char('r'), // 10
    };
    print_hex(key80, 10);
    printf("\n----------\n");

    // Random IV (simple): fixed example, but you can replace with rand()
    bit8 iv[8];
    bit8 exampleIV[8] = { 0x12, 0x34, 0x56, 0x78, 0xAB, 0xCD, 0xEF, 0x01 };
    memcpy(iv, exampleIV, 8);
    printf("IV: ");
    print_hex(iv, 8);
    printf("\n----------\n");

    // Prepare key schedule
    bit64 roundKeys[32];
    present_key_schedule(key80, roundKeys);

    // Output ciphertext buffer 
    bit8 ciphertext[256];
    present_cbc_encrypt(plain, len, roundKeys, iv, ciphertext);
    printf("Ciphertext (CBC): ");
    print_hex(ciphertext, len);
    printf("\n----------\n");
	bit8 decrypted[256];
	present_cbc_decrypt(ciphertext, len, roundKeys, iv, decrypted);
    print_hex(decrypted, len);
}

int main(void)
{
    //run_paper_test_vectors();
    //run_homework_test_vectors();
    //run_ascii_padding_example();
    run_present_cbc_example();
    return 0;
}
