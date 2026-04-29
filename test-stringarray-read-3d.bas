10 REM 3D STRING ARRAY READ/DATA TEST
20 DIM c$(2,2,2)
30 READ c$(0,0,0), c$(0,0,1), c$(0,1,0), c$(0,1,1)
40 READ c$(1,0,0), c$(1,0,1), c$(1,1,0), c$(1,1,1)
50 PRINT "Layer 0:"
60 PRINT "  "; c$(0,0,0); " "; c$(0,0,1)
70 PRINT "  "; c$(0,1,0); " "; c$(0,1,1)
80 PRINT "Layer 1:"
90 PRINT "  "; c$(1,0,0); " "; c$(1,0,1)
100 PRINT "  "; c$(1,1,0); " "; c$(1,1,1)
110 DATA "A1", "A2", "A3", "A4"
120 DATA "B1", "B2", "B3", "B4"
130 END
