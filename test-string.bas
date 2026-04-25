1 print "start of test"
2 let a$= "abcdefghi"
3 b$="123456789"
5 print "Length of a$=", len(a$)
6 print "Length of b$=", len(b$)
7 if len(a$) = len(b$) then print "same length"
8 if (a$ = b$) then print "same string"
9 c$=left$(a$+ b$,12)
10 print c$
11 c$=right$(a$+b$, 12)
12 print c$
13 c$=mid$(a$+b$, 8,8)
14 print c$
15 c$=str$(13+42)
16 print c$
17 print len(c$)
18 print len("this" + "that")
19 c$ = chr$(34)
20 print c$
21 j = asc(c$)
22 print j
23 print val("12345")
24 i=instr(3, "123456789", "67")
24 print "position of '67' in '123456789' is", i
25 print mid$(a$,2,2)+"xyx"
30 print "end of test"
