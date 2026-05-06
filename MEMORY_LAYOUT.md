# uBasic Memory Layout (Byte Level)

This document describes the active in-memory layout used by the interpreter.

Source definitions:
- ubasic.h constants and struct definitions
- ubasic.c runtime placement in ubasic_init() and ubasic_load_program()

## 1) Fixed low-memory control block

These offsets are absolute from the start of the memory buffer (memory[0]).

- UBASIC_MEM_RESUME_OFFSET = 0
  - bytes 0..3: int32_t resume_offset
- UBASIC_MEM_GOSUB_DEPTH_OFFSET = 4
  - bytes 4..7: int32_t gosub_depth
- UBASIC_MEM_FOR_DEPTH_OFFSET = 8
  - bytes 8..11: int32_t for_depth
- UBASIC_MEM_DATA_PTR_OFFSET = 12
  - bytes 12..15: int32_t data_ptr
- UBASIC_MEM_GOSUB_STACK_OFFSET = 16
  - bytes 16..55: int32_t gosub_stack[10]
- UBASIC_MEM_FOR_STACK_OFFSET = 56
  - bytes 56..119: for_state for_stack[4]
- UBASIC_MEM_PUSH_DEPTH_OFFSET = 120
  - bytes 120..123: int32_t push_depth
- UBASIC_MEM_PUSH_STACK_OFFSET = 124
  - bytes 124..(UBASIC_MEM_PROGRAM_OFFSET - 1): push_entry push_stack[16]

With current defaults on MSVC x64:
- sizeof(for_state) = 16
- sizeof(push_entry) = 56
- UBASIC_MEM_PROGRAM_OFFSET = 1020

Note: UBASIC_MEM_PROGRAM_OFFSET is computed from sizeof(for_state) and sizeof(push_entry), so this value can vary by ABI/packing rules.

## 2) Program region

- Program bytes start at UBASIC_MEM_PROGRAM_OFFSET.
- Program is NUL-terminated.

If program length is L bytes (excluding trailing NUL):
- program_start = UBASIC_MEM_PROGRAM_OFFSET
- program_end = UBASIC_MEM_PROGRAM_OFFSET + L + 1

## 3) Dynamic heap placement

The interpreter uses a dynamic variable heap model.

At ubasic_load_program():
- heap_state is placed at program_end.
- var_desc_t table immediately follows heap_state.
- var_count starts at 0 (descriptors are allocated on first use).
- var_heap_top starts after the fixed descriptor table area.
- str_pool_base starts at mem_capacity_bytes and grows downward.

This means the free space between var_heap_top and str_pool_base is shared by:
- dynamic variable/array/string descriptors and array data (grow upward)
- string pool data (grows downward)

## 4) Key struct sizes (current)

- VARIABLE_TYPE = double
- UBASIC_VARIABLE_COUNT = 26 * 11 = 286
- sizeof(var_desc_t) = 11 (packed)
- UBASIC_VAR_TABLE_BYTES = 286 * 11 = 3146
- sizeof(heap_state_t) = 6 (packed)

## 5) Minimum memory requirement

Defined in ubasic.h:

- UBASIC_MIN_MEMORY_BYTES = UBASIC_MEM_PROGRAM_OFFSET + 256 + UBASIC_HEAP_STATE_SIZE + UBASIC_VAR_TABLE_BYTES + 512

With current defaults (MSVC x64 values above):

- UBASIC_MIN_MEMORY_BYTES = 1020 + 256 + 6 + 3146 + 512 = 4940 bytes

## 6) Practical summary

Low memory is fixed control state and stacks. Program text starts at UBASIC_MEM_PROGRAM_OFFSET. Heap metadata is placed right after the loaded program, and runtime allocations expand upward while the string pool expands downward from the top of the memory buffer.
