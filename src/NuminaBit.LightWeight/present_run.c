#include <time.h>
#include <stdio.h>
#include <stdint.h>
#include <string.h>

#include "utils.c"
#include "present.h"

// Test vectors from Appendix I of paper.
static void run_paper_test_vectors(void)
{
	// We simply define a structure to hold test vectors.
    struct tv 
    { 
        bit8 pt[8]; 
        bit8 key[10]; 
        bit8 ct[8]; 
    };
    const int count = 4;
    struct tv tests[4] =
    {
        { 
            {0,0,0,0,0,0,0,0}, // plaintext = 0x0000000000000000
            {0,0,0,0,0,0,0,0,0,0}, // key = 0x00000000000000000000
			{0x55,0x79,0xC1,0x38,0x7B,0x22,0x84,0x45} // expected ciphertext = 0x5579C1387B228445
        },
        {
            {0,0,0,0,0,0,0,0}, // plaintext = 0x0000000000000000
			{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}, // key = 0xFFFFFFFFFFFFFFFFFFFF
			{0xE7,0x2C,0x46,0xC0,0xF5,0x94,0x50,0x49} // expected ciphertext = 0xE72C46C0F5945049
        },
        {
			{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}, // plaintext = 0xFFFFFFFFFFFFFFFF
			{0,0,0,0,0,0,0,0,0,0}, // key = 0x00000000000000000000
			{0xA1,0x12,0xFF,0xC7,0x2F,0x68,0x41,0x7B} // expected ciphertext = 0xA112FFC72F68417B
        },
        {
			{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}, // plaintext = 0xFFFFFFFFFFFFFFFF
			{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}, // key = 0xFFFFFFFFFFFFFFFFFFFF
			{0x33,0x33,0xDC,0xD3,0x21,0x32,0x10,0xD2} // expected ciphertext = 0x3333DCD3213210D2
        }
    };

    printf("\n--------------------\n");
    printf("PRESENT-80 Official Test");
    printf("\n--------------------\n");

    for (int i = 0; i < count; i++) 
    {
		// We first generate the round keys from the test vector key.
        bit64 rk[32];
        present_key_schedule(tests[i].key, rk);

		// We load plaintext from test vector.
        bit64 pt = 0;
        for (int b = 0; b < 8; ++b) 
            pt = (pt << 8) | tests[i].pt[b];

		// Perform encryption.
        bit64 ct = present_encrypt_block(pt, rk);

		// We load expected ciphertext from test vector.
        bit64 expected = 0;
        for (int b = 0; b < 8; ++b) 
            expected = (expected << 8) | tests[i].ct[b];

		// Print results.
        printf("%2d) ", i + 1);

        printf("Plaintext = ");
        print_u64_hex(pt);

        printf(", Key = ");
        print_hex(tests[i].key, 10);

        printf(" -> Output = ");
        print_u64_hex(ct);

        printf(", Expected = ");
        print_u64_hex(expected);

        printf(" -> Status = %s\n", (ct == expected) ? "OK" : "FAIL");
    }
}

