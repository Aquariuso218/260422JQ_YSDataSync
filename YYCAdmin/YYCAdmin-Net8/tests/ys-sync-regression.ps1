$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$failures = New-Object System.Collections.Generic.List[string]

$assemblyPaths = @(
    (Join-Path $root 'ZR.Model\bin\Debug\net8.0\ZR.Model.dll'),
    (Join-Path $root 'ZR.Service\bin\Debug\net8.0\ZR.Service.dll'),
    (Join-Path $root 'ZR.Tasks\bin\Debug\net8.0\ZR.Tasks.dll')
)

foreach ($assemblyPath in $assemblyPaths) {
    if (-not (Test-Path $assemblyPath)) {
        $failures.Add("Missing assembly: $assemblyPath")
    }
}

function Assert-SourceContains {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,
        [Parameter(Mandatory = $true)]
        [string]$Pattern,
        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    $fullPath = Join-Path $root $RelativePath
    if (-not (Test-Path $fullPath)) {
        $failures.Add("Missing source file: $fullPath")
        return
    }

    if (-not (Select-String -Path $fullPath -Pattern $Pattern -SimpleMatch -Quiet)) {
        $failures.Add("Missing $Description in $fullPath")
    }
}

Assert-SourceContains 'ZR.Tasks\TaskScheduler\Job_YsBillSync.cs' 'class Job_YsBillSync' 'job class'
Assert-SourceContains 'ZR.Service\Business\YsBillSyncService.cs' 'class YsBillSyncService' 'sync service class'
Assert-SourceContains 'ZR.Service\Business\IService\IYsBillSyncService.cs' 'Task<string> SyncAsync(' 'sync service contract'
Assert-SourceContains 'ZR.Model\Business\Model\EF_MidYSBillData.cs' 'class EF_MidYSBillData' 'mid table entity'
Assert-SourceContains 'ZR.Model\Business\Model\EF_sysSyncLog.cs' 'class EF_sysSyncLog' 'sync log entity'
Assert-SourceContains 'ZR.Admin.WebApi\Program.cs' 'AddHttpClient();' 'HttpClient registration'

if ($failures.Count -gt 0) {
    Write-Host 'YS sync regression harness failed:'
    foreach ($failure in $failures) {
        Write-Host "- $failure"
    }
    exit 1
}

Write-Host 'YS sync regression harness passed.'
