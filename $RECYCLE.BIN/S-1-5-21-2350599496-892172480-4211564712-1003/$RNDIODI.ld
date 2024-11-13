INPUT(boot.ao long_mode.ao boot.o output.o)
/* let the linker use its 'native' format (ELF/COFF/PE)
OUTPUT_FORMAT("elf32") */
/* no leading underscore for symbols handled in asm: */
ENTRY(start)
LS_Phys = 0x400000; /* 4 meg = load (physical) address */
LS_Virt = 0x400000; /* 4 meg = virtual address */
SECTIONS
{
    .text LS_Virt : AT(LS_Phys)
	{
		LS_Code = .;
		/* symbols to mark start of code segment */
		code = .; _code = .;
		/* kernel code */
		*(.text)
		/* .rodata is the ELF constant data section */
		*(.rodata*)
		. = ALIGN(4096);
	}	

    .data : AT(LS_Phys + (LS_Data - LS_Code))
	{
		LS_Data = .;
		/* symbols to mark start of data segment */
		data = .; _data = .;
		/* kernel data */
		*(.data)
		. = ALIGN(4096);
	}

	.bss : AT(LS_Phys + (LS_Bss - LS_Code))
	{
		LS_Bss = .;
		/* symbols to mark start of BSS segment */
		bss = .; _bss = .;
		/* kernel BSS */
		*(.bss)
		*(COMMON) /* "common" variables */
		. = ALIGN(4096);
	}

	/* symbols to mark end of kernel */
    end = .; _end = .;
}