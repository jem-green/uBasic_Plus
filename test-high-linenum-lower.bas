10 PRINT "Testing high line numbers with lowercase vars"
20 LET a = 100
30 PRINT "a = "; a
40 GOTO 50000
50000 PRINT "Success! Jumped to line 50000"
50010 LET b = 200  
50020 PRINT "b = "; b
50030 GOSUB 100000
50040 PRINT "Returned from GOSUB"
50050 GOTO 1000000
100000 PRINT "In subroutine at line 100000"
100010 LET c = 300
100020 PRINT "c = "; c
100030 RETURN
1000000 PRINT "Success! Reached line 1000000"
1000010 FOR i = 1 TO 3
1000020 PRINT "Loop i = "; i
1000030 NEXT i
1000040 PRINT "All high line number tests passed!"
1000050 END
