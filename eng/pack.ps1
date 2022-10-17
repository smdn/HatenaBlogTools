#!/usr/bin/env pwsh
# SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
# SPDX-License-Identifier: MIT

$PublishTargetFramework = 'netcoreapp3.1'
$PathToProjectToGetVersion = $([System.IO.Path]::Combine(${PSScriptRoot}, '../src/Smdn.HatenaBlogTools.Cli.Login/Smdn.HatenaBlogTools.Cli.Login.csproj'))

# get CLI version
dotnet build --framework net6.0 $PathToProjectToGetVersion
$InformationalVersion = dotnet run --no-build --framework net6.0 --project $PathToProjectToGetVersion -- --version
$InformationalVersion = $InformationalVersion -replace '\(.+\)', ''
$Version = New-Object -TypeName System.Version -ArgumentList $InformationalVersion

# determine package name and output directory
$PackageName = "HatenaBlogTools-${Version}"
$PathToPublishOutputDirectory = $([System.IO.Path]::Combine(${PSScriptRoot}, $PackageName))

# determine CLI solution file path
$PathToCliSolution = $([System.IO.Path]::Combine(${PSScriptRoot}, 'HatenaBlogTools-CLI.sln'))

# build and publish CLI executables
dotnet publish --configuration Release --framework $PublishTargetFramework --no-self-contained --output $PathToPublishOutputDirectory $PathToCliSolution

# create ZIP archive
$PathToArchive = $([System.IO.Path]::Combine(${PSScriptRoot}, $PackageName + ".zip"))

Compress-Archive -CompressionLevel Optimal -Path ${PathToPublishOutputDirectory} -DestinationPath $PathToArchive
