10 PRINT "Testing low line numbers"
20 LET a = 100
30 PRINT "a = ", a
40 GOTO 50000
50000 PRINT "Jump to line 50000 successful!"
50010 LET b = 200
50020 PRINT "b = ", b
50030 GOSUB 100000
50040 PRINT "Returned from gosub"
50050 GOTO 1000000
100000 PRINT "In subroutine at line 100000"
100010 LET c = 300
100020 PRINT "c = ", c
100030 RETURN
1000000 PRINT "Jump to line 1000000 successful!"
1000010 FOR i = 1 TO 5
1000020 PRINT "Loop iteration: ", i
1000030 NEXT i
1000040 GOTO 4000000000
4000000000 PRINT "Testing line number 4000000000 (near uint32_t max)"
4000000010 PRINT "All high line number tests passed!"
4000000020 END
