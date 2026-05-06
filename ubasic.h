/*
 * Copyright (c) 2006, Adam Dunkels
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. Neither the name of the author nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 *
 */
 
 /*
 * Modified to support simple string variables and functions by David Mitchell
 * November 2008.
 * Changes and additions are marked 'string additions' throughout
 */


#ifndef __UBASIC_H__
#define __UBASIC_H__

#include <stddef.h>
#include <stdint.h>

#include "vartype.h"

typedef VARIABLE_TYPE (*peek_func)(VARIABLE_TYPE);
typedef void (*poke_func)(VARIABLE_TYPE, VARIABLE_TYPE);

/* I/O function pointers matching C# wrapper API */
typedef void (*out_func)(const char *message);
typedef void (*put_func)(char character);
typedef void (*in_func)(char *buffer, size_t max_len);
typedef char (*get_func)(void);
typedef void (*err_func)(const char *code, const char *description, uint32_t line);

/*
 * 8-bit style dynamic allocation layout:
 *   LOW MEMORY:
 *     int32_t resume_offset at offset 0
 *     int32_t gosub_depth at offset 4
 *     int32_t for_depth at offset 8
 *     int32_t gosub_stack[UBASIC_MAX_GOSUB_STACK_DEPTH]
 *     struct for_state for_stack[UBASIC_MAX_FOR_STACK_DEPTH]
 *     program bytes (NUL-terminated)
 *     heap_state_t (6 bytes)
 *     var_desc_t[] entries (grows UP)
 *     array data blocks (grows UP)
 *   
 *   FREE SPACE (between var_heap_top and str_pool_base)
 *   
 *   HIGH MEMORY:
 *     string pool data (grows DOWN from top)
 *
 * Variables allocated dynamically on first use. String data in pool,
 * string descriptors in variable heap. Arrays in heap.
 */
#define UBASIC_STRING_VAR_MAXLEN 255
#ifndef UBASIC_STRING_POOL_SIZE
#define UBASIC_STRING_POOL_SIZE 1024
#endif
#define UBASIC_MAX_GOSUB_STACK_DEPTH 10
#define UBASIC_MAX_FOR_STACK_DEPTH 4
#define UBASIC_MAX_PUSH_STACK_DEPTH 16
#define UBASIC_PUSH_STRING_LEN 40
#define UBASIC_VARIABLE_INDEXES_PER_LETTER 11 /* bare + 0..9 */
#define UBASIC_VARIABLE_COUNT (26 * UBASIC_VARIABLE_INDEXES_PER_LETTER)

#ifndef UBASIC_HEAP_BYTES
#define UBASIC_HEAP_BYTES 0
#endif

#define UBASIC_PRINT_ZONE_WIDTH 14

typedef struct for_state {
  uint32_t line_after_for;
  int32_t for_variable_index; /* 0..285 (a..z, a0..z9) */
  int32_t to;
  int32_t step;
} for_state;

typedef struct push_entry {
  int32_t type;                          /* 0 = numeric, 1 = string */
  VARIABLE_TYPE ival;
  char sval[UBASIC_PUSH_STRING_LEN + 1];
} push_entry;

/* Control state at low addresses */
#define UBASIC_MEM_RESUME_OFFSET      0
#define UBASIC_MEM_GOSUB_DEPTH_OFFSET 4
#define UBASIC_MEM_FOR_DEPTH_OFFSET   8
#define UBASIC_MEM_DATA_PTR_OFFSET    12
#define UBASIC_MEM_GOSUB_STACK_OFFSET 16
#define UBASIC_MEM_FOR_STACK_OFFSET \
  (UBASIC_MEM_GOSUB_STACK_OFFSET + UBASIC_MAX_GOSUB_STACK_DEPTH * (int)sizeof(int32_t))
/* PUSH/POP stack: depth cell + typed push_entry slots */
#define UBASIC_MEM_PUSH_DEPTH_OFFSET \
  (UBASIC_MEM_FOR_STACK_OFFSET + UBASIC_MAX_FOR_STACK_DEPTH * (int)sizeof(for_state))
#define UBASIC_MEM_PUSH_STACK_OFFSET \
  (UBASIC_MEM_PUSH_DEPTH_OFFSET + (int)sizeof(int32_t))
