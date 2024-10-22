#!/usr/bin/env pwsh
# SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
# SPDX-License-Identifier: MIT

$PublishTargetFramework = 'net8.0'
$PathToProjectToGetVersion = $([System.IO.Path]::Join(${PSScriptRoot}, '..', 'src', 'Smdn.HatenaBlogTools.Cli.Login', 'Smdn.HatenaBlogTools.Cli.Login.csproj'))

# get CLI version
dotnet build --framework net8.0 $PathToProjectToGetVersion
$InformationalVersion = dotnet run --no-build --framework net8.0 --project $PathToProjectToGetVersion -- --version
$InformationalVersion = $InformationalVersion -replace '\(.+\)', ''
$InformationalVersion = $InformationalVersion -replace '\+[0-9a-z]+', ''
$Version = New-Object -TypeName System.Version -ArgumentList $InformationalVersion

# generate a temporary solution file for build CLI assemblies
$CliSolutionName = 'Smdn.HatenaBlogTools.Cli'
$PathToCliSolutionDirectory = $PSScriptRoot
$PathToCliSolutionFile = $([System.IO.Path]::Join($PathToCliSolutionDirectory, $CliSolutionName + '.sln'))

dotnet new sln --name $CliSolutionName --output $PathToCliSolutionDirectory --force

$ProjectFilesToPublish = Get-ChildItem -Path $([System.IO.Path]::Join(${PSScriptRoot}, '..', 'src', 'Smdn.HatenaBlogTools.Cli.*', '*')) -Filter '*.csproj'

foreach ($ProjectFile in $ProjectFilesToPublish) {
  dotnet sln $PathToCliSolutionFile add $ProjectFile
}

# determine package name and output directory
$PackageName = "HatenaBlogTools-$($Version.Major).$($Version.Minor)"
$PathToPublishOutputDirectory = $([System.IO.Path]::Combine(${PSScriptRoot}, $PackageName))

# build and publish CLI executables
dotnet clean --configuration Release $PathToCliSolutionFile
dotnet publish --configuration Release --framework $PublishTargetFramework --no-self-contained --output $PathToPublishOutputDirectory $PathToCliSolutionFile

# create ZIP archive
$PathToArchive = $([System.IO.Path]::Combine(${PSScriptRoot}, $PackageName + ".zip"))

if (Test-Path $PathToArchive) {
  # remove existing file before creating archive
  Remove-Item -Path $PathToArchive
}
Compress-Archive -CompressionLevel Optimal -Path ${PathToPublishOutputDirectory} -DestinationPath $PathToArchive

# delete the temporary output directory
Remove-Item -Recurse -Path $PathToPublishOutputDirectory

# delete the temporary solution file
Remove-Item -Path $PathToCliSolutionFile
