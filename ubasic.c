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
 
#define DEBUG 0
#define VERBOSE 0

#if DEBUG
#define DEBUG_PRINTF(...)  printf(__VA_ARGS__)
#else
#define DEBUG_PRINTF(...)
#endif


#include "ubasic.h"
#include "tokenizer.h"

#include <stdio.h> /* printf() */
#include <stdlib.h> /* exit() */
#include <string.h> /* strlen(), memcpy(), sprintf() */
#include <stdint.h>
#include <math.h> /* Math functions */

// Probably private

typedef VARIABLE_TYPE (*peek_func)(VARIABLE_TYPE);
typedef void (*poke_func)(VARIABLE_TYPE, VARIABLE_TYPE);
static VARIABLE_TYPE expr(void);
static void line_statement(void);
static void statement(void);
static void data_statement(void);
static void restore_statement(void);
static void read_statement(void);
static void index_free(void);
static void reset_control_state(void);
static VARIABLE_TYPE expr(void);
void set_variable(int varum, VARIABLE_TYPE value);
VARIABLE_TYPE get_variable(int varnum);
static void gosub_push(int32_t line_num);
static int32_t gosub_pop(void);
static void for_push(for_state state);
static for_state for_pop(void);

static char const *program_ptr;
static char const *data_ptr;  /* Current position in DATA statements */
#define MAX_STRINGLEN 40
static char string[MAX_STRINGLEN];

static uint8_t *mem_base = NULL;
static size_t mem_capacity_bytes = 0;
static int32_t *resume_offset_cell = NULL;
static int32_t *gosub_depth_cell = NULL;
static int32_t *for_depth_cell = NULL;
static int32_t *gosub_stack_mem = NULL;
static for_state *for_stack = NULL;
static heap_state_t *heap_state = NULL;



// string additions
#define MAX_STRINGVARLEN UBASIC_STRING_VAR_MAXLEN
#define MAX_BUFFERLEN    4000
#if UBASIC_STRING_POOL_SIZE > MAX_BUFFERLEN
#error UBASIC_STRING_POOL_SIZE must not exceed MAX_BUFFERLEN (str_pool_compact scratch)
#endif
static char stringbuffer[MAX_BUFFERLEN];
static int freebufptr = 0;
#define MAX_SVARNUM UBASIC_VARIABLE_COUNT
// end of string additions

struct line_index {
  int line_number;
  char const *program_text_position;
  struct line_index *next;
};

struct line_index *line_index_head = NULL;
struct line_index *line_index_current = NULL;


static int ended;

static VARIABLE_TYPE expr(void);
static void line_statement(void);
static void statement(void);

static void index_free(void);

peek_func peek_function = NULL;
poke_func poke_function = NULL;

/* I/O function pointers matching C# wrapper API */
static void default_out_function(const char *message) {
  printf("%s", message);
}

static void default_in_function(char *buffer, size_t max_len) {
  if (fgets(buffer, (int)max_len, stdin) != NULL) {
    /* Remove trailing newline if present */
    size_t len = strlen(buffer);
    if (len > 0 && buffer[len - 1] == '\n') {
      buffer[len - 1] = '\0';
    }
  } else {
    buffer[0] = '\0';
  }
}

out_func out_function = default_out_function;  /* Default to printf wrapper */
put_func put_function = NULL;
in_func in_function = default_in_function;     /* Default to fgets wrapper */
get_func get_function = NULL;

// string additions
static char nullstring[] = "\0"; 
static void  var_init(void);
static char* sexpr(void);
static VARIABLE_TYPE get_array_element(int varnum, uint16_t idx1, uint16_t idx2, uint16_t idx3);
static void set_array_element(int varnum, uint16_t idx1, uint16_t idx2, uint16_t idx3, VARIABLE_TYPE value);
static char* scpy(const char *);
static char* sconcat(const char *, const char *);
static char* sleft(const char *, int); 
static char* sright(const char *,int);
static char* smid(const char *, int, int);
static char* sstr(int);
static char* schr(int);
static int sinstr(int, const char*, const char*);
static void str_pool_compact(void);
static var_desc_t *find_variable(char name);
static var_desc_t *find_or_create_variable(char name, uint8_t type);
static uint16_t str_pool_alloc(uint16_t size);
// end of string additions

/*---------------------------------------------------------------------------*/
// Heap management - 8-bit style dynamic allocation
/*---------------------------------------------------------------------------*/
static heap_state_t *get_heap_state(void) {
  if (mem_base == NULL) return NULL;
  return heap_state;
}

static var_desc_t *get_var_table(void) {
  if (heap_state == NULL) return NULL;
  /* Variable table starts right after heap_state */
  return (var_desc_t *)((uint8_t *)heap_state + sizeof(heap_state_t));
}

static var_desc_t *find_variable(char name) {
  var_desc_t *vartab;
  uint16_t i;
  
  if (heap_state == NULL) return NULL;
  vartab = get_var_table();
  
  for (i = 0; i < heap_state->var_count; i++) {
    if (vartab[i].name == name) {
      return &vartab[i];
    }
  }
  return NULL;
}

static var_desc_t *create_variable(char name, uint8_t type) {
  var_desc_t *vartab;
  var_desc_t *newvar;
  uint16_t new_heap_top;
  
  if (heap_state == NULL) {
    DEBUG_PRINTF("create_variable: heap_state is NULL\n");
    return NULL;
  }
  
  /* Check if heap and string pool would collide */
  new_heap_top = heap_state->var_heap_top + sizeof(var_desc_t);
  if (new_heap_top >= heap_state->str_pool_base) {
    DEBUG_PRINTF("create_variable: Out of memory (heap collision)\n");
    ended = 1;
    return NULL;
  }
  
  vartab = get_var_table();
  newvar = (var_desc_t *)(mem_base + heap_state->var_heap_top);
  
  newvar->name = name;
  newvar->type = type;
  newvar->int_value = 0;  /* Initialize to zero */
  
  heap_state->var_heap_top = new_heap_top;
  heap_state->var_count++;
  
  #if VERBOSE
    DEBUG_PRINTF("create_variable: Created '%c' type %d at offset %u\n",
                 name, type, (unsigned)(heap_state->var_heap_top - sizeof(var_desc_t)));
  #endif
  
  return newvar;
}

static var_desc_t *find_or_create_variable(char name, uint8_t type) {
  var_desc_t *v = find_variable(name);
  if (v != NULL) {
    /* Variable exists - verify type matches */
    if (v->type != type) {
      DEBUG_PRINTF("find_or_create_variable: Type mismatch for '%c'\n", name);
      return NULL;
    }
    return v;
  }
  /* Create new variable */
  return create_variable(name, type);
}

static uint16_t heap_alloc(uint16_t size) {
  uint16_t offset;
  uint16_t new_heap_top;
  
  if (heap_state == NULL) {
    ended = 1;
    return 0;
  }
  
  new_heap_top = heap_state->var_heap_top + size;
  if (new_heap_top >= heap_state->str_pool_base) {
    DEBUG_PRINTF("heap_alloc: Out of memory (need %u bytes)\n", (unsigned)size);
    ended = 1;
    return 0;
  }
  
  offset = heap_state->var_heap_top;
  heap_state->var_heap_top = new_heap_top;
  
  return offset;
}

static uint16_t str_pool_alloc(uint16_t size) {
  uint16_t new_pool_base;
  
  if (heap_state == NULL) {
    ended = 1;
    return 0;
  }
  
  if (heap_state->str_pool_base < heap_state->var_heap_top + size) {
    DEBUG_PRINTF("str_pool_alloc: Out of memory (need %u bytes)\n", (unsigned)size);
    ended = 1;
    return 0;
  }
  
  new_pool_base = heap_state->str_pool_base - size;
  heap_state->str_pool_base = new_pool_base;
  
  return new_pool_base;
}

