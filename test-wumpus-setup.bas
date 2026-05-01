10 INPUT i$
20 IF i$="N" THEN 40
30 PRINT "Instructions"
40 DIM s(20,3)
50 FOR j=1 TO 3
60 FOR k=1 TO 2
70 READ s(j,k)
80 NEXT k
90 NEXT j
100 DATA 2,5,1,3,2,4
110 PRINT "Setup done"
120 PRINT "s(1,1)=";s(1,1)
130 PRINT "s(2,2)=";s(2,2)
