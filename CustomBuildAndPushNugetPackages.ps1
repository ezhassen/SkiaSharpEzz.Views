# projectNames and dll names without extinsons
$projectNamesAndDllNames = @{
    "SkiaSharpEzz.Views.WindowsForms"   = "SkiaSharpEzz.Views.WindowsForms"
    "SkiaSharpEzz.Views.WindowsFormsV2" = "SkiaSharpEzz.Views.WindowsForms"
    "SkiaSharpEzz.Views.WPF"            = "SkiaSharpEzz.Views.WPF"
    "SkiaSharpEzz.Views.WPFv2"          = "SkiaSharpEzz.Views.WPF"
    "SkiaSharpEzz.Views.WPFGL"          = "SkiaSharpEzz.Views.WPFGL"
    "SkiaSharpEzz.Views.WPFGLv2"          = "SkiaSharpEzz.Views.WPFGL"
}



$MainTargetFramWork = "net9.0-windows"

function Get-FullProjectPath {
    param (
        [string]$projectName
    )

    # Combine script root with the project folder and file name
    $projectFolder = Join-Path -Path $PSScriptRoot -ChildPath $projectName
    $fullPath = Join-Path -Path $projectFolder -ChildPath "$projectName.csproj"

    return $fullPath
}

function Get-FullDllFilePath {
    param (
        [string]$projectName, [string]$dllName
    )

    # Combine script root with the project folder and file name
    $projectFolder = Join-Path -Path $PSScriptRoot -ChildPath $projectName
    $projectFolder = Join-Path -Path $projectFolder -ChildPath "bin"
    $projectFolder = Join-Path -Path $projectFolder -ChildPath "Release"
    $projectFolder = Join-Path -Path $projectFolder -ChildPath $MainTargetFramWork
    
    $fullPath = Join-Path -Path $projectFolder -ChildPath "$dllName.dll"

    return $fullPath
}


function Get-SemanticVersion {
    param (
        [string]$filePath
    )

    if (-not (Test-Path $filePath)) {
        Write-Warning "File not found: $filePath"
        Exit $LASTEXITCODE
        return $null
    }

    try {
        $info = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($filePath)
        $semVer = ($info.ProductVersion -split '\+')[0]  # Strips off +metadata

        if ($semVer -match '^\d+\.\d+\.\d+(-[A-Za-z0-9]+)?$') {
            Write-Host "SemVer: $semVer"
            return $semVer
        }
        else {
            Write-Warning "Version format not SemVer: $($info.ProductVersion)"
            Exit $LASTEXITCODE
            return $null
        }

    }
    catch {
        Write-Warning "Error reading version from $filePath : $_"
        Exit $LASTEXITCODE
    }
    # try {
    #     $assembly = [Reflection.Assembly]::LoadFrom($filePath)
    #     $infoAttr = $assembly.GetCustomAttributes([System.Reflection.AssemblyInformationalVersionAttribute], $false)

    #     if ($infoAttr.Length -gt 0) {
    #         $version = $infoAttr[0].InformationalVersion
    #     } else {
    #         $version = $assembly.GetName().Version.ToString()
    #     }

    #     # Strip build metadata (anything after +)
    #     $coreVersion = ($version -split '\+')[0]

    #     # Validate if it's SemVer core or pre-release: e.g., 3.4.4 or 3.4.4-beta
    #     if ($coreVersion -match '^\d+\.\d+\.\d+(-[A-Za-z0-9]+)?$') {
    #         return $coreVersion
    #     } else {
    #         Write-Warning "Version not in SemVer format: $version"
    #         Exit $LASTEXITCODE
    #         return $null
    #     }
    # }
    # catch {
    #     Write-Warning "Error reading version from $filePath : $_"
    #     Exit $LASTEXITCODE
    #     return $null
    # }
}

function Get-FullNugetPath {
    param (
        [string]$projectName, 
        [string]$dllName
    )
    $dllFile = Get-FullDllFilePath -projectName $projectName -dllName $dllName
    $dVerison = Get-SemanticVersion -filePath $dllFile

    # Combine script root with the project folder and file name
    $projectFolder = Join-Path -Path $PSScriptRoot -ChildPath $projectName
    $projectFolder = Join-Path -Path $projectFolder -ChildPath "bin"
    $projectFolder = Join-Path -Path $projectFolder -ChildPath "Release"
    
    $fullPath = Join-Path -Path $projectFolder -ChildPath "$projectName.$dVerison.nupkg"

    return $fullPath
}

function Exit-OnError {
    param (
        [int]$exitCode = 1
    )

    if (-not $?) {
        Write-Host "❌ Last command failed. Exiting script with code $exitCode..."
        exit $exitCode
    }
}

# Exit-OnError -exitCode $LASTEXITCODE

function Pack-Projects {
    
    Exit-OnError -exitCode $LASTEXITCODE
    foreach ($project in $projectNamesAndDllNames.Keys) {
        Exit-OnError -exitCode $LASTEXITCODE
        $projectPath = Get-FullProjectPath -projectName $project
        Write-Host "Full path for $projectPath : $path"
        try {
            dotnet pack "$projectPath" -c "Release"
        }
        catch {
            exit $LASTEXITCODE;
        }
    }
}


function Push-Nuget-Packages {
    
    foreach ($project in $projectNamesAndDllNames.Keys) {
        $nPackagePath = Get-FullNugetPath -projectName $project -dllName $projectNamesAndDllNames[$project]
        Write-Host "Full path for $nPackagePath : $path"
    
        Exit-OnError -exitCode $LASTEXITCODE
        try {
            nuget push "$nPackagePath" -Source "https://nuget.pkg.github.com/ezhassen/index.json" -SkipDuplicate
        }
        catch {
            exit $LASTEXITCODE;
        }
        Exit-OnError -exitCode $LASTEXITCODE
        try {
            nuget add "$nPackagePath" -Source "D:\Programming\NugetLocalFeed\"
        }
        catch {
            exit $LASTEXITCODE;
        }
        Write-Host "Done:"
    }
}

Pack-Projects
Exit-OnError -exitCode $LASTEXITCODE
Push-Nuget-Packages

exit $LASTEXITCODE;