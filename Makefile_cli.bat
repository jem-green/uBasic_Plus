cl cli_ubasic.c ubasic.c tokenizer.c
rem del ubasic.exe /Q
rem copy cli_ubasic.exe ubasic.exe
cl /Zi /Od /Feubasic.exe cli_ubasic.c ubasic.c tokenizer.c