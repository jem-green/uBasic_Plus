# UPPERCASE Keywords Conversion - Summary

## Issue Discovered

The initial keyword conversion script incorrectly uppercased variable names, which caused errors because the tokenizer treats lowercase and uppercase variables as **different variables**:

```c
int tokenizer_variable_num(void) {
  if(*ptr >= 'a' && *ptr <= 'z') {
    return *ptr - 'a';           // Variables 0-25
  }
  if(*ptr >= 'A' && *ptr <= 'Z') {
    return 26 + (*ptr - 'A');   // Variables 26-51
  }
  return -1;
}
```

So `a` maps to variable 0, but `A` maps to variable 26 - they are DIFFERENT variables!

## What Should Be UPPERCASE

**Keywords and Functions:**
- Statements: PRINT, LET, IF, THEN, ELSE, FOR, TO, STEP, NEXT, GOTO, GOSUB, RETURN, END, REM, DIM, INPUT, ON, CALL, PEEK, POKE
- Data statements: DATA, READ, RESTORE, CHANGE
- Functions: TAB, ABS, INT, SGN, SQR, RND, SIN, COS, TAN, ATN, EXP, LOG
- String functions: LEFT$, RIGHT$, MID$, STR$, CHR$, VAL, LEN, INSTR, ASC

**Example:**
```basic
10 PRINT "Hello"
20 LET a = 5
30 FOR i = 1 TO 10
40 PRINT TAB(5);i
50 NEXT i
```

## What Should Stay lowercase

**Variable names:**
- Numeric variables: `a`, `b`, `x`, `count`, etc.
- String variables: `a$`, `name$`, `text$`, etc.
- Array variables: `a(1)`, `matrix(i,j)`, etc.

**Example:**
```basic
10 LET a = 100
20 LET b$ = "test"
30 READ x, y, z
40 PRINT a; b$
```

## Files Fixed

The following files had uppercase variables that were corrected to lowercase:

1. **test-data-multiple.bas** - Changed `A, B, C, X, Y, Z` → `a, b, c, x, y, z`
2. **test-line9.bas** - Changed `A` → `a`
3. **test-data-lines1-9.bas** - Changed `A` → `a`
4. **test-data-restore.bas** - Changed `A, B, C, D` → `a, b, c, d`
5. **test-string.bas** - Changed `left$, right$, mid$, str$, chr$` → `LEFT$, RIGHT$, MID$, STR$, CHR$`

## Verified Working

✅ test-math.bas - Arithmetic operations  
✅ test-print.bas - PRINT with comma/semicolon  
✅ test-tab.bas - TAB function  
✅ test-operators.bas - Relational operators  
✅ test-string.bas - String functions  
✅ test-data-debug.bas - DATA/READ  
✅ test-data-lines1-9.bas - DATA/READ with lines 1-9  

## Convention Summary

**Dartmouth BASIC v4 Syntax:**
- **Keywords**: UPPERCASE (matches EBNF specification)
- **Variables**: lowercase (convention and compatibility)
- **Line numbers**: numeric (1, 10, 20, etc.)

This follows the standard Dartmouth BASIC style where keywords are capitalized for clarity, while variables remain lowercase.
