#include <stdio.h>
#include <stdint.h>
#include <string.h>
#include "present.h"

/* helpers to print hex */
static void print_u64_hex(uint64_t v)
{
    printf("%016llx", (unsigned long long)v);
}

/* test vectors from Appendix I of paper */
static void run_paper_test_vectors(void)
{
    /* each test vector: plaintext (8 bytes), key(10 bytes), expected ciphertext (8 bytes) */
    struct tv { uint8_t pt[8]; uint8_t key[10]; uint8_t ct[8]; };

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
        uint64_t rk[32];
        present_key_schedule(tests[i].key, rk);

        /* load plaintext into uint64_t (big-endian bytes -> MSB first)
           match the library expectation: state is assembled as big-endian:
           state = pt[0]<<56 | pt[1]<<48 | ... pt[7]<<0
        */
        uint64_t pt = 0;
        for (int b = 0; b < 8; ++b) pt = (pt << 8) | tests[i].pt[b];

        uint64_t ct = present_encrypt_block(pt, rk);

        /* expected from vector */
        uint64_t expected = 0;
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

/* test vectors from our homework */
static void run_homework_test_vectors(void)
{
    uint64_t plaintext = 0;
	uint8_t key[10] = { 0 };
    uint64_t roundKeys[32] = { 0 };
    present_key_schedule(key, roundKeys);

    printf("Running PRESENT-80 homework 0 test vectors with each 32 rounds.");
    printf("\n----------\n");

	uint64_t state = plaintext;
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
    uint64_t ciphertext = state ^ roundKeys[31];
    print_u64_hex(ciphertext);    
}

int main(void)
{
    run_paper_test_vectors();
    //run_homework_test_vectors();
    return 0;
}
