10 REM 2D STRING ARRAY READ/DATA TEST
20 DIM t$(2,3)
30 READ t$(0,0), t$(0,1), t$(0,2)
40 READ t$(1,0), t$(1,1), t$(1,2)
50 PRINT "Row 0: "; t$(0,0); " "; t$(0,1); " "; t$(0,2)
60 PRINT "Row 1: "; t$(1,0); " "; t$(1,1); " "; t$(1,2)
70 DATA "One", "Two", "Three"
80 DATA "Four", "Five", "Six"
90 END