// Test vectors from our homework question 1.
static void run_homework_test_vectors(void)
{
    bit64 plaintext = 0;
	bit8 key[10] = { 0 };
    bit64 roundKeys[32] = { 0 };
    present_key_schedule(key, roundKeys);

    printf("\n---------------------------\n");
    printf("PRESENT-80 Homework Test Rounds");
    printf("\n---------------------------\n");

	bit64 state = plaintext;
    for (int r = 0; r < 31; r++) 
    {
        state = present_single_round_encryption(state, roundKeys[r]);
        printf("Round %2d: ", r + 1);
        printf("\n");
        printf("Key: ");
        print_u64_hex(roundKeys[r]);
        printf("\n");
        printf("State: ");
        print_u64_hex(state);
        printf("\n\n");
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
    printf("\n");
}

// Testing ASCII convertion and padding 
// method froum our homework question 2.
static void run_ascii_padding_example(void) 
{
    printf("\n------------------\n");
    printf("ASCII and Padding Test");
    printf("\n------------------\n");

    // Example full name
    const char* fullname = u8"Cihangir Tezcan";

    // Convert to ASCII bytes
    bit8 plain[256] = { 0 };
    int len = ascii_to_bytes(fullname, plain, 256);

    printf("Original text: ");
    printf(fullname);
    printf("\n");

    printf("ASCII: ");
    print_hex(plain, len);
    printf("\n");

	len = standard_padding(plain, len);
    printf("Padded: ");
    print_hex(plain, len);
    printf("\n");

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
    printf("\n");
}

// Test method from our homework question 2.
static void run_present_cbc_example(void)
{
    printf("\n-------------------------\n");
    printf("PRESENT-80 CBC Mode Full Name");
    printf("\n-------------------------\n");

	// Our name with Turkish characters.
    const char* fullname = "Hüseyin Eren Demirtaş";

    // We first convert to ASCII bytes.
    bit8 plain[256];
    int len = ascii_to_bytes(fullname, plain, sizeof(plain));

    printf("Original text: ");
    printf(fullname);
    printf("\n");

    printf("ASCII: ");
    print_hex(plain, len);
    printf("\n");

    len = standard_padding(plain, len);
    printf("Padded: ");
    print_hex(plain, len);
    printf("\n");

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
    printf("\n");

    // Random IV (simple): fixed example, but you can replace with rand()
    bit8 iv[8];
    bit8 exampleIV[8] = { 0x12, 0x34, 0x56, 0x78, 0xAB, 0xCD, 0xEF, 0x01 };
    memcpy(iv, exampleIV, 8);
    printf("IV: ");
    print_hex(iv, 8);
    printf("\n");

    // Prepare key schedule
    bit64 roundKeys[32];
    present_key_schedule(key80, roundKeys);

    // Output ciphertext buffer 
    bit8 ciphertext[256];
    present_cbc_encrypt(plain, len, roundKeys, iv, ciphertext);
    printf("Ciphertext (CBC): ");
    print_hex(ciphertext, len);
    printf("\n");
}

void test_present_speed_64mb(void)
{
    printf("\n---------------------------------\n");
    printf("PRESENT-80 Encryption Benchmark 64 MB");
    printf("\n---------------------------------\n");
    
	// 80-bit key as an example we use our student id: 0235865
    bit8 key80[10] = { 0, 2, 3, 5, 8, 6, 5, 3 };

    // Prepare our round keys
    bit64 roundKeys[32];
    present_key_schedule(key80, roundKeys);

    // Convert P to 64-bit
    bit64 block = 0;

    // Number of encryptions = 2^23 = 8.388.608
    const int ITERATIONS = 1 << 23;
    printf("Iterations: %d\n", ITERATIONS);

	// Length of the data in bits
    const int BITS = ITERATIONS * 64;
    printf("Total data: %d Bits\n", BITS);

	// Start timing
    clock_t start = clock();

    // Main loop to perform encryption on the same block repeatedly.
    for (int i = 0; i < ITERATIONS; i++)
    {
        block = present_encrypt_block(block, roundKeys);
    }

    // We force the compiler to execute the loop to get this value.
    printf("Final Result: ");
    print_u64_hex(block);
	printf("\n");

	// End timing
    clock_t end = clock();

    double seconds = (double)(end - start) / CLOCKS_PER_SEC;

    printf("Time used: %.6f seconds.\n", seconds);
    printf("CPU model: 11th Gen Intel(R) Core(TM) i5-1135G7 (2.42 GHz)\n");
}

#include "pride.h"
#include "present_constants.h"

static void test_pride_vector(const char* pt_hex, const char* k0_hex, const char* k1_hex, const char* ct_hex_expected)
{
    /* convert hex strings (16 hex chars -> 8 bytes little-endian? We'll parse as big-endian consistent with load64/save64)
       For convenience use sscanf to parse 64-bit hex values. */
    unsigned long long pt_v = 0, k0_v = 0, k1_v = 0, expected_v = 0;
    int ret_pt = sscanf_s(pt_hex, "%llx", &pt_v);
    if (ret_pt != 1) {
        printf("Error parsing pt_hex\n");
        return;
    }
    int ret_k0 = sscanf_s(k0_hex, "%llx", &k0_v);
    if (ret_k0 != 1) {
        printf("Error parsing k0_hex\n");
        return;
    }
    int ret_k1 = sscanf_s(k1_hex, "%llx", &k1_v);
    if (ret_k1 != 1) {
        printf("Error parsing k1_hex\n");
        return;
    }
    int ret_expected = sscanf_s(ct_hex_expected, "%llx", &expected_v);
    if (ret_expected != 1) {
        printf("Error parsing ct_hex_expected\n");
        return;
    }

/* Build key128 = k0 || k1 as bytes big-endian (most significant byte first) to match load64/save64 convention */
    bit8 key128[16];
    for (int i = 0;i < 8;i++) key128[i] = (bit8)((k0_v >> (8 * (7 - i))) & 0xFFu);
    for (int i = 0;i < 8;i++) key128[8 + i] = (bit8)((k1_v >> (8 * (7 - i))) & 0xFFu);

    bit64 roundKeys[20];
    bit64 k0, k2;
    pride_key_schedule(key128, roundKeys, &k0, &k2);

    /* plaintext as 64-bit */
    bit64 pt = (bit64)pt_v;
    /* if input endianness matters, ensure consistent use of load64/save64 in other APIs. Here we interpret everything big-endian consistently. */

    bit64 ct = pride_encrypt_block(pt, roundKeys, k0, k2);

    printf("PT: "); print_u64_hex_local(pt);
    printf(" KEY0: "); print_u64_hex_local(k0_v);
    printf(" KEY1: "); print_u64_hex_local(k1_v);
    printf(" -> CT: "); print_u64_hex_local(ct);
    printf("  (expected: "); print_u64_hex_local(expected_v); printf(")");
    if (ct == (bit64)expected_v) printf(" OK\n"); else printf(" FAIL\n");
}

void test_pride()
{
    /* Test vectors from Appendix J (paper). Format: plaintext, k0, k1, ciphertext */
    /* 1) all zero */
    test_pride_vector("0000000000000000", "0000000000000000", "0000000000000000", "82b4109fcc70bd1f");
    /* 2) plaintext all 1s */
    test_pride_vector("ffffffffffffffff", "0000000000000000", "0000000000000000", "d70e60680a17b956");
    /* 3) k0 zeros, k1 ones */
    test_pride_vector("0000000000000000", "ffffffffffffffff", "0000000000000000", "28f19f97f5e846a9");
    /* 4) k1 zeros, k2 ones */
    test_pride_vector("0000000000000000", "0000000000000000", "ffffffffffffffff", "d123ebaf368fce62");
    /* 5) sample */
    test_pride_vector("0123456789abcdef", "0000000000000000", "fedcba9876543210", "d1372929712d336e");

}


int main(void)
{
    run_paper_test_vectors();
    run_homework_test_vectors();
    run_ascii_padding_example();
    run_present_cbc_example();
	test_present_speed_64mb();
    return 0;
}
