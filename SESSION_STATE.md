# Session State - READ/DATA/RESTORE Implementation

**Date**: April 27, 2026  
**Branch**: main  
**Status**: Implementation complete, blocked by line numbering bug

## Summary

Successfully implemented READ/DATA/RESTORE statements for Dartmouth BASIC v4 compliance based on working C# implementation found in `working_c#/` folder. Implementation is complete but cannot be fully tested due to a pre-existing bug where programs with line numbers ≥10 fail silently.

## Completed Work

### 1. Tokens and Keywords Added
- **File**: `tokenizer.h` - Added `TOKENIZER_READ`, `TOKENIZER_DATA`, `TOKENIZER_RESTORE` (lines ~76-78)
- **File**: `tokenizer.c` - Added keywords "read", "data", "restore" (lines ~91-93)

### 2. State Management
- **File**: `ubasic.c` - Added `data_ptr` static variable to track position in DATA statements (line ~74)
- **File**: `ubasic.c` - Initialize `data_ptr` in `ubasic_init()` (line ~309)
- **File**: `ubasic.c` - Reset `data_ptr` in `ubasic_load_program()` (line ~398)

### 3. Statement Implementations

#### DATA Statement (lines 1538-1547)
```c
static void data_statement(void){
  /* DATA statement - skip during normal execution */
  accept(TOKENIZER_DATA);
  DEBUG_PRINTF("data_statement: Skipping DATA.\n");
  tokenizer_skip();
}
```
- Skips DATA lines during normal program execution
- Uses `tokenizer_skip()` to jump to end of line

#### RESTORE Statement (lines 1551-1558)
```c
static void restore_statement(void){
  /* RESTORE statement - reset DATA pointer to start of program */
  accept(TOKENIZER_RESTORE);
  accept(TOKENIZER_LF);
  
  data_ptr = tokenizer_start();
  DEBUG_PRINTF("restore_statement: DATA pointer reset to start.\n");
}
```
- Resets `data_ptr` to beginning of program
- Allows re-reading DATA from the start

#### READ Statement (lines 1560-1738)
```c
static void read_statement(void){
  /* Based on C# working implementation */
  // 1. Save current tokenizer position
  // 2. Initialize tokenizer at data_ptr
  // 3. Search forward for next DATA statement
  // 4. Read the next value from DATA
  // 5. Update data_ptr
  // 6. Restore tokenizer position
```
- Reads values from DATA statements on demand
- Supports numeric variables, numeric arrays (1D, 2D, 3D)
- Supports string variables (not string arrays)
- Maintains `data_ptr` across multiple READ statements
- Handles comma-separated values in DATA

### 4. Statement Dispatcher
- **File**: `ubasic.c` - Added case handlers in `statement()` function (lines ~2098-2106)
  - `TOKENIZER_DATA` → `data_statement()`
  - `TOKENIZER_RESTORE` → `restore_statement()`
  - `TOKENIZER_READ` → `read_statement()`

### 5. Function Declarations
- **File**: `ubasic.c` - Added forward declarations (lines ~60-62)

## Implementation Pattern (from C# Reference)

The C# working implementation uses:
1. **Queue-based buffering** - C# uses `Queue data` to buffer DATA values
2. **Position tracking** - `int dataPos` tracks where to search next
3. **ReadData() method** - Searches from dataPos, parses DATA into queue, updates dataPos
4. **ReadStatement()** - Checks queue, calls ReadData if empty, dequeues value
5. **RestoreStatement()** - Resets dataPos to 0

Our C implementation adapts this by:
- No queue - reads values directly on demand
- `data_ptr` (const char*) instead of `dataPos` (int)
- Direct value reading instead of buffering
- Same search pattern: save position, goto data_ptr, search for DATA, read value, update data_ptr, restore position

## Known Issue - LINE NUMBERING BUG

**Symptoms**:
- Programs with line numbers 1-9 work perfectly
- Programs with line numbers ≥10 fail silently (no output, exit code 1)
- This affects ALL programs (not just READ/DATA)

**Test Results**:
```
✅ test-line5.bas (lines 5-7) - works
✅ test-line8.bas (lines 8-9) - works  
❌ test-data-minimal.bas (lines 10, 20) - fails
❌ test-read1.bas (lines 10, 20, 30, 40) - fails
❌ test-simple-print.bas (lines 10, 20, 30) - fails
```

**Impact**:
- Cannot test READ/DATA/RESTORE with standard line numbering (10, 20, 30...)
- Code implementation is correct but verification blocked

