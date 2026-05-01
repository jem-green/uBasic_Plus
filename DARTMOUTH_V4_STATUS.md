# Dartmouth BASIC v4 Feature Status

This document tracks the implementation status of Dartmouth BASIC v4 features in uBasic-Plus.

## Implemented Features

### ✅ TAB Function
- **Status**: Fully implemented
- **Syntax**: `TAB(expression)`
- **Usage**: Positions output at specified column within PRINT statement
- **Test**: test-tab.bas

### ✅ PRINT Comma/Semicolon
- **Status**: Fully implemented per EBNF spec
- **Comma**: Advances to next 14-column zone (0, 14, 28, 42, 56...)
- **Semicolon**: Concatenates with no spacing
- **Test**: test-print.bas, test-print-valid.bas

### ✅ READ/DATA/RESTORE Statements
- **Status**: ✅ Fully implemented and tested
- **Syntax**:
  - `READ variable [, variable]*`
  - `DATA constant [, constant]*`
  - `RESTORE`
- **Implementation**: Based on working C# pattern with offset-based data pointer
- **Features**:
  - Reads numeric/string values from DATA statements
  - Supports numeric arrays (1D, 2D, 3D)
  - Supports string variables and string arrays
  - RESTORE resets data pointer to beginning
  - Maintains position across multiple READ statements
  - Skips REM comments when searching for DATA
- **Bug Fix**: Fixed READ statement finding "DATA" keyword inside REM comments
- **Tests**: 
  - test-data-simple.bas ✅ (basic READ/DATA)
  - test-data-multiple.bas ✅ (multiple DATA statements)
  - test-data-restore.bas ✅ (RESTORE functionality)
  - test-data-string.bas ✅ (string variables)
  - test-data-debug.bas ✅ (single value)
  - test-stringarray-read.bas ✅ (string arrays)

### ✅ Numeric Arrays
- **Status**: ✅ Fully implemented and tested
- **Spec Compliance**: Dartmouth v4 mandates 1D and 2D arrays only (per EBNF)
- **Extensions**: 3D arrays added as uBasic-Plus extension
- **Syntax**: 
  - `DIM var(size)` - 1D array (spec)
  - `DIM var(rows, cols)` - 2D array (spec)
  - `DIM var(d1, d2, d3)` - 3D array (extension)
- **Features**:
  - Dynamic allocation from variable heap
  - Full integration with DIM, LET, READ, and expressions
  - Arrays are 0-indexed
- **Tests**:
  - test-array.bas ✅ (1D array basic operations)
  - test-array-2d-let.bas ✅ (2D array with LET - spec)
  - test-array-2d-read.bas ✅ (2D array with READ/DATA - spec)
  - test-array-3d-let.bas ✅ (3D array with LET - extension)
  - test-array-3d-read.bas ✅ (3D array with READ/DATA - extension)

### ✅ String Arrays
- **Status**: ✅ Fully implemented and tested
- **Spec Compliance**: Dartmouth v4 mandates 1D and 2D string arrays only (per EBNF)
- **Extensions**: 3D string arrays added as uBasic-Plus extension
- **History**: String arrays introduced in Dartmouth BASIC v3/v4 (not in original v1)
- **Syntax**:
  - `DIM string_var(size)` - 1D array (spec)
  - `DIM string_var(rows, cols)` - 2D array (spec)
  - `DIM string_var(d1, d2, d3)` - 3D array (extension)
- **Features**:
  - Dynamic allocation from variable heap
  - String pool management with garbage collection
  - Full integration with DIM, LET, READ, and expressions
- **Implementation**:
  - New type `VARTYPE_STRING_ARRAY = 3`
  - Structure `string_array_desc_t` stores dimensions and data offset
  - Array data stores `uint16_t` offsets into string pool
  - Modified `str_pool_compact()` to preserve array strings during GC
- **Tests**:
  - test-stringarray.bas ✅ (1D array with LET - spec)
  - test-stringarray-read.bas ✅ (1D array with READ/DATA - spec)
  - test-stringarray-2d.bas ✅ (2D array with LET - spec)
  - test-stringarray-read-2d.bas ✅ (2D array with READ/DATA - spec)
  - test-stringarray-read-3d.bas ✅ (3D array with READ/DATA - extension)

### ⚠️ CHANGE Statement
- **Status**: Recognized but NOT IMPLEMENTED
- **Syntax**: `CHANGE string_var TO numeric_var` or `CHANGE numeric_var TO string_var`
- **Reason**: Dropped in Dartmouth BASIC v5
- **Behavior**: Parses correctly, throws "not implemented" error
- **Test**: test-change.bas, test-change2.bas, test-change-array.bas

## Not Yet Implemented

### ❌ STOP Statement
- **Status**: Not implemented
- **Syntax**: `STOP`
- **Description**: Halts execution (similar to END but semantically different for debugging)
- **Complexity**: Low (simple statement)

### ✅ DEF FN / FN
- **Status**: ✅ Fully implemented and tested
- **Syntax**: 
  - Definition: `DEF FN letter(variable) = expression`
  - Usage: `FN letter(variable)`
- **Description**: User-defined functions with single parameter
- **Features**:
  - Function table supports A-Z (26 functions)
  - Parameter substitution during evaluation
  - Expression evaluated in function's context
  - Preserves parameter variable state (save/restore)
- **Implementation**:
  - `fn_def_t` structure stores function definitions
  - Static array `fn_table[26]` for A-Z functions
  - DEF statement parser stores expression offsets
  - FN evaluator uses tokenizer_goto for expression evaluation
- **Tests**:
  - test-def-fn.bas ✅ (basic function definitions)
  - Wumpus.bas ✅ (real-world usage with RND functions)

## Removed Features

### 🗑️ READ/DATA/RESTORE (Old Implementation)
- **Status**: Removed (broken implementation, replaced with working version)
- **Reason**: Original implementation was incomplete and buggy
- **Note**: Re-implemented from scratch based on working C# code (see above)

## Notes

- All implementations follow dartmouth4_syntax.ebnf specification
- PRINT zones use UBASIC_PRINT_ZONE_WIDTH constant (14 characters)
- Changes follow COPILOT_C_CONTRACT.md: minimal modifications, extend rather than rewrite
- Keywords are UPPERCASE (case-insensitive matching), variables are lowercase (case-sensitive a≠A)
