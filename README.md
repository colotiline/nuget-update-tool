# .NET Core NuGet update tool

## Status

![.NET Core](https://github.com/colotiline/nuget-update-tool/workflows/.NET%20Core/badge.svg)

## Algorithm

- Going through all subdirectories in provided directory.
- Looking for .csproj files.
- Update all packages in them to the latest version.

## Get started

- Install NuGet tool https://www.nuget.org/packages/Colotiline.NuGet.UpdateTool/.
- Run `dotnet nu -d ./path/to/your/directory`.