# PowerShell script to convert BASIC keywords to UPPERCASE in all .bas files

$keywords = @(
    'print', 'let', 'if', 'then', 'else', 'for', 'to', 'step', 'next',
    'goto', 'gosub', 'return', 'end', 'rem', 'dim', 'input',
    'on', 'call', 'peek', 'poke',
    'data', 'read', 'restore', 'change', 'tab',
    'abs', 'int', 'sgn', 'sqr', 'rnd', 'sin', 'cos', 'tan', 'atn', 'exp', 'log',
    'val', 'len', 'instr', 'asc'
)

# Special handling for keywords with $ (must match exactly with $)
$dollarKeywords = @('left$', 'right$', 'mid$', 'str$', 'chr$')

# Get all .bas files in current directory
$basFiles = Get-ChildItem -Path . -Filter *.bas

foreach ($file in $basFiles) {
    Write-Host "Processing $($file.Name)..."
    $content = Get-Content $file.FullName -Raw
    
    # Replace dollar keywords first (need exact match with $)
    foreach ($keyword in $dollarKeywords) {
        $uppercase = $keyword.ToUpper()
        # Escape the $ for regex
        $escapedKeyword = [regex]::Escape($keyword)
        $pattern = "(?i)(?<![a-zA-Z_])$escapedKeyword(?![a-zA-Z_])"
        $content = $content -replace $pattern, $uppercase
    }
    
    # Replace regular keywords
    # Use word boundaries to avoid partial matches
    foreach ($keyword in $keywords) {
        $uppercase = $keyword.ToUpper()
        # Match keyword as whole word (preceded/followed by space, (, ;, ", or line boundary)
        $pattern = "(?i)(?<![a-zA-Z_])$keyword(?![a-zA-Z_\$])"
        $content = $content -replace $pattern, $uppercase
    }
    
    # Write back to file
    Set-Content -Path $file.FullName -Value $content -NoNewline
}

Write-Host "Conversion complete!"
