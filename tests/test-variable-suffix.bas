10 REM Test variable names with optional numeric suffixes
20 LET a=1
30 LET a0=2
40 LET a9=3
50 LET a$="ONE"
60 LET a0$="TWO"
70 DIM b0(2)
80 LET b0(0)=10
90 LET b0(1)=20
100 DIM c9$(2)
110 LET c9$(0)="X"
120 LET c9$(1)="Y"
130 PRINT "A=";a,"A0=";a0,"A9=";a9
140 PRINT "A$=";a$,"A0$=";a0$
150 PRINT "B0(0)=";b0(0),"B0(1)=";b0(1)
160 PRINT "C9$(0)=";c9$(0),"C9$(1)=";c9$(1)
170 END
