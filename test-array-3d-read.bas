10 REM 3D NUMERIC ARRAY WITH READ/DATA
20 DIM c(2,2,2)
30 READ c(0,0,0), c(0,0,1), c(0,1,0), c(0,1,1)
40 READ c(1,0,0), c(1,0,1), c(1,1,0), c(1,1,1)
50 PRINT "Layer 0:"
60 PRINT "  "; c(0,0,0); " "; c(0,0,1)
70 PRINT "  "; c(0,1,0); " "; c(0,1,1)
80 PRINT "Layer 1:"
90 PRINT "  "; c(1,0,0); " "; c(1,0,1)
100 PRINT "  "; c(1,1,0); " "; c(1,1,1)
110 DATA 10, 20, 30, 40
120 DATA 50, 60, 70, 80
130 END