/*---------------------------------------------------------------------------*/
// Public functions
/*---------------------------------------------------------------------------*/
void ubasic_init(uint8_t *memory, uint32_t memory_bytes) {
  size_t heap_state_offset;
  
  mem_base = memory;
  mem_capacity_bytes = memory_bytes;
  resume_offset_cell = (int32_t *)(memory + UBASIC_MEM_RESUME_OFFSET);
  gosub_depth_cell = (int32_t *)(memory + UBASIC_MEM_GOSUB_DEPTH_OFFSET);
  for_depth_cell = (int32_t *)(memory + UBASIC_MEM_FOR_DEPTH_OFFSET);
  gosub_stack_mem = (int32_t *)(memory + UBASIC_MEM_GOSUB_STACK_OFFSET);
  for_stack = (for_state *)(memory + UBASIC_MEM_FOR_STACK_OFFSET);
  
  /* Heap state immediately after program offset (program loaded later) */
  heap_state_offset = UBASIC_MEM_PROGRAM_OFFSET + 1;
  heap_state = (heap_state_t *)(memory + heap_state_offset);
  
  /* Initialize heap - heap grows UP, string pool grows DOWN */
  heap_state->var_heap_top = (uint16_t)(heap_state_offset + UBASIC_HEAP_STATE_SIZE);
  heap_state->str_pool_base = (uint16_t)memory_bytes;
  heap_state->var_count = 0;
  
  program_ptr = (char const *)(memory + UBASIC_MEM_PROGRAM_OFFSET);
  memory[UBASIC_MEM_PROGRAM_OFFSET] = '\0';
  data_ptr = program_ptr;  /* Initialize DATA pointer to start of program */
  index_free();
  tokenizer_init(program_ptr);
  
  #if VERBOSE
    DEBUG_PRINTF("ubasic_init: Heap state at offset %zu\n", heap_state_offset);
    DEBUG_PRINTF("ubasic_init: Var heap starts at %u, string pool at %u\n",
                 (unsigned)heap_state->var_heap_top, (unsigned)heap_state->str_pool_base);
    DEBUG_PRINTF("ubasic_init: Total dynamic space: %u bytes\n",
                 (unsigned)(heap_state->str_pool_base - heap_state->var_heap_top));
  #endif
}
/*---------------------------------------------------------------------------*/
void ubasic_init_peek_poke(uint8_t *memory, uint32_t memory_bytes,peek_func peek, poke_func poke) {
  ubasic_init(memory, memory_bytes);
  peek_function = peek;
  poke_function = poke;
  #if VERBOSE
    DEBUG_PRINTF("ubasic_init_peek_poke: Initializing uBasic with peek/poke.\n");
  #endif
}
/*---------------------------------------------------------------------------*/
static void reset_control_state(void) {
  index_free();
  tokenizer_reset();
  if (resume_offset_cell != NULL) {
    *resume_offset_cell = 0;
  }
  if (gosub_depth_cell != NULL) {
    *gosub_depth_cell = 0;
  }
  if (for_depth_cell != NULL) {
    *for_depth_cell = 0;
  }
  if (gosub_stack_mem != NULL) {
    memset(gosub_stack_mem, 0, UBASIC_MAX_GOSUB_STACK_DEPTH * sizeof(int32_t));
  }
  if (for_stack != NULL) {
    memset(for_stack, 0, UBASIC_MAX_FOR_STACK_DEPTH * sizeof(for_state));
  }
  
  ended = 0;
}
/*---------------------------------------------------------------------------*/
void ubasic_reset(void) {
  index_free();
  tokenizer_reset();
  if (gosub_depth_cell != NULL) {
    *gosub_depth_cell = 0;
  }
  if (for_depth_cell != NULL) {
    *for_depth_cell = 0;
  }
  ended = 0;

  #if VERBOSE
    DEBUG_PRINTF("ubasic_reset: Full reset including variables.\n");
  #endif
}
/*---------------------------------------------------------------------------*/
void ubasic_load_program(const char *program) {
  size_t len;
  size_t program_end;
  size_t heap_state_offset;

  if (mem_base == NULL) {
    DEBUG_PRINTF("ubasic_load_program: call ubasic_init first.\n");
    return;
  }
  
  /* Reset control state but PRESERVE VARIABLES */
  reset_control_state();
  
  len = strlen(program);
  program_end = UBASIC_MEM_PROGRAM_OFFSET + len + 1;
  heap_state_offset = program_end;
  
  /* Check program fits before heap state */
  if (program_end + UBASIC_HEAP_STATE_SIZE >= mem_capacity_bytes) {
    DEBUG_PRINTF("ubasic_load_program: program too large (%u bytes)\n", (unsigned)len);
    return;
  }
  
  /* Load program */
  memcpy(mem_base + UBASIC_MEM_PROGRAM_OFFSET, program, len + 1);
  program_ptr = (char const *)(mem_base + UBASIC_MEM_PROGRAM_OFFSET);
  data_ptr = program_ptr;  /* Reset DATA pointer to start of new program */
  tokenizer_init(program_ptr);
  
  /* Reset heap - position heap_state after program */
  heap_state = (heap_state_t *)(mem_base + heap_state_offset);
  heap_state->var_heap_top = (uint16_t)(heap_state_offset + UBASIC_HEAP_STATE_SIZE);
  heap_state->str_pool_base = (uint16_t)mem_capacity_bytes;
  heap_state->var_count = 0;
  
  #if VERBOSE
    DEBUG_PRINTF("ubasic_load_program: Loaded %u bytes\n", (unsigned)len);
    DEBUG_PRINTF("ubasic_load_program: Heap reset, %u bytes available\n",
                 (unsigned)(heap_state->str_pool_base - heap_state->var_heap_top));
  #endif
}
/*---------------------------------------------------------------------------*/
int ubasic_finished(void){
  return ended || tokenizer_finished();
}
/*---------------------------------------------------------------------------*/
void ubasic_run(void){
  if(tokenizer_finished()) {
    #if VERBOSE
      DEBUG_PRINTF("ubasic_run: Program finished.\n");
    #endif
    return;
  }
  // string additions: repack string pool in memory[], then discard expr temporaries
  str_pool_compact();
  freebufptr = 0;
  // end of string additions

  line_statement();
}
/*---------------------------------------------------------------------------*/
static void save_position(void) {
  const char *current_pos = tokenizer_pos();
  const char *start_pos = tokenizer_start();
  int32_t offset = (int32_t)(current_pos - start_pos);
  
  if (resume_offset_cell != NULL) {
    *resume_offset_cell = offset;
    #if VERBOSE
      DEBUG_PRINTF("save_position: Saved offset %d.\n", offset);
    #endif
  }
}
/*---------------------------------------------------------------------------*/
void ubasic_resume(void) {
  int32_t offset;
  const char *start_pos;
  const char *resume_pos;
  
  if (resume_offset_cell == NULL || program_ptr == NULL) {
    DEBUG_PRINTF("ubasic_resume: Not initialized.\n");
    return;
  }
  
  offset = *resume_offset_cell;
  start_pos = tokenizer_start();
  resume_pos = start_pos + offset;
  
  tokenizer_goto(resume_pos);
  ended = 0;
  
  #if VERBOSE
    DEBUG_PRINTF("ubasic_resume: Resumed from offset %d.\n", offset);
  #endif
}
/*---------------------------------------------------------------------------*/
// Private functions
/*---------------------------------------------------------------------------*/
static void accept(int token){
  if(token != tokenizer_token()) {
    DEBUG_PRINTF("accept: Token not what was expected (expected '%s', got %s).\n",
    tokenizer_token_name(token),
    tokenizer_token_name(tokenizer_token()));
    tokenizer_error_print();
    exit(1);
  }
  DEBUG_PRINTF("accept: Expected '%s', got it.\n", tokenizer_token_name(token));
  tokenizer_next();
}
/*---------------------------------------------------------------------------*/
// string additions
/*---------------------------------------------------------------------------*/
static void str_pool_compact(void) {
  /* Compact string pool - repack all string variables */
  var_desc_t *vartab;
  uint16_t i;
  uint16_t new_pool_base;
  uint16_t temp_offset = 0;
  
  if (heap_state == NULL || mem_base == NULL) return;
  
  vartab = get_var_table();
  new_pool_base = (uint16_t)mem_capacity_bytes;
  
  /* Copy all strings to scratch buffer and rebuild pool from top */
  for (i = 0; i < heap_state->var_count; i++) {
    if (vartab[i].type == VARTYPE_STRING && vartab[i].str_offset != 0) {
      char *str_data = (char *)(mem_base + vartab[i].str_offset);
      size_t len = strlen(str_data);
      
      if (len > 0 && len <= MAX_STRINGVARLEN) {
        if (temp_offset + len + 1 > MAX_BUFFERLEN) {
          ended = 1;
          return;
        }
        memcpy(stringbuffer + temp_offset, str_data, len + 1);
        temp_offset += (uint16_t)(len + 1);
      }
    }
  }
  
  /* Rebuild string pool from top down */
  temp_offset = 0;
  for (i = 0; i < heap_state->var_count; i++) {
    if (vartab[i].type == VARTYPE_STRING && vartab[i].str_offset != 0) {
      char *src = stringbuffer + temp_offset;
      size_t len = strlen(src);
      
      if (len > 0) {
        new_pool_base -= (uint16_t)(len + 1);
        memcpy(mem_base + new_pool_base, src, len + 1);
        vartab[i].str_offset = new_pool_base;
        temp_offset += (uint16_t)(len + 1);
      } else {
        vartab[i].str_offset = 0;
      }
    }
  }
  
  heap_state->str_pool_base = new_pool_base;
  
  #if VERBOSE
    DEBUG_PRINTF("str_pool_compact: Compacted to %u bytes\n",
                 (unsigned)(mem_capacity_bytes - new_pool_base));
  #endif
}

