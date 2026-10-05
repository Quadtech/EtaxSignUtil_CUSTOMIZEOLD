$ErrorActionPreference = 'Stop'

# Standalone regression check; does not need ERP, Java, a Token, or a database.
$source = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '..\EtaxSignUtil\JavaRuntimeLocator.cs'))
Add-Type -TypeDefinition ($source + @'
public static class JavaLocatorProbe
{
    public static string[] Find() { return EtaxSignUtil.JavaRuntimeLocator.GetExecutablePaths().ToArray(); }
}
'@)

$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('EtaxJavaLocator-' + [Guid]::NewGuid().ToString('N'))
$oldPath = $env:PATH
$oldJavaHome = $env:JAVA_HOME
$oldProbeRoot = $env:ETAX_JAVA_LOCATOR_TEST_ROOT
try {
    $firstDirectory = Join-Path $testRoot 'Other Vendor\runtime'
    $secondDirectory = Join-Path $testRoot 'second-runtime'
    $homeDirectory = Join-Path $testRoot 'jdk-home'
    $homeBin = Join-Path $homeDirectory 'bin'
    foreach ($directory in @($firstDirectory, $secondDirectory, $homeBin)) {
        [void][IO.Directory]::CreateDirectory($directory)
        [IO.File]::WriteAllBytes((Join-Path $directory 'java.exe'), [byte[]]@())
    }
    $first = Join-Path $firstDirectory 'java.exe'
    $second = Join-Path $secondDirectory 'java.exe'
    $homeJava = Join-Path $homeBin 'java.exe'

    # The reported client scenario: Java exists in PATH, with no JAVA_HOME.
    $env:JAVA_HOME = $null
    $env:PATH = (Join-Path $testRoot 'missing') + ';"' + $firstDirectory + '"'
    $found = [JavaLocatorProbe]::Find()
    if ($found.Count -eq 0 -or $found[0] -ne $first) { throw 'PATH-only Java discovery failed.' }
    Write-Output 'PASS: PATH-only discovery with spaces and no vendor-specific path'

    # PATH order wins over JAVA_HOME; duplicate paths and empty entries are skipped.
    $env:JAVA_HOME = $homeDirectory
    $env:PATH = $firstDirectory + ';;' + $firstDirectory.ToUpperInvariant() + ';' + $secondDirectory
    $found = [JavaLocatorProbe]::Find()
    if ($found.Count -lt 3 -or $found[0] -ne $first -or $found[1] -ne $second -or $found[2] -ne $homeJava) {
        throw 'Discovery order or case-insensitive de-duplication failed.'
    }
    Write-Output 'PASS: PATH priority, JAVA_HOME fallback, and de-duplication'

    $env:PATH = ''
    $found = [JavaLocatorProbe]::Find()
    if ($found[0] -ne $homeJava) { throw 'JAVA_HOME-only discovery failed.' }
    Write-Output 'PASS: JAVA_HOME-only discovery'

    $env:ETAX_JAVA_LOCATOR_TEST_ROOT = $testRoot
    $env:JAVA_HOME = $null
    $env:PATH = 'bad"path;%ETAX_JAVA_LOCATOR_TEST_ROOT%\Other Vendor\runtime;'
    $found = [JavaLocatorProbe]::Find()
    if ($found[0] -ne $first) { throw 'Invalid PATH entry or variable expansion failed.' }
    Write-Output 'PASS: malformed entries skipped and environment variables expanded'

    [IO.File]::Delete($first)
    $found = [JavaLocatorProbe]::Find()
    if ($found -contains $first) { throw 'Missing java.exe was returned as a candidate.' }
    Write-Output 'PASS: missing java.exe excluded'
}
finally {
    $env:PATH = $oldPath
    $env:JAVA_HOME = $oldJavaHome
    $env:ETAX_JAVA_LOCATOR_TEST_ROOT = $oldProbeRoot
    # Only remove this run's GUID directory under the system temporary directory.
    $resolvedRoot = [IO.Path]::GetFullPath($testRoot)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if ($resolvedRoot.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($resolvedRoot).StartsWith('EtaxJavaLocator-') -and
        (Test-Path -LiteralPath $resolvedRoot)) {
        Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
    }
}
