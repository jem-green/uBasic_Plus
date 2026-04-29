# Bug Fix: READ Statement Finding DATA in REM Comments

## Problem Description

The READ statement's search for DATA statements was incorrectly finding the keyword "DATA" inside REM comments. This caused READ to fail with "Expected number in DATA" errors.

### Example
```basic
10 REM MULTIPLE DATA STATEMENTS TEST
20 READ a, b, c
60 DATA 1, 2, 3
```

In this program, the READ statement at line 20 would search from the beginning and tokenize line 10. When the tokenizer encountered the word "DATA" in the comment text "MULTIPLE DATA STATEMENTS TEST", it would return TOKENIZER_DATA. The READ statement would then accept this token and expect numeric values to follow, but instead found the rest of the comment text "STATEMENTS TEST", causing an error.

## Root Cause

The READ statement's search loop:
```c
while (tokenizer_token() != TOKENIZER_LF &&
       tokenizer_token() != TOKENIZER_DATA &&
       tokenizer_token() != TOKENIZER_ENDOFINPUT) {
  tokenizer_next();
}
```

This loop advanced through all tokens looking for TOKENIZER_DATA, but it didn't skip REM statements. The tokenizer treats REM as just another keyword, so it continues tokenizing the rest of the line. If that line happens to contain text that matches a keyword (like "DATA"), the tokenizer returns that keyword token.

## Solution

Added REM detection to the search loop, with immediate skip to end of line when REM is encountered:

```c
while (tokenizer_token() != TOKENIZER_LF &&
       tokenizer_token() != TOKENIZER_DATA &&
       tokenizer_token() != TOKENIZER_REM &&
       tokenizer_token() != TOKENIZER_ENDOFINPUT) {
  tokenizer_next();
}

if (tokenizer_token() == TOKENIZER_DATA) {
  accept(TOKENIZER_DATA);
  found_data = 1;
  break;
}

if (tokenizer_token() == TOKENIZER_REM) {
  /* Skip REM comments - go to end of line */
  tokenizer_skip();
  continue;
}
```

## Files Modified

- **ubasic.c**: Modified `read_statement()` function (lines ~1635-1665)
  - Added TOKENIZER_REM to inner search loop condition
  - Added REM handler that calls `tokenizer_skip()` to advance past comment
  - Added debug output to track token position after finding DATA

- **test-data-string.bas**: Fixed variable case (A$,B$,C$ → a$,b$,c$)
  - String variables should be lowercase per project convention

## Testing

All tests now pass:

- **test-data-simple.bas**: Basic READ/DATA with numeric values ✅
- **test-data-multiple.bas**: Multiple DATA statements with REM comment ✅
- **test-data-restore.bas**: RESTORE functionality ✅
- **test-data-string.bas**: String variables in DATA ✅
- **test-data-debug.bas**: Single value READ ✅

### Test Output Examples

```
test-data-multiple.bas:
A=1 B=2 C=3
X=4 Y=5 Z=6

test-data-string.bas:
Names: Alice, Bob, Charlie

test-data-restore.bas:
First: A=100 B=-56
After RESTORE: C=100 D=-56
```

## Impact

This bug affected **any program with a REM comment containing BASIC keywords** before DATA statements. Common documentation patterns like "REM DATA FORMAT" or "REM READ THESE VALUES" would trigger the bug.

The fix ensures that:
1. REM comments are completely skipped during DATA search
2. Only actual DATA statements (line start → line_number → DATA) are found
3. Comments can safely contain any text without interfering with READ

## Prevention

Similar search loops in other statements should also check for REM:
- Any statement that searches through program text
- Any statement that advances through tokens looking for keywords
- Consider adding a `tokenizer_skip_rem()` utility function if this pattern recurs

## Related Issues

This was initially misdiagnosed as a "line numbering bug" because tests with line numbers ≥10 were failing. The actual cause was that those tests included REM comments with keywords in them.
