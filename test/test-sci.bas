10 PRINT "Scientific Notation Tests"
20 PRINT "========================="
30 PRINT ""
40 PRINT "Negative exponents:"
50 LET a = 1E-1
60 PRINT "1E-1   = "; a
70 LET a = 1.5E-2
80 PRINT "1.5E-2 = "; a
90 LET a = 2.5E-3
100 PRINT "2.5E-3 = "; a
110 LET a = -1E-1
120 PRINT "-1E-1  = "; a
130 PRINT ""
140 PRINT "Positive exponents:"
150 LET a = 1E1
160 PRINT "1E1    = "; a
170 LET a = 1E2
180 PRINT "1E2    = "; a
190 LET a = 1E3
200 PRINT "1E3    = "; a
210 LET a = 1.5E4
220 PRINT "1.5E4  = "; a
230 LET a = 1E10
240 PRINT "1E10   = "; a
250 PRINT ""
260 PRINT "Mixed:"
270 LET a = 3.141592E0
280 PRINT "3.141592E0  = "; a
290 LET a = 6.022E3
300 PRINT "6.022E3     = "; a
310 LET a = -2.5E2
320 PRINT "-2.5E2      = "; a
330 LET a = 1e-1
340 PRINT "1e-1 (lower)= "; a
350 PRINT ""
360 PRINT "Arithmetic with scientific:"
370 LET a = 1E3 + 5E2
380 PRINT "1E3 + 5E2  = "; a
390 LET a = 1E4 * 1E-2
400 PRINT "1E4 * 1E-2 = "; a
410 LET a = 1E6 / 1E3
420 PRINT "1E6 / 1E3  = "; a
430 PRINT ""
440 END