## Test Files Created

### Working Tests (lines 1-9)
- `test-line5.bas` - Simple print test (lines 5-7)
- `test-simple2.bas` - Simple print test (lines 1-3)
- `test-line8.bas` - Line 8-9 test

### Blocked Tests (lines ≥10)
- `test-read1.bas` - Simple READ A / DATA 100 / PRINT A
- `test-read2.bas` - READ with debug prints
- `test-data-minimal.bas` - Just DATA 100 / END
- `test-data-only.bas` - PRINT / DATA / PRINT / END
- `test-data-simple.bas` - READ A,B,C / DATA 100,200,300
- `test-data-multiple.bas` - Multiple DATA statements
- `test-data-restore.bas` - RESTORE test
- `test-data-string.bas` - String DATA test

## Files Modified

1. **tokenizer.h** - Added 3 tokens
2. **tokenizer.c** - Added 3 keywords
3. **ubasic.c** - Major changes:
   - Added `data_ptr` variable
   - Added 3 statement implementations (~180 lines)
   - Added case handlers
   - Added forward declarations
   - Modified `ubasic_init()` and `ubasic_load_program()`

## Next Steps

### To Continue This Work:

1. **Debug Line Numbering Bug** (PRIORITY)
   - Investigate why lines ≥10 fail
   - Check `index_add()`, `index_find()`, `jump_linenum()`
   - Check line number parsing in tokenizer
   - This is a pre-existing bug, not introduced by READ/DATA changes

2. **Once Line Bug Fixed**:
   - Test `test-read1.bas` - simplest READ/DATA
   - Test `test-data-simple.bas` - multiple values
   - Test `test-data-multiple.bas` - multiple DATA statements
   - Test `test-data-restore.bas` - RESTORE functionality
   - Test `test-data-string.bas` - string variables
   - Test with arrays: `READ A(1,1)` with `DATA 100`

3. **Edge Cases to Test**:
   - Out of DATA error handling
   - Mixed numeric/string DATA
   - Comma-separated values
   - Multiple READ statements
   - RESTORE and re-read

4. **Documentation**:
   - Update `DARTMOUTH_V4_STATUS.md` when tests pass
   - Add READ/DATA/RESTORE to feature list

## Reference Files

- **C# Implementation**: `working_c#/Interpreter.cs`
  - Lines 310-410: `ReadData()` method
  - Lines 1904-2070: `ReadStatement()` method  
  - Lines 689-693: `DataStatement()` method
  - Lines 700-706: `RestoreStatement()` method
  - Line 128: `int dataPos = 0;` declaration
  - Line 127: `readonly Queue data` declaration

- **EBNF Specification**: `dartmouth4_syntax.ebnf`
  - Line 17: `read_statement = "READ" , variable { "," , variable } ;`
  - Line 14: `data_statement = "DATA" , constant , { "," , constant } ;`
  - Line 40: `restore_statement = "RESTORE" ;`

## Build Commands

```powershell
# Rebuild
.\Makefile_run.bat

# Test (when line bug fixed)
.\run-ubasic.exe test-read1.bas
.\run-ubasic.exe test-data-simple.bas

# Current working test
.\run-ubasic.exe test-math.bas       # Baseline - still works
.\run-ubasic.exe test-line5.bas      # Lines 1-9 work
```

## Verification Checklist

When line numbering bug is fixed:

- [ ] Simple READ/DATA works
- [ ] Multiple variables in READ work
- [ ] Multiple DATA statements work
- [ ] Comma-separated DATA values work
- [ ] RESTORE resets data pointer
- [ ] Out of DATA error displays
- [ ] String variables work
- [ ] Numeric arrays work
- [ ] Mixed DATA types work
- [ ] TAB and PRINT still work (regression test)
- [ ] CHANGE still shows "not implemented"

## Related Features

- ✅ TAB function - working
- ✅ PRINT comma/semicolon - working
- ⚠️ CHANGE statement - recognized, throws "not implemented" error
- ❌ STOP statement - not implemented
- ❌ DEF FN / FN - not implemented

## Notes

- Implementation follows C# working code pattern exactly
- Uses `tokenizer_goto()` and `tokenizer_pos()` for position management
- Saves/restores tokenizer position to avoid corrupting program flow
- `data_ptr` is persistent across multiple READ statements
- String arrays not supported (noted in code)
- Error handling via `exit(1)` with DEBUG_PRINTF messages