static void var_init() {
  /* Reset heap completely */
  freebufptr = 0;
  
  if (heap_state != NULL && mem_base != NULL) {
    size_t heap_state_offset = (uint8_t *)heap_state - mem_base;
    heap_state->var_heap_top = (uint16_t)(heap_state_offset + UBASIC_HEAP_STATE_SIZE);
    heap_state->str_pool_base = (uint16_t)mem_capacity_bytes;
    heap_state->var_count = 0;
  }
}
/*---------------------------------------------------------------------------*/
static int string_space_check(int l) {
   // returns true if not enough room for new string
   int i;
   i = ((MAX_BUFFERLEN - freebufptr) <= (l + 2)); // +2 to play it safe
   if (i) {
      ended = 1;
   }
   return i;
}
/*---------------------------------------------------------------------------*/
static char* scpy(const char *s1) { // return a copy of s1
   int bp = freebufptr;
   int l;
   l = strlen(s1);
   if (string_space_check(l)) 
      return nullstring;
   strcpy(stringbuffer+bp, s1);
   freebufptr = bp + l + 1;
   return stringbuffer+bp;
}
/*---------------------------------------------------------------------------*/
static char* sconcat(const char *s1, const char*s2) { // return the concatenation of s1 and s2
   int bp = freebufptr;   
   int rp = bp;
   int l1, l2;
   l1 = strlen(s1);
   l2 = strlen(s2);
   if (string_space_check(l1+l2))
      return nullstring;
   strcpy((stringbuffer+bp), s1);
   bp += l1;
   if (l1 == MAX_STRINGVARLEN) {
      freebufptr = bp + 1;
	  return (stringbuffer + rp); 
   }
   l2 = strlen(s2);
   strcpy((stringbuffer+bp), s2);
   if (l1 + l2 > MAX_STRINGVARLEN) {
      l2 = MAX_STRINGVARLEN - l1;
	  // truncate
	  *(stringbuffer + bp + l2) = '\0';
   }   
   freebufptr = bp + l2 + 1;
   return (stringbuffer+rp);   
}
/*---------------------------------------------------------------------------*/
static char* sleft(const char *s1, int l) { // return the left l chars of s1
   int bp = freebufptr;
   int rp = bp;
   int i;
   if (l<1) 
     return scpy(nullstring);
   if (string_space_check(l))
      return nullstring;
   if (strlen(s1) <=l) {
      return scpy(s1);
   } else {
      for (i=0; i<l; i++) {
	     *(stringbuffer +bp) = *s1;
		 bp++;
		 s1++;
	  }
	  *(stringbuffer + bp) = '\0';
	  freebufptr = bp+1;
	
   }
   return stringbuffer + rp;
}
/*---------------------------------------------------------------------------*/
static char* sright(const char *s1, int l) { // return the right l chars of s1
   int bp = freebufptr;
   int rp = bp;
   int i, j;
   j = strlen(s1);
   if (l<1) 
     return scpy(nullstring);
   if (string_space_check(l))
      return nullstring;
   if (j <= l) {
      return scpy(s1);
   } else {
      for (i=0; i<l; i++) {
	     *(stringbuffer + bp) = *(s1 + j-l);
		 bp++;
		 s1++;
	  }
	  *(stringbuffer + bp) = '\0';
	  freebufptr = bp+1;
	
   }
   return stringbuffer + rp;
}
/*---------------------------------------------------------------------------*/
static char* smid(const char *s1, int l1, int l2) { // return the l2 chars of s1 starting at offset l1
   int bp = freebufptr;
   int rp = bp;
   int i, j;
   j = strlen(s1);
   if (l2<1 || l1>j) 
      return scpy(nullstring);
   if (string_space_check(l2))
      return nullstring;
   if (l2 > j-l1)
     l2 = j-l1;
   for (i=l1; i<l1+l2; i++) {
      *(stringbuffer + bp) = *(s1 + l1 -1);
      bp++;
      s1++;
   }
   *(stringbuffer + bp) = '\0';
   freebufptr = bp+1;
   return stringbuffer + rp;
}
/*---------------------------------------------------------------------------*/
static char* sstr(int j) { // return the integer j as a string
   int bp = freebufptr;
   int rp = bp;
   if (string_space_check(10))
      return nullstring;
   sprintf((stringbuffer+bp),"%d",j);
   freebufptr = bp + strlen(stringbuffer+bp) + 1;
   return stringbuffer + rp;
}
/*---------------------------------------------------------------------------*/
static char* schr(int j) { // return the character whose ASCII code is j
   int bp = freebufptr;
   int rp = bp;
   
   if (string_space_check(1))
      return nullstring;
   sprintf((stringbuffer+bp),"%c",j);
   
   freebufptr = bp + 2;
   return stringbuffer + rp;
}
/*---------------------------------------------------------------------------*/
static int sinstr(int j, const char *s, const char *s1) { // return the position of s1 in s (or 0) 
   char *p;
   p = strstr(s+j, s1);
   if (p == NULL)
      return 0;
   return (p - s + 1);
}
/*---------------------------------------------------------------------------*/
static char* sfactor() { // string form of factor
   char *r, *s;
   int i, j;
   switch(tokenizer_token()) {
       case TOKENIZER_LEFTPAREN:
	      accept(TOKENIZER_LEFTPAREN);
		  r = sexpr();
		  accept(TOKENIZER_RIGHTPAREN);
		  break;
	   case TOKENIZER_STRING:
	      tokenizer_string(string, sizeof(string));
		  r= scpy(string);
  	      accept(TOKENIZER_STRING);
	      break;
 	case TOKENIZER_LEFT$:
	      accept(TOKENIZER_LEFT$);
		  accept(TOKENIZER_LEFTPAREN);
          s = sexpr();
		  accept(TOKENIZER_COMMA);
		  i = expr();
		  r = sleft(s,i);
		  accept(TOKENIZER_RIGHTPAREN);
          break;
	case TOKENIZER_RIGHT$:
	      accept(TOKENIZER_RIGHT$);
		  accept(TOKENIZER_LEFTPAREN);
		  s = sexpr();
		  accept(TOKENIZER_COMMA);
		  i = expr();
		  r = sright(s,i);
		  accept(TOKENIZER_RIGHTPAREN);
          break;
	case TOKENIZER_MID$:
	      accept(TOKENIZER_MID$);
		  accept(TOKENIZER_LEFTPAREN);
		  s = sexpr();
		  accept(TOKENIZER_COMMA);
		  i = expr();
		  if (tokenizer_token() == TOKENIZER_COMMA) {
		     accept(TOKENIZER_COMMA);
			 j = expr();
		  } else {
		     j = 999; // ensure we get all of it
		  }
		  r = smid(s,i,j);
		  accept(TOKENIZER_RIGHTPAREN);
          break;
    case TOKENIZER_STR$:
	      accept(TOKENIZER_STR$);
	      j = expr();
		  r = sstr(j);
	      break;
	case TOKENIZER_CHR$:
	     accept(TOKENIZER_CHR$);
		 j = expr();
		 if (j<0 || j>255)
		    j = 0;
		 r = schr(j);
		 break;
	default:	  
		  r = get_stringvariable(tokenizer_variable_num());
	      accept(TOKENIZER_STRINGVARIABLE);
	}
   return r;
}
/*---------------------------------------------------------------------------*/
static char* sexpr(void) { // string form of expr
   char *s1, *s2;
   int op;
   s1 = sfactor();
   op = tokenizer_token();
   DEBUG_PRINTF("sexpr s1= '%s' op= %d\n", s1, op);  
   while(op == TOKENIZER_PLUS) {
      tokenizer_next();
	  s2 = sfactor();
	  s1 = sconcat(s1,s2);
	  op = tokenizer_token();
   }
   DEBUG_PRINTF("sexpr returning s1= '%s'\n", s1);  

   return s1;
}
/*---------------------------------------------------------------------------*/
static int slogexpr() { // string logical expression
   char *s1, *s2;
   int op;
   int r = 0;
   s1 = sexpr();
   op = tokenizer_token();
   tokenizer_next();
   switch(op) {
      case TOKENIZER_EQ:
	     s2 = sexpr();
	     r = (strcmp(s1,s2) == 0);
		 break;
   }
   return r;
}
/*---------------------------------------------------------------------------*/
// end of string additions
/*---------------------------------------------------------------------------*/
static int varfactor(void) {
  int r;
  int var;
  uint16_t idx1 = 0, idx2 = 0, idx3 = 0;
  
  var = tokenizer_variable_num();
  DEBUG_PRINTF("varfactor: obtaining %d from variable %d.\n", get_variable(var), var);
  accept(TOKENIZER_VARIABLE);
  
  /* Check for array indexing */
  if (tokenizer_token() == TOKENIZER_LEFTPAREN) {
    accept(TOKENIZER_LEFTPAREN);
    idx1 = (uint16_t)expr();
    
    if (tokenizer_token() == TOKENIZER_COMMA) {
      accept(TOKENIZER_COMMA);
      idx2 = (uint16_t)expr();
      
      if (tokenizer_token() == TOKENIZER_COMMA) {
        accept(TOKENIZER_COMMA);
        idx3 = (uint16_t)expr();
      }
    }
    
    accept(TOKENIZER_RIGHTPAREN);
    r = get_array_element(var, idx1, idx2, idx3);
  } else {
    r = get_variable(var);
  }
  
  return r;
}
/*---------------------------------------------------------------------------*/
static int factor(void) {
  int r;
 // string function additions
  int j;
  char *s, *s1;
  
  DEBUG_PRINTF("factor: token '%s'.\n", tokenizer_token_name(tokenizer_token()));
  switch(tokenizer_token()) {
     case TOKENIZER_LEN:
      accept(TOKENIZER_LEN);
      r = strlen(sexpr());
      break;  
    case TOKENIZER_VAL:
     accept(TOKENIZER_VAL);
     r = atoi(sexpr());
	 break;
   case TOKENIZER_ASC:
    accept(TOKENIZER_ASC);
	s = sexpr();
	r = *s; 
	break;
   case TOKENIZER_INSTR:
    accept(TOKENIZER_INSTR);
	accept(TOKENIZER_LEFTPAREN);
	j = 1;
	if (tokenizer_token() == TOKENIZER_NUMBER) {
	  j = tokenizer_num();
	  accept(TOKENIZER_NUMBER);
	  accept(TOKENIZER_COMMA);
	} 
	if (j <1)
	   return 0;
	s = sexpr();
	accept(TOKENIZER_COMMA);
	s1 = sexpr();
	accept(TOKENIZER_RIGHTPAREN);
	r = sinstr(j, s, s1);
	break;	
 // end of string additions 
 // math functions
  case TOKENIZER_ABS:
    accept(TOKENIZER_ABS);
    accept(TOKENIZER_LEFTPAREN);
    r = expr();
    accept(TOKENIZER_RIGHTPAREN);
    r = (r < 0) ? -r : r;
    break;
  case TOKENIZER_INT:
    accept(TOKENIZER_INT);
    accept(TOKENIZER_LEFTPAREN);
    r = (int)floor((double)expr());
    accept(TOKENIZER_RIGHTPAREN);
    break;
  case TOKENIZER_SGN:
    accept(TOKENIZER_SGN);
    accept(TOKENIZER_LEFTPAREN);
    r = expr();
    accept(TOKENIZER_RIGHTPAREN);
    r = (r > 0) ? 1 : ((r < 0) ? -1 : 0);
    break;
  case TOKENIZER_SQR:
    accept(TOKENIZER_SQR);
    accept(TOKENIZER_LEFTPAREN);
    r = (int)sqrt((double)expr());
    accept(TOKENIZER_RIGHTPAREN);
    break;
  case TOKENIZER_RND:
    accept(TOKENIZER_RND);
    accept(TOKENIZER_LEFTPAREN);
    j = expr();
    accept(TOKENIZER_RIGHTPAREN);
    r = (int)((rand() / (double)RAND_MAX) * j);
    break;
  case TOKENIZER_SIN:
    accept(TOKENIZER_SIN);
    accept(TOKENIZER_LEFTPAREN);
    r = (int)(sin((double)expr()) * 1000);
    accept(TOKENIZER_RIGHTPAREN);
    break;
  case TOKENIZER_COS:
    accept(TOKENIZER_COS);
    accept(TOKENIZER_LEFTPAREN);
    r = (int)(cos((double)expr()) * 1000);
    accept(TOKENIZER_RIGHTPAREN);
    break;
  case TOKENIZER_TAN:
    accept(TOKENIZER_TAN);
    accept(TOKENIZER_LEFTPAREN);
    r = (int)(tan((double)expr()) * 1000);
    accept(TOKENIZER_RIGHTPAREN);
    break;
  case TOKENIZER_ATN:
    accept(TOKENIZER_ATN);
    accept(TOKENIZER_LEFTPAREN);
    r = (int)(atan((double)expr()) * 1000);
    accept(TOKENIZER_RIGHTPAREN);
    break;
  case TOKENIZER_EXP:
    accept(TOKENIZER_EXP);
    accept(TOKENIZER_LEFTPAREN);
    r = (int)exp((double)expr());
    accept(TOKENIZER_RIGHTPAREN);
    break;
  case TOKENIZER_LOG:
    accept(TOKENIZER_LOG);
    accept(TOKENIZER_LEFTPAREN);
    r = (int)(log((double)expr()) * 1000);
    accept(TOKENIZER_RIGHTPAREN);
    break;
 // end of math functions
	 
  case TOKENIZER_NUMBER:
    r = tokenizer_num();
    DEBUG_PRINTF("factor: number %d.\n", r);
    accept(TOKENIZER_NUMBER);
    break;
  case TOKENIZER_LEFTPAREN:
    accept(TOKENIZER_LEFTPAREN);
    r = expr();
    accept(TOKENIZER_RIGHTPAREN);
    break;
  default:
    r = varfactor();
    break;
  }
  
  /* Handle exponentiation (right-associative) */
  if (tokenizer_token() == TOKENIZER_POWER) {
    accept(TOKENIZER_POWER);
    r = (int)pow((double)r, (double)factor());
  }
  
  DEBUG_PRINTF("term: %d.\n", r);
  return r;
}
/*---------------------------------------------------------------------------*/
static int term(void) {
  int f1, f2;
  int op;
  if (tokenizer_stringlookahead()) {
    f1 = slogexpr();
  } else {
   f1 = factor();
   op = tokenizer_token();
   DEBUG_PRINTF("term: token '%s'.\n", tokenizer_token_name(op));
   while(op == TOKENIZER_ASTR ||
     op == TOKENIZER_SLASH ||
     op == TOKENIZER_MOD) {
     tokenizer_next();
     f2 = factor();
     DEBUG_PRINTF("term: %d %d %d\n", f1, op, f2);
     switch(op) {
       case TOKENIZER_ASTR:
        f1 = f1 * f2;
        break;
       case TOKENIZER_SLASH:
        f1 = f1 / f2;
        break;
       case TOKENIZER_MOD:
        f1 = f1 % f2;
        break;
     }
     op = tokenizer_token();
   }	 
  }
  DEBUG_PRINTF("term: factor=%d.\n", f1);
  return f1;
}
/*---------------------------------------------------------------------------*/
static VARIABLE_TYPE expr(void){
  int t1, t2;
  int op;
  int unary_minus = 0;
  
  /* Handle unary + or - */
  if (tokenizer_token() == TOKENIZER_PLUS) {
    accept(TOKENIZER_PLUS);
  } else if (tokenizer_token() == TOKENIZER_MINUS) {
    accept(TOKENIZER_MINUS);
    unary_minus = 1;
  }

  t1 = term();
  
  /* Apply unary minus if present */
  if (unary_minus) {
    t1 = -t1;
  }
  
  op = tokenizer_token();
  DEBUG_PRINTF("expr: token %s.\n", tokenizer_token_name(op));
  while(op == TOKENIZER_PLUS ||
       op == TOKENIZER_MINUS) {
    tokenizer_next();
    t2 = term();
    DEBUG_PRINTF("expr: %d %d %d.\n", t1, op, t2);
    switch(op) {
    case TOKENIZER_PLUS:
      t1 = t1 + t2;
      break;
    case TOKENIZER_MINUS:
      t1 = t1 - t2;
      break;
    }
    op = tokenizer_token();
  }
  DEBUG_PRINTF("expr: term=%d.\n", t1);
  return t1;
}
/*---------------------------------------------------------------------------*/
static int relation(void){
  int r1, r2;
  int op;

  r1 = expr();
  op = tokenizer_token();
  DEBUG_PRINTF("relation: token %d.\n", op);
  while(op == TOKENIZER_LT ||
       op == TOKENIZER_GT ||
       op == TOKENIZER_EQ ||
       op == TOKENIZER_NE ||
       op == TOKENIZER_LE ||
       op == TOKENIZER_GE) {
    tokenizer_next();
    r2 = expr();
    DEBUG_PRINTF("relation: %d %d %d.\n", r1, op, r2);
    switch(op) {
    case TOKENIZER_LT:
      r1 = r1 < r2;
      break;
    case TOKENIZER_GT:
      r1 = r1 > r2;
      break;
    case TOKENIZER_EQ:
      r1 = r1 == r2;
      break;
    case TOKENIZER_NE:
      r1 = r1 != r2;
      break;
    case TOKENIZER_LE:
      r1 = r1 <= r2;
      break;
    case TOKENIZER_GE:
      r1 = r1 >= r2;
      break;
    }
    op = tokenizer_token();
  }
  DEBUG_PRINTF("relation: expr=%d.\n", r1);
  return r1;
}
/*---------------------------------------------------------------------------*/
static void index_free(void) {
  if(line_index_head != NULL) {
    line_index_current = line_index_head;
    do {
      DEBUG_PRINTF("index_free: Freeing index for line %d.\n", line_index_current->line_number);
      line_index_head = line_index_current;
      line_index_current = line_index_current->next;
      free(line_index_head);
    } while (line_index_current != NULL);
    line_index_head = NULL;
  }
}
/*---------------------------------------------------------------------------*/
static char const* index_find(int linenum) {
  struct line_index *lidx;
  lidx = line_index_head;

  #if DEBUG
  int step = 0;
  #endif

  while(lidx != NULL && lidx->line_number != linenum) {
    lidx = lidx->next;

    #if DEBUG
    	#if VERBOSE
      		if(lidx != NULL) {
        		DEBUG_PRINTF("index_find: Step %3d. Found index for line %d: %lld.\n",
        		step,
        		lidx->line_number,
        		(long long)(lidx->program_text_position - tokenizer_start()));
      		}
      		step++;
    	#endif
    #endif
  }
  if(lidx != NULL && lidx->line_number == linenum) {
    #if DEBUG
    	#if VERBOSE
      		DEBUG_PRINTF("index_find: Returning index for line %d.\n", linenum);
    	#endif
    #endif
    return lidx->program_text_position;
  }
  DEBUG_PRINTF("index_find: Returning NULL.\n", linenum);
  return NULL;
}
/*---------------------------------------------------------------------------*/
static void index_add(int linenum, char const* sourcepos) {
  if(line_index_head != NULL && index_find(linenum)) {
    return;
  }

  struct line_index *new_lidx;

  new_lidx = malloc(sizeof(struct line_index));
  new_lidx->line_number = linenum;
  new_lidx->program_text_position = sourcepos;
  new_lidx->next = NULL;

  if(line_index_head != NULL) {
    line_index_current->next = new_lidx;
    line_index_current = line_index_current->next;
  } else {
    line_index_current = new_lidx;
    line_index_head = line_index_current;
  }
  #if DEBUG
  	#if VERBOSE
		DEBUG_PRINTF("index_add: Adding index for line %d: %lld.\n", linenum,
			(long long)(sourcepos - tokenizer_start()));
		#endif
	#endif
}
/*---------------------------------------------------------------------------*/
static void jump_linenum_slow(int linenum) {
  tokenizer_init(program_ptr);
  while(tokenizer_num() != linenum) {
    do {
      do {
        tokenizer_next();
      } while(tokenizer_token() != TOKENIZER_LF &&
          tokenizer_token() != TOKENIZER_ENDOFINPUT);
      if(tokenizer_token() == TOKENIZER_LF) {
        tokenizer_next();
      }
    } while(tokenizer_token() != TOKENIZER_NUMBER);
	#if DEBUG
      #if VERBOSE
        DEBUG_PRINTF("jump_linenum_slow: Found line %d.\n", tokenizer_num());
	  #endif
	#endif
  }
}
/*---------------------------------------------------------------------------*/
static void jump_linenum(int linenum) {
  char const* pos = index_find(linenum);
  if(pos != NULL) {
    DEBUG_PRINTF("jump_linenum: Going to line %d.\n", linenum);
    tokenizer_goto(pos);
  } else {
    /* We'll try to find a yet-unindexed line to jump to. */
    DEBUG_PRINTF("jump_linenum: Calling jump_linenum_slow for line %d.\n", linenum);
    jump_linenum_slow(linenum);
  }
}
/*---------------------------------------------------------------------------*/
static void goto_statement(void) {
  accept(TOKENIZER_GOTO);
  DEBUG_PRINTF("jump_linenum: goto.\n");
  jump_linenum(tokenizer_num());
}
/*---------------------------------------------------------------------------*/
static void on_statement(void) {
  int index;
  int line_numbers[20];
  int count = 0;
  int is_gosub;
  
  accept(TOKENIZER_ON);
  
  /* Evaluate expression to get index (1-based) */
  index = expr();
  
  /* Determine if GOTO or GOSUB */
  if (tokenizer_token() == TOKENIZER_GOTO) {
    accept(TOKENIZER_GOTO);
    is_gosub = 0;
  } else if (tokenizer_token() == TOKENIZER_GOSUB) {
    accept(TOKENIZER_GOSUB);
    is_gosub = 1;
  } else {
    DEBUG_PRINTF("on_statement: Expected GOTO or GOSUB.\n");
    ended = 1;
    return;
  }
  
  /* Parse comma-separated list of line numbers */
  while (count < 20 && tokenizer_token() == TOKENIZER_NUMBER) {
    line_numbers[count] = tokenizer_num();
    count++;
    accept(TOKENIZER_NUMBER);
    
    if (tokenizer_token() == TOKENIZER_COMMA) {
      accept(TOKENIZER_COMMA);
    } else {
      break;
    }
  }
  
  accept(TOKENIZER_LF);
  
  /* Execute if index is in valid range (1-based) */
  if (index >= 1 && index <= count) {
    int target_line = line_numbers[index - 1];
    
    if (is_gosub) {
      /* GOSUB: push return address and jump */
      if (*gosub_depth_cell < UBASIC_MAX_GOSUB_STACK_DEPTH) {
        gosub_push(tokenizer_num());
        jump_linenum(target_line);
      } else {
        DEBUG_PRINTF("on_statement: gosub stack exhausted.\n");
      }
    } else {
      /* GOTO: just jump */
      jump_linenum(target_line);
    }
  }
  /* If index out of range, continue to next statement (do nothing) */
  
  DEBUG_PRINTF("on_statement: index=%d, count=%d, is_gosub=%d\n", index, count, is_gosub);
}
/*---------------------------------------------------------------------------*/
static void print_statement(void) {
  // string additions
  static char buf[128];
  buf[0]=0;
  // end of string additions
  accept(TOKENIZER_PRINT);
  DEBUG_PRINTF("print_statement: Loop.\n");
  
  // EBNF: "PRINT" list , { ("," | ";") , list }
  // list = string | variable | expression | tab_function
  
  // Process first list item if present (PRINT alone is valid for blank line)
  if(tokenizer_token() != TOKENIZER_LF && 
     tokenizer_token() != TOKENIZER_ENDOFINPUT &&
     tokenizer_token() != TOKENIZER_CR) {
    
    // Process list item
    if(tokenizer_token() == TOKENIZER_STRING) {
      tokenizer_string(string, sizeof(string));
      sprintf(buf+strlen(buf), "%s", string);
      tokenizer_next();
    } else if(tokenizer_token() == TOKENIZER_TAB) {
      tokenizer_next();
      accept(TOKENIZER_LEFTPAREN);
      int target_col = expr();
      accept(TOKENIZER_RIGHTPAREN);
      int current_col = strlen(buf);
      if (target_col > current_col && target_col < sizeof(buf)) {
        memset(buf + current_col, ' ', target_col - current_col);
        buf[target_col] = '\0';
      }
    } else {
      // variable or expression (or string expression)
      if (tokenizer_stringlookahead()) {
        sprintf(buf+strlen(buf), "%s", sexpr());
      } else {
        sprintf(buf+strlen(buf), "%d", expr());
      }
    }
    
    // Process { ("," | ";") , list }
    while(tokenizer_token() == TOKENIZER_COMMA || 
          tokenizer_token() == TOKENIZER_SEMICOLON) {
      
      // Process separator
      if(tokenizer_token() == TOKENIZER_COMMA) {
        int current_col = strlen(buf);
        int next_zone = ((current_col / UBASIC_PRINT_ZONE_WIDTH) + 1) * UBASIC_PRINT_ZONE_WIDTH;
        if (next_zone < sizeof(buf)) {
          memset(buf + current_col, ' ', next_zone - current_col);
          buf[next_zone] = '\0';
        }
        tokenizer_next();
      } else { // TOKENIZER_SEMICOLON
        tokenizer_next();
      }
      
      // After separator, process list item (required by EBNF)
      if(tokenizer_token() == TOKENIZER_STRING) {
        tokenizer_string(string, sizeof(string));
        sprintf(buf+strlen(buf), "%s", string);
        tokenizer_next();
      } else if(tokenizer_token() == TOKENIZER_TAB) {
        tokenizer_next();
        accept(TOKENIZER_LEFTPAREN);
        int target_col = expr();
        accept(TOKENIZER_RIGHTPAREN);
        int current_col = strlen(buf);
        if (target_col > current_col && target_col < sizeof(buf)) {
          memset(buf + current_col, ' ', target_col - current_col);
          buf[target_col] = '\0';
        }
      } else if(tokenizer_token() == TOKENIZER_LF || 
                tokenizer_token() == TOKENIZER_ENDOFINPUT ||
                tokenizer_token() == TOKENIZER_CR) {
        // Trailing separator - valid in BASIC, just ends the line
        break;
      } else {
        // variable or expression (or string expression)
        if (tokenizer_stringlookahead()) {
          sprintf(buf+strlen(buf), "%s", sexpr());
        } else {
          sprintf(buf+strlen(buf), "%d", expr());
        }
      }
    }
  }
  
  // Handle CR if present
  if(tokenizer_token() == TOKENIZER_CR) {
    tokenizer_next();
  }
  
  out_function(buf);
  out_function("\n");
  DEBUG_PRINTF("print_statement: End of print.\n");
  tokenizer_next();
}
/*---------------------------------------------------------------------------*/
static void input_statement(void) {
  static char input_buf[256];
  char *input_ptr;
  char *token_start;
  int var;
  int is_string;
  int has_prompt = 0;
  
  accept(TOKENIZER_INPUT);
  
  /* Check for optional prompt string */
  if (tokenizer_token() == TOKENIZER_STRING) {
    tokenizer_string(string, sizeof(string));
    out_function(string);
    has_prompt = 1;
    accept(TOKENIZER_STRING);
    
    /* Prompt can be followed by semicolon or comma */
    if (tokenizer_token() == TOKENIZER_SEMICOLON) {
      accept(TOKENIZER_SEMICOLON);
    } else if (tokenizer_token() == TOKENIZER_COMMA) {
      accept(TOKENIZER_COMMA);
    }
    
    /* Always add ? prompt after custom prompt */
    out_function("? ");
  } else {
    /* Show default prompt if no custom prompt */
    out_function("? ");
  }
  
  /* Read input line */
  in_function(input_buf, sizeof(input_buf));
  input_ptr = input_buf;
  
  DEBUG_PRINTF("input_statement: Read input '%s'\n", input_buf);
  
  /* Parse variables and assign values */
  do {
    /* Determine variable type */
    if (tokenizer_token() == TOKENIZER_VARIABLE) {
      var = tokenizer_variable_num();
      is_string = 0;
      accept(TOKENIZER_VARIABLE);
    } else if (tokenizer_token() == TOKENIZER_STRINGVARIABLE) {
      var = tokenizer_variable_num();
      is_string = 1;
      accept(TOKENIZER_STRINGVARIABLE);
    } else {
      DEBUG_PRINTF("input_statement: Expected variable.\n");
      ended = 1;
      return;
    }
    
    /* Skip leading whitespace in input */
    while (*input_ptr == ' ' || *input_ptr == '\t') {
      input_ptr++;
    }
    
    /* Parse value from input */
    if (is_string) {
      /* String variable - read until comma or end */
      token_start = input_ptr;
      while (*input_ptr != ',' && *input_ptr != '\0') {
        input_ptr++;
      }
      
      /* Copy string value */
      if (input_ptr > token_start) {
        size_t len = input_ptr - token_start;
        if (len > MAX_STRINGVARLEN) {
          len = MAX_STRINGVARLEN;
        }
        memcpy(string, token_start, len);
        string[len] = '\0';
        
        /* Trim trailing whitespace */
        while (len > 0 && (string[len-1] == ' ' || string[len-1] == '\t')) {
          len--;
          string[len] = '\0';
        }
        
        set_stringvariable(var, string);
      } else {
        set_stringvariable(var, "");
      }
    } else {
      /* Numeric variable */
      VARIABLE_TYPE value = 0;
      int negative = 0;
      
      /* Handle negative sign */
      if (*input_ptr == '-') {
        negative = 1;
        input_ptr++;
      } else if (*input_ptr == '+') {
        input_ptr++;
      }
      
      /* Parse digits */
      while (*input_ptr >= '0' && *input_ptr <= '9') {
        value = value * 10 + (*input_ptr - '0');
        input_ptr++;
      }
      
      if (negative) {
        value = -value;
      }
      
      set_variable(var, value);
    }
    
    /* Skip comma in input if present */
    if (*input_ptr == ',') {
      input_ptr++;
    }
    
    /* Check for more variables */
    if (tokenizer_token() == TOKENIZER_COMMA) {
      accept(TOKENIZER_COMMA);
    } else {
      break;
    }
  } while (tokenizer_token() != TOKENIZER_LF && 
           tokenizer_token() != TOKENIZER_ENDOFINPUT);
  
  accept(TOKENIZER_LF);
  DEBUG_PRINTF("input_statement: Input complete.\n");
}
/*---------------------------------------------------------------------------*/
static void change_statement(void){
  /* CHANGE statement - dropped in Dartmouth BASIC v5
   * Syntax: CHANGE string_var TO numeric_var
   *     or: CHANGE numeric_var TO string_var
   * This statement is recognized but not implemented.
   */
  accept(TOKENIZER_CHANGE);
  
  /* Parse the first variable (either string or numeric) */
  if (tokenizer_token() == TOKENIZER_STRINGVARIABLE) {
    accept(TOKENIZER_STRINGVARIABLE);
  } else if (tokenizer_token() == TOKENIZER_VARIABLE) {
    accept(TOKENIZER_VARIABLE);
    /* Handle potential array indexing */
    if (tokenizer_token() == TOKENIZER_LEFTPAREN) {
      accept(TOKENIZER_LEFTPAREN);
      expr();
      if (tokenizer_token() == TOKENIZER_COMMA) {
        accept(TOKENIZER_COMMA);
        expr();
      }
      accept(TOKENIZER_RIGHTPAREN);
    }
  } else {
    DEBUG_PRINTF("change_statement: Expected variable\n");
    exit(1);
  }
  
  /* Accept TO keyword */
  accept(TOKENIZER_TO);
  
  /* Parse the second variable (either string or numeric) */
  if (tokenizer_token() == TOKENIZER_STRINGVARIABLE) {
    accept(TOKENIZER_STRINGVARIABLE);
  } else if (tokenizer_token() == TOKENIZER_VARIABLE) {
    accept(TOKENIZER_VARIABLE);
    /* Handle potential array indexing */
    if (tokenizer_token() == TOKENIZER_LEFTPAREN) {
      accept(TOKENIZER_LEFTPAREN);
      expr();
      if (tokenizer_token() == TOKENIZER_COMMA) {
        accept(TOKENIZER_COMMA);
        expr();
      }
      accept(TOKENIZER_RIGHTPAREN);
    }
  } else {
    DEBUG_PRINTF("change_statement: Expected variable\n");
    exit(1);
  }
  
  accept(TOKENIZER_LF);
  
  /* CHANGE is not implemented - was dropped in Dartmouth BASIC v5 */
  DEBUG_PRINTF("CHANGE statement not implemented (dropped in Dartmouth BASIC v5)\n");
  exit(1);
}
/*---------------------------------------------------------------------------*/
static void data_statement(void){
  /* DATA statement - skip during normal execution */
  accept(TOKENIZER_DATA);
  DEBUG_PRINTF("data_statement: Skipping DATA.\n");
  tokenizer_skip();
}
/*---------------------------------------------------------------------------*/
static void restore_statement(void){
  /* RESTORE statement - reset DATA pointer to start of program */
  accept(TOKENIZER_RESTORE);
  accept(TOKENIZER_LF);
  
  data_ptr = tokenizer_start();
  DEBUG_PRINTF("restore_statement: DATA pointer reset to start.\n");
}
/*---------------------------------------------------------------------------*/
static void read_statement(void){
  /* READ statement - read values from DATA statements
   * Based on C# working implementation:
   * 1. Save current tokenizer position
   * 2. Initialize tokenizer at data_ptr
   * 3. Search forward for next DATA statement
   * 4. Read the next value from DATA
   * 5. Update data_ptr
   * 6. Restore tokenizer position
   */
  const char *saved_pos;
  int var;
  uint16_t idx1 = 0, idx2 = 0, idx3 = 0;
  int is_array = 0;
  int is_string = 0;
  VARIABLE_TYPE value;
  char str_value[MAX_STRINGLEN];
  int found_data;
  
  accept(TOKENIZER_READ);
  
  /* Loop through variables to read */
  do {
    is_array = 0;
    is_string = 0;
    found_data = 0;
    
    /* Determine variable type */
    if (tokenizer_token() == TOKENIZER_STRINGVARIABLE) {
      is_string = 1;
      var = tokenizer_variable_num();
      accept(TOKENIZER_STRINGVARIABLE);
      
      /* String arrays not supported in current implementation */
      if (tokenizer_token() == TOKENIZER_LEFTPAREN) {
        DEBUG_PRINTF("read_statement: String arrays not supported\n");
        exit(1);
      }
    } else if (tokenizer_token() == TOKENIZER_VARIABLE) {
      var = tokenizer_variable_num();
      accept(TOKENIZER_VARIABLE);
      
      /* Check for array indexing */
      if (tokenizer_token() == TOKENIZER_LEFTPAREN) {
        is_array = 1;
        accept(TOKENIZER_LEFTPAREN);
        idx1 = (uint16_t)expr();
        if (tokenizer_token() == TOKENIZER_COMMA) {
          accept(TOKENIZER_COMMA);
          idx2 = (uint16_t)expr();
          if (tokenizer_token() == TOKENIZER_COMMA) {
            accept(TOKENIZER_COMMA);
            idx3 = (uint16_t)expr();
          }
        }
        accept(TOKENIZER_RIGHTPAREN);
      }
    } else {
      DEBUG_PRINTF("read_statement: Expected variable\n");
      exit(1);
    }
    
    /* Save current tokenizer position */
    saved_pos = tokenizer_pos();
    
    /* Search from data_ptr for next DATA value */
    tokenizer_goto(data_ptr);
    
    /* Search for next DATA statement */
    while (tokenizer_token() != TOKENIZER_ENDOFINPUT && !found_data) {
      /* Skip to next line or DATA statement */
      while (tokenizer_token() != TOKENIZER_LF &&
             tokenizer_token() != TOKENIZER_DATA &&
             tokenizer_token() != TOKENIZER_ENDOFINPUT) {
        tokenizer_next();
      }
      
      if (tokenizer_token() == TOKENIZER_DATA) {
        accept(TOKENIZER_DATA);
        found_data = 1;
        break;
      }
      
      if (tokenizer_token() == TOKENIZER_LF) {
        tokenizer_next();
      }
    }
    
    if (!found_data) {
      DEBUG_PRINTF("read_statement: Out of DATA\n");
      tokenizer_goto(saved_pos);
      exit(1);
    }
    
    /* Read value from DATA statement */
    if (is_string) {
      /* Read string value */
      if (tokenizer_token() == TOKENIZER_STRING) {
        tokenizer_string(str_value, sizeof(str_value));
        accept(TOKENIZER_STRING);
      } else if (tokenizer_token() == TOKENIZER_NUMBER) {
        /* Allow numbers in string variables */
        snprintf(str_value, sizeof(str_value), "%d", tokenizer_num());
        accept(TOKENIZER_NUMBER);
      } else {
        DEBUG_PRINTF("read_statement: Expected string or number in DATA\n");
        tokenizer_goto(saved_pos);
        exit(1);
      }
      
      /* Assign to string variable */
      set_stringvariable(var, str_value);
    } else {
      /* Read numeric value */
      if (tokenizer_token() == TOKENIZER_NUMBER) {
        value = tokenizer_num();
        accept(TOKENIZER_NUMBER);
      } else if (tokenizer_token() == TOKENIZER_STRING) {
        /* Try to parse string as number */
        tokenizer_string(str_value, sizeof(str_value));
        value = atoi(str_value);
        accept(TOKENIZER_STRING);
      } else {
        DEBUG_PRINTF("read_statement: Expected number in DATA\n");
        tokenizer_goto(saved_pos);
        exit(1);
      }
      
      /* Assign to numeric variable */
      if (is_array) {
        set_array_element(var, idx1, idx2, idx3, value);
      } else {
        set_variable(var, value);
      }
    }
    
    /* Update data_ptr to current position (after the value we just read) */
    /* Skip comma if present */
    if (tokenizer_token() == TOKENIZER_COMMA) {
      accept(TOKENIZER_COMMA);
    }
    data_ptr = tokenizer_pos();
    
    /* Restore tokenizer to READ statement */
    tokenizer_goto(saved_pos);
    
    /* Check for more variables in READ */
    if (tokenizer_token() == TOKENIZER_COMMA) {
      accept(TOKENIZER_COMMA);
    } else {
      break;
    }
  } while (tokenizer_token() != TOKENIZER_LF && 
           tokenizer_token() != TOKENIZER_ENDOFINPUT);
  
  accept(TOKENIZER_LF);
  DEBUG_PRINTF("read_statement: READ complete.\n");
}
/*---------------------------------------------------------------------------*/
static void if_statement(void){
  int r;

  accept(TOKENIZER_IF);

  r = relation();
  #if VERBOSE
  	DEBUG_PRINTF("if_statement: relation %d.\n", r);
  #endif
  accept(TOKENIZER_THEN);
  if(r) {
    statement();
  } else {
    do {
      tokenizer_next();
    } while(tokenizer_token() != TOKENIZER_ELSE &&
        tokenizer_token() != TOKENIZER_LF &&
        tokenizer_token() != TOKENIZER_ENDOFINPUT);
    if(tokenizer_token() == TOKENIZER_ELSE) {
      tokenizer_next();
      statement();
    } else if(tokenizer_token() == TOKENIZER_LF) {
      tokenizer_next();
    }
  }
}
/*---------------------------------------------------------------------------*/
static void let_statement(void){
// string additions here
  int var;
  uint16_t idx1 = 0, idx2 = 0, idx3 = 0;
  int is_array = 0;
  
  if (tokenizer_token() == TOKENIZER_VARIABLE) {
     var = tokenizer_variable_num();
     accept(TOKENIZER_VARIABLE);
     
     /* Check for array indexing */
     if (tokenizer_token() == TOKENIZER_LEFTPAREN) {
       is_array = 1;
       accept(TOKENIZER_LEFTPAREN);
       idx1 = (uint16_t)expr();
       
       if (tokenizer_token() == TOKENIZER_COMMA) {
         accept(TOKENIZER_COMMA);
         idx2 = (uint16_t)expr();
         
         if (tokenizer_token() == TOKENIZER_COMMA) {
           accept(TOKENIZER_COMMA);
           idx3 = (uint16_t)expr();
         }
       }
       
       accept(TOKENIZER_RIGHTPAREN);
     }
     
     accept(TOKENIZER_EQ);
     
     if (is_array) {
       set_array_element(var, idx1, idx2, idx3, expr());
     } else {
       set_variable(var, expr());
     }
     
	 #if VERBOSE
     if (is_array) {
       DEBUG_PRINTF("let_statement: assign to array %c(%u", 'a'+var, (unsigned)idx1);
       if (idx2 > 0) DEBUG_PRINTF(",%u", (unsigned)idx2);
       if (idx3 > 0) DEBUG_PRINTF(",%u", (unsigned)idx3);
       DEBUG_PRINTF(")\n");
     } else {
       DEBUG_PRINTF("let_statement: assign %d to %d.\n", get_variable(var), var);
     }
     #endif
	 accept(TOKENIZER_LF);
  } else if (tokenizer_token() == TOKENIZER_STRINGVARIABLE) {
     var = tokenizer_variable_num();
	 accept(TOKENIZER_STRINGVARIABLE);
     accept(TOKENIZER_EQ);
	 set_stringvariable(var, sexpr());
	 DEBUG_PRINTF("let_statement: string assign '%s' to %d\n", get_stringvariable(var), var);
	 accept(TOKENIZER_LF);

  }
  // end of string additions
}
/*---------------------------------------------------------------------------*/
static void gosub_statement(void) {
  int linenum;
  accept(TOKENIZER_GOSUB);
  linenum = tokenizer_num();
  accept(TOKENIZER_NUMBER);
  accept(TOKENIZER_LF);
  if(*gosub_depth_cell < UBASIC_MAX_GOSUB_STACK_DEPTH) {

    /* Push the current return line into the configured gosub stack (memory or internal). */
    gosub_push(tokenizer_num());

    jump_linenum(linenum);
  } else {
    DEBUG_PRINTF("gosub_statement: gosub stack exhausted.\n");
  }
}
/*---------------------------------------------------------------------------*/
static void return_statement(void){
  accept(TOKENIZER_RETURN);
  if(*gosub_depth_cell > 0) {
    int32_t retline = gosub_pop();
    jump_linenum(retline);
  } else {
    DEBUG_PRINTF("return_statement: non-matching return.\n");
  }
}
/*---------------------------------------------------------------------------*/
static void rem_statement(void) {
  accept(TOKENIZER_REM);
  DEBUG_PRINTF("rem_statement: Skipping comment.\n");
  tokenizer_skip();
  if (tokenizer_token() == TOKENIZER_LF) {
    accept(TOKENIZER_LF);
  }
}
/*---------------------------------------------------------------------------*/
static void next_statement(void){
  int var;

  accept(TOKENIZER_NEXT);
  var = tokenizer_variable_num();
  accept(TOKENIZER_VARIABLE);
  if(*for_depth_cell > 0 &&
     var == (int)for_stack[*for_depth_cell - 1].for_variable_index) {
    for_state top = for_stack[*for_depth_cell - 1];
    VARIABLE_TYPE new_value = get_variable(var) + top.step;
    set_variable(var, new_value);
    
    /* Check loop condition based on step direction */
    int continue_loop;
    if (top.step >= 0) {
      continue_loop = (new_value <= top.to);
    } else {
      continue_loop = (new_value >= top.to);
    }
    
    if(continue_loop) {
      jump_linenum(top.line_after_for);
    } else {
      for_pop();
      accept(TOKENIZER_LF);
    }
  } else {
    if (*for_depth_cell > 0) {
      DEBUG_PRINTF("next_statement: non-matching next (expected %d, found %d).\n",
          (int)for_stack[*for_depth_cell - 1].for_variable_index, var);
    }
    accept(TOKENIZER_LF);
  }

}
/*---------------------------------------------------------------------------*/
static void for_statement(void) {
  int for_variable, to, step;

  accept(TOKENIZER_FOR);
  for_variable = tokenizer_variable_num();
  accept(TOKENIZER_VARIABLE);
  accept(TOKENIZER_EQ);
  set_variable(for_variable, expr());
  accept(TOKENIZER_TO);
  to = expr();
  
  /* Optional STEP parameter, defaults to 1 */
  if (tokenizer_token() == TOKENIZER_STEP) {
    accept(TOKENIZER_STEP);
    step = expr();
  } else {
    step = 1;
  }
  
  accept(TOKENIZER_LF);

  for_state st;
  st.line_after_for = (int32_t)tokenizer_num();
  st.for_variable_index = (int32_t)for_variable;
  st.to = (int32_t)to;
  st.step = (int32_t)step;
  for_push(st);
  #if VERBOSE
    DEBUG_PRINTF("for_statement: new for, var %d to %d step %d.\n", (int)st.for_variable_index, (int)st.to, (int)st.step);
  #endif
  
}
/*---------------------------------------------------------------------------*/
static void peek_statement(void){
  VARIABLE_TYPE peek_addr;
  int var;

  accept(TOKENIZER_PEEK);
  peek_addr = expr();
  accept(TOKENIZER_COMMA);
  var = tokenizer_variable_num();
  accept(TOKENIZER_VARIABLE);
  accept(TOKENIZER_LF);

  set_variable(var, peek_function(peek_addr));
}
/*---------------------------------------------------------------------------*/
static void poke_statement(void)
{
  VARIABLE_TYPE poke_addr;
  VARIABLE_TYPE value;

  accept(TOKENIZER_POKE);
  poke_addr = expr();
  accept(TOKENIZER_COMMA);
  value = expr();
  accept(TOKENIZER_LF);

  poke_function(poke_addr, value);
}
/*---------------------------------------------------------------------------*/
static void end_statement(void)
{
  accept(TOKENIZER_END);
  DEBUG_PRINTF("end_statement: Ending program.\n");
  ended = 1;
}
/*---------------------------------------------------------------------------*/
static void dim_statement(void) {
  int var;
  char varname;
  var_desc_t *v;
  uint16_t dim1, dim2 = 1, dim3 = 1;
  uint16_t total_elements;
  uint16_t data_bytes;
  uint16_t desc_offset, data_offset;
  array_desc_t *arr_desc;
  VARIABLE_TYPE *arr_data;
  uint16_t i;
  
  accept(TOKENIZER_DIM);
  
  if (tokenizer_token() != TOKENIZER_VARIABLE) {
    DEBUG_PRINTF("dim_statement: Expected variable name.\n");
    ended = 1;
    return;
  }
  
  var = tokenizer_variable_num();
  varname = 'a' + (char)var;
  accept(TOKENIZER_VARIABLE);
  accept(TOKENIZER_LEFTPAREN);
  
  /* Parse dimensions */
  dim1 = (uint16_t)expr();
  
  if (tokenizer_token() == TOKENIZER_COMMA) {
    accept(TOKENIZER_COMMA);
    dim2 = (uint16_t)expr();
    
    if (tokenizer_token() == TOKENIZER_COMMA) {
      accept(TOKENIZER_COMMA);
      dim3 = (uint16_t)expr();
    }
  }
  
  accept(TOKENIZER_RIGHTPAREN);
  accept(TOKENIZER_LF);
  
  /* Validate dimensions */
  if (dim1 == 0 || dim2 == 0 || dim3 == 0) {
    DEBUG_PRINTF("dim_statement: Invalid dimension (zero).\n");
    ended = 1;
    return;
  }
  
  /* Calculate total size */
  total_elements = dim1 * dim2 * dim3;
  data_bytes = total_elements * sizeof(VARIABLE_TYPE);
  
  #if VERBOSE
    DEBUG_PRINTF("dim_statement: DIM %c(%u", varname, (unsigned)dim1);
    if (dim2 > 1) DEBUG_PRINTF(",%u", (unsigned)dim2);
    if (dim3 > 1) DEBUG_PRINTF(",%u", (unsigned)dim3);
    DEBUG_PRINTF(") = %u elements, %u bytes\n", 
                 (unsigned)total_elements, (unsigned)data_bytes);
  #endif
  
  /* Create array variable */
  v = find_or_create_variable(varname, VARTYPE_ARRAY);
  if (v == NULL) {
    DEBUG_PRINTF("dim_statement: Failed to create variable.\n");
    ended = 1;
    return;
  }
  
  /* Allocate array descriptor */
  desc_offset = heap_alloc(sizeof(array_desc_t));
  if (desc_offset == 0) {
    DEBUG_PRINTF("dim_statement: Out of memory for array descriptor.\n");
    ended = 1;
    return;
  }
  v->arr_offset = desc_offset;
  
  /* Allocate array data */
  data_offset = heap_alloc(data_bytes);
  if (data_offset == 0) {
    DEBUG_PRINTF("dim_statement: Out of memory for array data.\n");
    ended = 1;
    return;
  }
  
  /* Initialize array descriptor */
  arr_desc = (array_desc_t *)(mem_base + desc_offset);
  arr_desc->data_offset = data_offset;
  arr_desc->dimensions = (dim2 > 1) ? ((dim3 > 1) ? 3 : 2) : 1;
  arr_desc->dim_size[0] = dim1;
  arr_desc->dim_size[1] = dim2;
  arr_desc->dim_size[2] = dim3;
  
  /* Initialize array data to zero */
  arr_data = (VARIABLE_TYPE *)(mem_base + data_offset);
  for (i = 0; i < total_elements; i++) {
    arr_data[i] = 0;
  }
  
  #if VERBOSE
    DEBUG_PRINTF("dim_statement: Array descriptor at %u, data at %u\n",
                 (unsigned)desc_offset, (unsigned)data_offset);
  #endif
}
/*---------------------------------------------------------------------------*/
static void statement(void){
  int token;

  token = tokenizer_token();

  switch(token) {
  case TOKENIZER_PRINT:
    print_statement();
    break;
  case TOKENIZER_INPUT:
    input_statement();
    break;
  case TOKENIZER_DIM:
    dim_statement();
    break;
  case TOKENIZER_IF:
    if_statement();
    break;
  case TOKENIZER_GOTO:
    goto_statement();
    break;
  case TOKENIZER_GOSUB:
    gosub_statement();
    break;
  case TOKENIZER_RETURN:
    return_statement();
    break;
  case TOKENIZER_FOR:
    for_statement();
    break;
  case TOKENIZER_PEEK:
    peek_statement();
    break;
  case TOKENIZER_POKE:
    poke_statement();
    break;
  case TOKENIZER_NEXT:
    next_statement();
    break;
  case TOKENIZER_ON:
    on_statement();
    break;
  case TOKENIZER_CHANGE:
    change_statement();
    break;
  case TOKENIZER_DATA:
    data_statement();
    break;
  case TOKENIZER_RESTORE:
    restore_statement();
    break;
  case TOKENIZER_READ:
    read_statement();
    break;
  case TOKENIZER_END:
    end_statement();
    break;
  case TOKENIZER_LET:
    accept(TOKENIZER_LET);
    /* Fall through. */
  case TOKENIZER_VARIABLE:
  // string addition
  case TOKENIZER_STRINGVARIABLE:
  // end of string addition
    let_statement();
    break;
  case TOKENIZER_REM:
    rem_statement();
    break;
  default:
    DEBUG_PRINTF("statement: not implemented %d.\n", token);
    exit(1);
  }
}
/*---------------------------------------------------------------------------*/
static void line_statement(void){
  #if VERBOSE
    DEBUG_PRINTF("----------- Line number %d ---------\n", tokenizer_num());
  #endif
  index_add(tokenizer_num(), tokenizer_pos());
  accept(TOKENIZER_NUMBER);
  statement();
}
/*---------------------------------------------------------------------------*/
static void set_variable(int varnum, VARIABLE_TYPE value) {
  char varname = 'a' + (char)varnum;
  var_desc_t *v;
  
  if (varnum < 0 || varnum >= 26) return;
  
  v = find_or_create_variable(varname, VARTYPE_INTEGER);
  if (v != NULL) {
    v->int_value = value;
  }
}
/*---------------------------------------------------------------------------*/
static VARIABLE_TYPE get_variable(int varnum) {
  char varname = 'a' + (char)varnum;
  var_desc_t *v;
  
  if (varnum < 0 || varnum >= 26) return 0;
  
  v = find_or_create_variable(varname, VARTYPE_INTEGER);
  return (v != NULL) ? v->int_value : 0;
}
/*---------------------------------------------------------------------------*/
// string additions
/*---------------------------------------------------------------------------*/
void set_stringvariable(int svarnum, char *svalue) {
  char varname = 'a' + (char)svarnum;
  var_desc_t *v;
  size_t len;
  uint16_t offset;

  if (svalue == NULL || mem_base == NULL) return;
  if (svarnum < 0 || svarnum >= 26) return;
  
  v = find_or_create_variable(varname, VARTYPE_STRING);
  if (v == NULL) return;
  
  len = strlen(svalue);
  if (len > MAX_STRINGVARLEN) {
    len = MAX_STRINGVARLEN;
  }
  if (len == 0) {
    v->str_offset = 0;
    return;
  }
  
  /* Allocate in string pool */
  offset = str_pool_alloc((uint16_t)(len + 1));
  if (offset == 0) return;
  
  memcpy(mem_base + offset, svalue, len);
  mem_base[offset + len] = '\0';
  v->str_offset = offset;
  
  #if VERBOSE
    DEBUG_PRINTF("set_stringvariable: Assigned '%s' to %c$ at offset %u\n",
                 svalue, varname, (unsigned)offset);
  #endif
}
/*---------------------------------------------------------------------------*/
char *get_stringvariable(int varnum) {
  char varname = 'a' + (char)varnum;
  var_desc_t *v;
  
  if (varnum < 0 || varnum >= 26 || mem_base == NULL) {
    return scpy(nullstring);
  }
  
  v = find_variable(varname);
  if (v == NULL || v->type != VARTYPE_STRING) {
    return scpy(nullstring);
  }
  
  if (v->str_offset == 0) {
    return scpy(nullstring);
  }
  
  return (char *)(mem_base + v->str_offset);
}
/*---------------------------------------------------------------------------*/
static VARIABLE_TYPE get_array_element(int varnum, uint16_t idx1, uint16_t idx2, uint16_t idx3) {
  char varname = 'a' + (char)varnum;
  var_desc_t *v;
  array_desc_t *arr_desc;
  VARIABLE_TYPE *arr_data;
  uint16_t offset;
  
  if (varnum < 0 || varnum >= 26 || mem_base == NULL) {
    return 0;
  }
  
  v = find_variable(varname);
  if (v == NULL || v->type != VARTYPE_ARRAY) {
    DEBUG_PRINTF("get_array_element: Variable %c is not an array\n", varname);
    return 0;
  }
  
  arr_desc = (array_desc_t *)(mem_base + v->arr_offset);
  
  /* Validate indices */
  if (idx1 >= arr_desc->dim_size[0]) {
    DEBUG_PRINTF("get_array_element: Index 1 (%u) out of bounds (max %u)\n", 
                 (unsigned)idx1, (unsigned)arr_desc->dim_size[0]);
    return 0;
  }
  if (arr_desc->dimensions >= 2 && idx2 >= arr_desc->dim_size[1]) {
    DEBUG_PRINTF("get_array_element: Index 2 (%u) out of bounds (max %u)\n", 
                 (unsigned)idx2, (unsigned)arr_desc->dim_size[1]);
    return 0;
  }
  if (arr_desc->dimensions >= 3 && idx3 >= arr_desc->dim_size[2]) {
    DEBUG_PRINTF("get_array_element: Index 3 (%u) out of bounds (max %u)\n", 
                 (unsigned)idx3, (unsigned)arr_desc->dim_size[2]);
    return 0;
  }
  
  /* Calculate linear offset */
  if (arr_desc->dimensions == 1) {
    offset = idx1;
  } else if (arr_desc->dimensions == 2) {
    offset = idx1 * arr_desc->dim_size[1] + idx2;
  } else {
    offset = (idx1 * arr_desc->dim_size[1] + idx2) * arr_desc->dim_size[2] + idx3;
  }
  
  arr_data = (VARIABLE_TYPE *)(mem_base + arr_desc->data_offset);
  return arr_data[offset];
}
/*---------------------------------------------------------------------------*/
static void set_array_element(int varnum, uint16_t idx1, uint16_t idx2, uint16_t idx3, VARIABLE_TYPE value) {
  char varname = 'a' + (char)varnum;
  var_desc_t *v;
  array_desc_t *arr_desc;
  VARIABLE_TYPE *arr_data;
  uint16_t offset;
  
  if (varnum < 0 || varnum >= 26 || mem_base == NULL) {
    return;
  }
  
  v = find_variable(varname);
  if (v == NULL || v->type != VARTYPE_ARRAY) {
    DEBUG_PRINTF("set_array_element: Variable %c is not an array\n", varname);
    ended = 1;
    return;
  }
  
  arr_desc = (array_desc_t *)(mem_base + v->arr_offset);
  
  /* Validate indices */
  if (idx1 >= arr_desc->dim_size[0]) {
    DEBUG_PRINTF("set_array_element: Index 1 (%u) out of bounds (max %u)\n", 
                 (unsigned)idx1, (unsigned)arr_desc->dim_size[0]);
    ended = 1;
    return;
  }
  if (arr_desc->dimensions >= 2 && idx2 >= arr_desc->dim_size[1]) {
    DEBUG_PRINTF("set_array_element: Index 2 (%u) out of bounds (max %u)\n", 
                 (unsigned)idx2, (unsigned)arr_desc->dim_size[1]);
    ended = 1;
    return;
  }
  if (arr_desc->dimensions >= 3 && idx3 >= arr_desc->dim_size[2]) {
    DEBUG_PRINTF("set_array_element: Index 3 (%u) out of bounds (max %u)\n", 
                 (unsigned)idx3, (unsigned)arr_desc->dim_size[2]);
    ended = 1;
    return;
  }
  
  /* Calculate linear offset */
  if (arr_desc->dimensions == 1) {
    offset = idx1;
  } else if (arr_desc->dimensions == 2) {
    offset = idx1 * arr_desc->dim_size[1] + idx2;
  } else {
    offset = (idx1 * arr_desc->dim_size[1] + idx2) * arr_desc->dim_size[2] + idx3;
  }
  
  arr_data = (VARIABLE_TYPE *)(mem_base + arr_desc->data_offset);
  arr_data[offset] = value;
  
  #if VERBOSE
    DEBUG_PRINTF("set_array_element: %c(%u", varname, (unsigned)idx1);
    if (arr_desc->dimensions >= 2) DEBUG_PRINTF(",%u", (unsigned)idx2);
    if (arr_desc->dimensions >= 3) DEBUG_PRINTF(",%u", (unsigned)idx3);
    DEBUG_PRINTF(") = %d\n", (int)value);
  #endif
}
/*---------------------------------------------------------------------------*/
void ubasic_set_out_function(out_func func) {
  out_function = func;
}
/*---------------------------------------------------------------------------*/
void ubasic_set_put_function(put_func func) {
  put_function = func;
}
/*---------------------------------------------------------------------------*/
void ubasic_set_in_function(in_func func) {
  in_function = func;
}
/*---------------------------------------------------------------------------*/
void ubasic_set_get_function(get_func func) {
  get_function = func;
}
/*---------------------------------------------------------------------------*/
static int32_t gosub_pop(void) {
  int32_t ptr = *gosub_depth_cell;
  if (ptr == 0) {
    DEBUG_PRINTF("gosub_pop: underflow (ptr=0)\n");
    return 0;
  }
  ptr--;
  *gosub_depth_cell = ptr;
  return gosub_stack_mem[ptr];
}

