10 REM 3D NUMERIC ARRAY WITH LET
20 DIM c(2,2,2)
30 LET c(0,0,0) = 10
40 LET c(0,0,1) = 20
50 LET c(0,1,0) = 30
60 LET c(0,1,1) = 40
70 LET c(1,0,0) = 50
80 LET c(1,0,1) = 60
90 LET c(1,1,0) = 70
100 LET c(1,1,1) = 80
110 PRINT "Layer 0:"
120 PRINT "  "; c(0,0,0); " "; c(0,0,1)
130 PRINT "  "; c(0,1,0); " "; c(0,1,1)
140 PRINT "Layer 1:"
150 PRINT "  "; c(1,0,0); " "; c(1,0,1)
160 PRINT "  "; c(1,1,0); " "; c(1,1,1)
170 END
