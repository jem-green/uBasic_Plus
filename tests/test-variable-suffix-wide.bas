10 REM Wider test for bare and suffixed variable names
20 LET a=11
30 LET a0=12
40 LET a9=19
50 LET z=21
60 LET z0=22
70 LET z9=29
80 LET a$="A"
90 LET a0$="A0"
100 LET a9$="A9"
110 LET z$="Z"
120 LET z0$="Z0"
130 LET z9$="Z9"
140 DIM b(3)
150 DIM b9(3)
160 DIM c$(3)
170 DIM c9$(3)
180 LET b(0)=101
190 LET b(1)=102
200 LET b(2)=103
210 LET b9(0)=901
220 LET b9(1)=902
230 LET b9(2)=903
240 LET c$(0)="C0"
250 LET c$(1)="C1"
260 LET c$(2)="C2"
270 LET c9$(0)="Q0"
280 LET c9$(1)="Q1"
290 LET c9$(2)="Q2"
300 PRINT "NUM:";a;",";a0;",";a9;",";z;",";z0;",";z9
310 PRINT "STR:";a$;",";a0$;",";a9$;",";z$;",";z0$;",";z9$
320 PRINT "ARR B:";b(0);",";b(1);",";b(2)
330 PRINT "ARR B9:";b9(0);",";b9(1);",";b9(2)
340 PRINT "ARR C$:";c$(0);",";c$(1);",";c$(2)
350 PRINT "ARR C9$:";c9$(0);",";c9$(1);",";c9$(2)
360 END
