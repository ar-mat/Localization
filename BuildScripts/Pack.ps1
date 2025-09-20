# Run in release mode
param(
    [string]$Configuration = "Release",
    [string]$ProjectName = ""
)

# define array of project names (default set)
$Projects = @("Localization.Core", "Localization.Wpf")

# If a specific project name was passed, limit the list to just that project
if (![string]::IsNullOrWhiteSpace($ProjectName)) {
    $Projects = @($ProjectName)
}

#iterate by project names
foreach ($Proj in $Projects) {

	# Get the original directory, it will get back there after it's done
	$OriginalDir = Get-Location | Select-Object -ExpandProperty Path

	# Go to the project directory
	cd ../Projects/$Proj

	# Target path of published artifacts
	$BuildPath = "../../bin/$Configuration"
	$TargetPath = "$BuildPath/pack/$Proj"

	#Build the project
	dotnet build "$Proj.csproj" -c $Configuration -o $BuildPath

	# Pack nuget artifacts
	dotnet pack "$Proj.csproj" -c $Configuration --no-build -o $TargetPath /p:OutputPath=$BuildPath

	# Go back to the original directory
	cd $OriginalDir
}