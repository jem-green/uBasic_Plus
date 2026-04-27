10 rem Test READ, DATA, and RESTORE statements
20 print "READ/DATA/RESTORE TEST"
30 print "="
40 rem
50 rem Test 1: Basic READ and DATA
60 print "Test 1: Basic numeric READ"
70 read a,b,c
80 print "A=";a,"B=";b,"C=";c
90 data 10,20,30
100 print "="
110 rem
120 rem Test 2: Multiple DATA statements
130 print "Test 2: Reading from multiple DATA lines"
140 read x,y,z
150 print "X=";x,"Y=";y,"Z=";z
160 data 100,200
170 data 300
180 print "="
190 rem
200 rem Test 3: RESTORE to reuse data
210 print "Test 3: RESTORE resets data pointer"
220 restore
230 read p,q
240 print "P=";p,"Q=";q
250 print "="
260 rem
270 rem Test 4: String data
280 print "Test 4: String DATA"
290 read n$,c$
300 print "Name: ";n$
310 print "City: ";c$
320 data "Alice","Boston"
330 print "="
340 rem
350 rem Test 5: Mixed numeric and string
360 print "Test 5: Mixed data types"
370 restore
380 read v1
390 read v2
400 read v3
410 print "V1=";v1,"V2=";v2,"V3=";v3
420 print "="
430 rem
440 print "All tests completed"
450 end