/*---------------------------------------------------------------------------*/
// end of string additions
/*---------------------------------------------------------------------------*/
static void gosub_push(int32_t line_num) {
  int32_t ptr = *gosub_depth_cell;
  if (ptr >= UBASIC_MAX_GOSUB_STACK_DEPTH) {
    DEBUG_PRINTF("gosub_push: overflow (ptr=%d)\n", (int)ptr);
    return;
  }
  gosub_stack_mem[ptr] = line_num;
  *gosub_depth_cell = ptr + 1;
}
/*---------------------------------------------------------------------------*/
static void for_push(for_state state) {
  int32_t depth = *for_depth_cell;
  if (depth < UBASIC_MAX_FOR_STACK_DEPTH) {
    for_stack[depth++] = state;
    *for_depth_cell = depth;
  } else {
    DEBUG_PRINTF("for_push: for stack depth exceeded.\n");
  }
}
/*---------------------------------------------------------------------------*/
static for_state for_pop(void) {
  for_state error_state = {-1, '\0', 0};
  int32_t depth = *for_depth_cell;
  if (depth > 0) {
    for_state value = for_stack[--depth];
    *for_depth_cell = depth;
    return value;
  } else {
    DEBUG_PRINTF("for_pop: for stack underflow.\n");
    return error_state;
  }
}
/*---------------------------------------------------------------------------*/



