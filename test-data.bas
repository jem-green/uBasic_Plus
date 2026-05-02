10 REM Test READ, DATA, and RESTORE statements
20 PRINT "READ/DATA/RESTORE TEST"
30 PRINT "="
50 REM Test 1: Basic READ and DATA
60 PRINT "Test 1: Basic numeric READ"
70 READ a,b,c
80 PRINT "A=";a,"B=";b,"C=";c
90 DATA 10,20,30
100 PRINT "="
120 REM Test 2: Multiple DATA statements
130 PRINT "Test 2: Reading from multiple DATA lines"
140 READ x,y,z
150 PRINT "X=";x,"Y=";y,"Z=";z
160 DATA 70,80
170 DATA 90
180 PRINT "="
200 REM Test 3: RESTORE TO reuse DATA
210 PRINT "Test 3: RESTORE resets DATA pointer"
220 RESTORE
230 READ p,q,r,s,t,u
240 PRINT "P=";p,"Q=";q,"R=";r,"S=";s,"T=";t,"U=";u
250 PRINT "="
270 REM Test 4: String DATA
280 PRINT "Test 4: String DATA"
290 READ n$,c$
300 PRINT "Name: ";n$
310 PRINT "City: ";c$
320 DATA "Alice","Boston"
330 PRINT "="
350 REM Test 5: Mixed numeric and string
360 PRINT "Test 5: Mixed DATA types"
370 RESTORE
380 READ d
390 READ e
400 READ f
410 PRINT "V1=";d,"V2=";e,"V3=";f
420 PRINT "="
440 PRINT "All tests completed"
450 END
