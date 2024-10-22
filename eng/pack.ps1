#!/usr/bin/env pwsh
# SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
# SPDX-License-Identifier: MIT

$PublishTargetFramework = 'net8.0'
$PathToProjectToGetVersion = $([System.IO.Path]::Combine(${PSScriptRoot}, '../src/Smdn.HatenaBlogTools.Cli.Login/Smdn.HatenaBlogTools.Cli.Login.csproj'))

# get CLI version
dotnet build --framework net8.0 $PathToProjectToGetVersion
$InformationalVersion = dotnet run --no-build --framework net8.0 --project $PathToProjectToGetVersion -- --version
$InformationalVersion = $InformationalVersion -replace '\(.+\)', ''
$Version = New-Object -TypeName System.Version -ArgumentList $InformationalVersion

# generate a temporary solution file for build CLI assemblies
$CliSolutionName = 'Smdn.HatenaBlogTools.Cli'
$PathToCliSolutionDirectory = $PSScriptRoot
$PathToCliSolutionFile = $([System.IO.Path]::Combine($PathToCliSolutionDirectory, $CliSolutionName + '.sln'))

dotnet new sln --name $CliSolutionName --output $PathToCliSolutionDirectory
dotnet sln $PathToCliSolutionFile add $([System.IO.Path]::Combine(${PSScriptRoot}, '../src/Smdn.HatenaBlogTools.Cli.*/Smdn.HatenaBlogTools.Cli.*.csproj'))

# determine package name and output directory
$PackageName = "HatenaBlogTools-${Version}"
$PathToPublishOutputDirectory = $([System.IO.Path]::Combine(${PSScriptRoot}, $PackageName))

# build and publish CLI executables
dotnet clean --configuration Release $PathToCliSolutionFile
dotnet publish --configuration Release --framework $PublishTargetFramework --no-self-contained --output $PathToPublishOutputDirectory $PathToCliSolutionFile

# create ZIP archive
$PathToArchive = $([System.IO.Path]::Combine(${PSScriptRoot}, $PackageName + ".zip"))

Remove-Item -Path $PathToArchive # remove existing file before creating archive
Compress-Archive -CompressionLevel Optimal -Path ${PathToPublishOutputDirectory} -DestinationPath $PathToArchive

# delete the temporary output directory
Remove-Item -Recurse -Path $PathToPublishOutputDirectory

# delete the temporary solution file
Remove-Item -Path $PathToCliSolutionFile
