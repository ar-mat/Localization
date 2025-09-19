# Run in release mode
param($Configuration = "Release")

# define array of project names
$Projects = @("Localization.Core", "Localization.Wpf")

#iterate by project names
foreach ($ProjectName in $Projects) {

	# Get the original directoty, it will get back there after it's done
	$OriginalDir=Get-Location | select -ExpandProperty Path

	# Go to the project directory
	cd ../Projects/$ProjectName

	# Tagret path of published artifacts
	$BuildPath = "../../bin/$Configuration"
	$TargetPath = "$BuildPath/pack/$ProjectName"

	#Build the project
	dotnet build $ProjectName.csproj -c $Configuration -o $BuildPath

	# Pack nuget artifacts
	dotnet pack $ProjectName.csproj -c $Configuration --no-build -o $TargetPath /p:OutputPath=$BuildPath

	# Go back to the original directory
	cd $OriginalDir

}