cl cli_ubasic.c ubasic.c tokenizer.c
del ubasic.exe /Q
copy cli_ubasic.exe ubasic.exe
cl /Zi /Od /Feubasicd.exe cli_ubasic.c ubasic.c tokenizer.c