# CountLines.ps1
param(
    [string]$RepoPath = "."
)

# 定义要统计的文件扩展名（可以按需要添加）
$extensions = @("*.cs", "*.shader", "*.hlsl", "*.cginc", "*.uxml", "*.uss")

Write-Host "正在统计代码行数...`n"

$totalLines = 0

foreach ($ext in $extensions) {
    $files = Get-ChildItem -Path $RepoPath -Recurse -Include $ext -File
    foreach ($file in $files) {
        $lines = (Get-Content $file.FullName | Measure-Object -Line).Lines
        $totalLines += $lines
    }
}

Write-Host "总代码行数: $totalLines"
