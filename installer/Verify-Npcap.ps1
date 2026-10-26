param([Parameter(Mandatory=$true)][string]$InstallerPath)
$ErrorActionPreference = 'Stop'
try {
    $signature = Get-AuthenticodeSignature -LiteralPath $InstallerPath
    if ($signature.Status -ne 'Valid' -or
        $signature.SignerCertificate.Subject -notmatch 'O="?Insecure\.Com LLC|O="?Nmap Software LLC') {
        throw 'Npcap signature is not trusted or the publisher is unexpected.'
    }
    $process = Start-Process -FilePath $InstallerPath -Verb RunAs -Wait -PassThru
    exit $process.ExitCode
} catch {
    Write-Error $_ -ErrorAction Continue
    exit 1
}
