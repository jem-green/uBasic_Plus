# uBasic Error Codes

This document lists all runtime error codes emitted by the uBasic interpreter.

## Error Code Reference

| Code | Description | Severity | Line Reported | Notes |
|------|-------------|----------|---------------|-------|
| `RS` | RETURN without GOSUB | Fatal | Current line | Attempted RETURN statement when no GOSUB call is active. Sets `ended = 1`. |
| `GS` | GOSUB stack overflow | Fatal | Current line | GOSUB nesting exceeded max depth (default: 10). Sets `ended = 1`. |
| `NF` | NEXT without FOR | Fatal | Current line | Attempted NEXT without matching FOR loop. Sets `ended = 1`. |
| `MN` | Non-matching NEXT | Fatal | Current line | NEXT variable does not match the top FOR loop variable. Sets `ended = 1`. |
| `FS` | FOR stack overflow | Fatal | Current line | FOR loop nesting exceeded max depth (default: 4). Sets `ended = 1`. |
| `FS` | FOR stack underflow | Fatal | Current line | Internal FOR stack corruption. Sets `ended = 1`. |
| `PS` | PUSH stack overflow | Fatal | Current line | PUSH nesting exceeded max depth (default: 16). Sets `ended = 1`. |
| `PS` | POP stack underflow | Fatal | Current line | POP executed with empty PUSH/POP stack. Sets `ended = 1`. |
| `PT` | POP type mismatch | Fatal | Current line | POP target type does not match pushed value type. Sets `ended = 1`. |
| `/0` | Division by zero | Fatal | Current line | Arithmetic expression attempted division by zero. Sets `ended = 1`. |
| `/0` | Modulo by zero | Fatal | Current line | Arithmetic expression attempted modulo by zero (applies to `%` and `MOD`). Sets `ended = 1`. |
| `DM` | Domain error | Fatal | Current line | Math function domain error (e.g. `SQR`, `LOG`, `POWER`). Sets `ended = 1`. |
| `OF` | Overflow | Fatal | Current line | Math overflow (currently `EXP`). Sets `ended = 1`. |
| `TS` | Token syntax mismatch | Fatal | Current line | Parser token did not match expected grammar token. Sets `ended = 1`. |
| `TS` | Unsupported statement | Fatal | Current line | Parser encountered unimplemented or invalid statement token. Sets `ended = 1`. |
| `TS` | POP requires variable target | Fatal | Current line | POP was not followed by a numeric or string variable target. Sets `ended = 1`. |
| `UL` | Undefined line target | Fatal | Calling line | GOTO or GOSUB references a line number that does not exist. Sets `ended = 1`. |

## Stack Limits

- **GOSUB stack**: Maximum 10 nested calls (set by `UBASIC_MAX_GOSUB_STACK_DEPTH`)
- **FOR stack**: Maximum 4 nested loops (set by `UBASIC_MAX_FOR_STACK_DEPTH`)
- **PUSH stack**: Maximum 16 entries (set by `UBASIC_MAX_PUSH_STACK_DEPTH`)

## Error State Retrieval

After execution, the last error state can be queried using:

```c
const char *ubasic_last_error_code(void);       /* Returns 2-char code string */
const char *ubasic_last_error_description(void);  /* Returns human-readable text */
uint32_t ubasic_last_error_line(void);           /* Returns line number where error occurred */
```

## Silent Failures (No Structured Error)

The following conditions set `ended = 1` but do not populate error state:

- String variable pool exhaustion (out of memory during string assignment)
- String pool compaction failure (buffer size exceeded during GC)
- Temporary string expression buffer exhaustion (in expr helpers like scpy, sconcat, etc.)
- Invalid stored string variable offset (memory corruption detection)

These are logged via `DEBUG_PRINTF` when DEBUG/VERBOSE flags are enabled.

## Default Error Handler

The default error handler prints to `stderr`:

```
<CODE> <DESCRIPTION> on line <LINE_NUMBER>
```

Or if line number is 0:

```
<CODE> <DESCRIPTION>
```

A custom error handler can be installed via:

```c
void ubasic_set_error_function(err_func func);
```

where `err_func` has signature:

```c
typedef void (*err_func)(const char *code, const char *description, uint32_t line);
```
