# Run in release mode
param(
    [string]$Configuration = "Release",
    [string]$ProjectName = ""
)

# define array of project names (default full set)
$Projects = @("Localization.Core", "Localization.Wpf", "Localization.Designer")

# If a specific project name was passed, limit list to that one
if (![string]::IsNullOrWhiteSpace($ProjectName)) {
    $Projects = @($ProjectName)
}

# iterate by project names
foreach ($Proj in $Projects) {

    # Get the original directory, it will get back there after it's done
    $OriginalDir = Get-Location | Select-Object -ExpandProperty Path

    # Go to the project directory
    cd ../Projects/$Proj

    # Target path of published artifacts
    $BuildPath = "../../bin/$Configuration"
    $TargetPath = "$BuildPath/publish/$Proj"

    # Build the project
    dotnet build "$Proj.csproj" -c $Configuration -o $BuildPath

    # Get the build version (still reading core dll version as before)
    $Version = & "$OriginalDir/GetAssemblyVersion.ps1" -AssemblyPath $BuildPath/armat.localization.core.dll

    # Clean all contents in the Target path
    if (Test-Path $TargetPath) {
        Remove-Item $TargetPath -Recurse -Force
    }

    # Publish artifacts
    dotnet publish "$Proj.csproj" -c $Configuration --no-build -o $TargetPath /p:OutputPath=$BuildPath

    # Zip the contents
    Compress-Archive -Path $TargetPath -DestinationPath $TargetPath/../Armat.$Proj-$Version.zip -Force

    # Go back to the original directory
    cd $OriginalDir
}