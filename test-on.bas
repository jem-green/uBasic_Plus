10 print "ON...GOTO AND ON...GOSUB TEST"
20 print ""
30 print "MENU:"
40 print "1. FIRST OPTION"
50 print "2. SECOND OPTION"
60 print "3. THIRD OPTION"
70 print ""
80 input "ENTER CHOICE (1-3)"; c
90 on c goto 100, 200, 300
95 print "INVALID CHOICE"
96 goto 20
100 print "YOU CHOSE OPTION 1"
110 goto 400
200 print "YOU CHOSE OPTION 2"
210 goto 400
300 print "YOU CHOSE OPTION 3"
310 goto 400
400 print ""
410 print "TESTING ON...GOSUB:"
420 let x = 2
430 on x gosub 500, 600, 700
440 print "RETURNED FROM SUBROUTINE"
450 goto 800
500 print "SUBROUTINE 1"
510 return
600 print "SUBROUTINE 2"
610 return
700 print "SUBROUTINE 3"
710 return
800 print ""
810 print "TESTING OUT OF RANGE:"
820 let y = 5
830 on y goto 100, 200, 300
840 print "INDEX OUT OF RANGE - CONTINUED"
850 print ""
860 print "DONE"
870 end
