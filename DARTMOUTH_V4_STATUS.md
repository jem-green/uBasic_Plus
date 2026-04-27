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

### ⚠️ READ/DATA/RESTORE Statements
- **Status**: Implemented but UNTESTED (blocked by line numbering bug)
- **Syntax**:
  - `READ variable [, variable]*`
  - `DATA constant [, constant]*`
  - `RESTORE`
- **Implementation**: Complete, based on working C# version
- **Features**:
  - Reads numeric/string values from DATA statements
  - Supports numeric arrays (1D, 2D, 3D)
  - Supports string variables (not string arrays)
  - RESTORE resets data pointer to beginning
  - Maintains position across multiple READ statements
- **Blocker**: Programs with line numbers ≥10 fail (pre-existing bug)
- **Test**: test-read1.bas, test-data-simple.bas (cannot run until line bug fixed)
- **See**: SESSION_STATE.md for details

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

### ❌ DEF FN / FN
- **Status**: Not implemented
- **Syntax**: `DEF FN letter(variable) = expression`
- **Usage**: `FN letter(variable)`
- **Description**: User-defined functions
- **Complexity**: Medium (requires function table, evaluation context)

## Removed Features

### 🗑️ READ/DATA/RESTORE (Old Implementation)
- **Status**: Removed (broken implementation, replaced with working version)
- **Reason**: Original implementation was incomplete and buggy
- **Note**: Re-implemented from scratch based on working C# code (see above)

## Notes

- All implementations follow dartmouth4_syntax.ebnf specification
- PRINT zones use UBASIC_PRINT_ZONE_WIDTH constant (14 characters)
- Changes follow COPILOT_C_CONTRACT.md: minimal modifications, extend rather than rewrite

## Known Issues

### Line Numbering Bug (BLOCKING)
- **Symptom**: Programs with line numbers ≥10 fail silently
- **Impact**: Cannot test READ/DATA/RESTORE with standard line numbering (10, 20, 30...)
- **Status**: Under investigation
- **Workaround**: Use line numbers 1-9 for testing
- **Affected**: ALL programs, not specific to any feature
