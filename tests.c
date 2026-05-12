/*
 * Copyright (c) 2006, Adam Dunkels
 * Copyright (c) 2013, Danyil Bohdan
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

#include <time.h>
#include <stdio.h>
#include <assert.h>
#include "ubasic.h"

#define MEMORY_SIZE 4096

static const char program_let[] =
"10 LET a = 42\n\
20 END\n";

static const char program_goto[] =
"10 GOTO 50\n\
20 GOTO 40\n\
30 GOTO 60\n\
40 GOTO 30\n\
50 GOTO 20\n\
60 LET c = 108\n\
70 END\n";

static const char program_loop[] =
"10 FOR i = 0 TO 126\n\
20 FOR j = 0 TO 126\n\
30 FOR k = 0 TO 10\n\
40 LET a = i * j * k\n\
45 REM PRINT a, i, j, k\n\
50 NEXT k\n\
60 NEXT j\n\
70 NEXT i\n\
80 END\n";

static const char program_fibs[] =
"20 LET a = 1\n\
40 LET b = 1\n\
60 FOR i = 0 TO 8\n\
80 LET b = a + b\n\
100 LET a = b - a\n\
120 REM PRINT a, b\n\
140 NEXT i\n\
160 END\n";

static const char program_peek_poke[] =
"10 PEEK 100 + 20 + 3, a\n\
20 PEEK 123, z\n\
30 POKE 100 - 1, 99\n\
40 POKE 0, 0\n\
50 END\n";

/*---------------------------------------------------------------------------*/
VARIABLE_TYPE peek(VARIABLE_TYPE arg) {
    return arg;
}

/*---------------------------------------------------------------------------*/
void poke(VARIABLE_TYPE arg, VARIABLE_TYPE value) {
    assert(arg == value);
}

/*---------------------------------------------------------------------------*/
void run(const char program[]) {
  static int test_num = 0;
  static uint8_t memory[MEMORY_SIZE];
  test_num++;
  printf("Running test #%u... ", test_num);
  fflush(stdout);

  clock_t start_t, end_t;
  double delta_t;

  start_t = clock();

  ubasic_init_peek_poke(memory, MEMORY_SIZE, &peek, &poke);
  ubasic_load_program(program);

  do {
    ubasic_run();
  } while(!ubasic_finished());

  end_t = clock();
  delta_t = (double)(end_t - start_t) / CLOCKS_PER_SEC;

  printf("done. Run time: %.3f s\n", delta_t);
}

/*---------------------------------------------------------------------------*/
int
main(void)
{
  run(program_let);
  assert(ubasic_get_variable(0) == 42);

  run(program_goto);
  assert(ubasic_get_variable(2) == 108);

  run(program_loop);
  assert(ubasic_get_variable(0) == (VARIABLE_TYPE)(126 * 126 * 10));

  run(program_fibs);
  assert(ubasic_get_variable(1) == 89);

  run(program_peek_poke);
  assert(ubasic_get_variable(0) == 123);
  assert(ubasic_get_variable(25) == 123);

  return 0;
}
/*---------------------------------------------------------------------------*/
