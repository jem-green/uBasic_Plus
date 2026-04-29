# PowerShell script to convert BASIC keywords to UPPERCASE while preserving lowercase variable names

# Define all BASIC keywords (these should be uppercased)
$keywords = @(
    'PRINT', 'LET', 'IF', 'THEN', 'ELSE', 'FOR', 'TO', 'STEP', 'NEXT',
    'GOTO', 'GOSUB', 'RETURN', 'END', 'REM', 'DIM', 'INPUT',
    'ON', 'CALL', 'PEEK', 'POKE',
    'DATA', 'READ', 'RESTORE', 'CHANGE', 'TAB',
    'ABS', 'INT', 'SGN', 'SQR', 'RND', 'SIN', 'COS', 'TAN', 'ATN', 'EXP', 'LOG',
    'LEFT$', 'RIGHT$', 'MID$', 'STR$', 'CHR$', 'VAL', 'LEN', 'INSTR', 'ASC'
)

# Get all .bas files in current directory
$basFiles = Get-ChildItem -Path . -Filter *.bas

foreach ($file in $basFiles) {
    Write-Host "Processing $($file.Name)..."
    $lines = Get-Content $file.FullName
    $newLines = @()
    
    foreach ($line in $lines) {
        $newLine = $line
        
        # Process each keyword
        foreach ($keyword in $keywords) {
            # Case-insensitive replacement, matching whole words
            # Use \b for word boundary to avoid partial matches
            # This preserves variable names like 'a', 'b', 'x$', 'name$'
            $pattern = "\b$keyword\b"
            $newLine = $newLine -replace "(?i)$pattern", $keyword
        }
        
        $newLines += $newLine
    }
    
    # Write back to file
    $newLines | Set-Content -Path $file.FullName
}

Write-Host "Conversion complete! Keywords uppercased, variables preserved."
