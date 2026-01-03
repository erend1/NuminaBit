#include <stdio.h>

void run_present_demo();

int main() {
    printf("Select Cipher:\n1. Present\n2. Pride\n3. Ascon\n> ");
    int choice = getchar();

    switch (choice) {
    case 49: run_present_demo(); break;
    default: printf("Invalid choice.\n");
    }

    printf("\nPress Enter to exit...");
    int temp = getchar();

    return 0;
}