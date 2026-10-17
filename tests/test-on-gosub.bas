10 LET a=1
20 ON a GOSUB 100,200,300
30 PRINT "This line should be printed"
40 END
100 PRINT "This is the first GOSUB subroutine"
110 RETURN
120 PRINT "This is the second GOSUB subroutine"
130 RETURN
140 PRINT "This is the third GOSUB subroutine"
150 RETURN
160 END