/* Program starts immediately after control structures */
#define UBASIC_MEM_PROGRAM_OFFSET \
  (UBASIC_MEM_PUSH_STACK_OFFSET + UBASIC_MAX_PUSH_STACK_DEPTH * (int)sizeof(push_entry))

/* Variables and strings at TOP of memory (calculated at runtime) */
#define UBASIC_PUSH_STACK_SIZE (UBASIC_MAX_PUSH_STACK_DEPTH * (int)sizeof(push_entry))

/* These offsets are calculated from END of memory in ubasic_init */
/* variables_mem = memory + (mem_bytes - UBASIC_VARIABLES_SIZE) */
/* string_state  = memory + (mem_bytes - UBASIC_VARIABLES_SIZE - UBASIC_STRING_STATE_SIZE) */
/* string pool grows DOWN from string_state toward program */

/* Variable types */
#define VARTYPE_INTEGER      0
#define VARTYPE_ARRAY        1
#define VARTYPE_STRING       2
#define VARTYPE_STRING_ARRAY 3

#pragma pack(push, 1)
/* Variable descriptor - allocated in heap on first use */
typedef struct {
  uint16_t var_id;        /* 0..285 (a..z, a0..z9) */
  uint8_t type;           /* VARTYPE_* */
  union {
    VARIABLE_TYPE int_value;
    uint16_t str_offset;      /* offset into string pool */
    uint16_t arr_offset;      /* offset to array_desc_t */
    uint16_t strarr_offset;   /* offset to string_array_desc_t */
  };
} var_desc_t;

/* Array descriptor stored in heap */
typedef struct {
  uint16_t data_offset;   /* offset to array data */
  uint8_t dimensions;     /* 1, 2, or 3 */
  uint16_t dim_size[3];   /* dimension sizes */
} array_desc_t;

/* String array descriptor stored in heap */
typedef struct {
  uint16_t data_offset;   /* offset to array of string offsets (uint16_t[]) */
  uint8_t dimensions;     /* 1, 2, or 3 */
  uint16_t dim_size[3];   /* dimension sizes */
} string_array_desc_t;

/* DEF FN function definition */
typedef struct {
  char func_letter;        /* 'A'..'Z' */
  uint16_t param_var;      /* Parameter variable number (0..285) */
  uint16_t expr_start;     /* Offset in program where expression starts */
  uint16_t expr_end;       /* Offset in program where expression ends */
  uint8_t defined;         /* 1 if function is defined, 0 otherwise */
} fn_def_t;

/* Heap state - immediately after program */
typedef struct {
  uint16_t var_heap_top;  /* Grows UP from heap_state + sizeof */
  uint16_t str_pool_base; /* Grows DOWN from top of memory */
  uint16_t var_count;     /* Number of variables allocated */
} heap_state_t;
#pragma pack(pop)

#define UBASIC_HEAP_STATE_SIZE ((int)sizeof(heap_state_t))
#define UBASIC_VAR_TABLE_BYTES (UBASIC_VARIABLE_COUNT * (int)sizeof(var_desc_t))

#define UBASIC_MIN_MEMORY_BYTES \
  (UBASIC_MEM_PROGRAM_OFFSET + 256u + UBASIC_HEAP_STATE_SIZE + UBASIC_VAR_TABLE_BYTES + 512u)

// Possibly public

void ubasic_init(uint8_t *memory, uint32_t memory_bytes);  /* Vars at top */
void ubasic_reset(void);
void ubasic_run(void);
int ubasic_finished(void);
void ubasic_load_program(const char *program);

// string addition
char* get_stringvariable(int);
void set_stringvariable(int, char *);
// end of string addition

/* I/O function setters matching C# wrapper API */
void ubasic_set_out_function(out_func func);
void ubasic_set_put_function(put_func func);
void ubasic_set_in_function(in_func func);
void ubasic_set_get_function(get_func func);
void ubasic_set_error_function(err_func func);
const char *ubasic_last_error_code(void);
const char *ubasic_last_error_description(void);
uint32_t ubasic_last_error_line(void);

#endif /* __UBASIC_H__ */
