10 print "TAB FUNCTION TEST"
20 print "="
30 print "Basic TAB usage:"
40 print "A";tab(10);"B";tab(20);"C"
50 print "="
60 print "TAB with values:"
70 print tab(5);"Indented 5"
80 print tab(10);"Indented 10"
90 print tab(15);"Indented 15"
100 print "="
110 print "TAB with variables:"
120 let x = 5
130 print tab(x);"Column";x
140 print tab(x*2);"Column";x*2
150 print tab(x*3);"Column";x*3
160 print "="
170 print "Table formatting:"
180 print "NAME";tab(15);"AGE";tab(25);"SCORE"
190 print "----";tab(15);"---";tab(25);"-----"
200 print "ALICE";tab(15);25;tab(25);95
210 print "BOB";tab(15);30;tab(25);87
220 print "CHARLIE";tab(15);22;tab(25);92
230 print "="
240 end
