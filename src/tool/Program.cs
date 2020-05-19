using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using nut.Entities;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace nut
{
    class Program
    {
        static int Main(string[] args)
        {
            var logger = CreateLogger();

            var app = CreateCommandLineApplication();

            return app.Execute(args);

            ILogger CreateLogger()
            {
                return new LoggerConfiguration()
                    .WriteTo
                    .Console
                    (
                        theme: CreateConsoleTheme()
                    )
                    .CreateLogger();

                SystemConsoleTheme CreateConsoleTheme()
                {
                    return new SystemConsoleTheme
                    (
                        new Dictionary
                        <
                            ConsoleThemeStyle, 
                            SystemConsoleThemeStyle
                        >
                        {
                            [ConsoleThemeStyle.LevelWarning] = 
                                new SystemConsoleThemeStyle 
                                { 
                                    Foreground = ConsoleColor.White, 
                                    Background = ConsoleColor.Yellow 
                                },
                            
                            [ConsoleThemeStyle.LevelError] = 
                                new SystemConsoleThemeStyle 
                                {
                                    Foreground = ConsoleColor.White, 
                                    Background = ConsoleColor.Red 
                                },
                            
                            [ConsoleThemeStyle.LevelFatal] = 
                                new SystemConsoleThemeStyle 
                                { 
                                    Foreground = ConsoleColor.White, 
                                    Background = ConsoleColor.Red 
                                }
                        }
                    );
                }
            }

            CommandLineApplication CreateCommandLineApplication()
            {
                var app = new CommandLineApplication();
            
                app.HelpOption("-h|--help");
                
                var directoryArgument = 
                    app.Argument("directory", "Directory update path.");

                app.OnExecuteAsync
                (
                    async cancellationToken => 
                    {
                        var isValid = ValidateArgs
                        (
                            directoryArgument
                        );

                        if (!isValid)
                        {
                            return;
                        }

                        var cSharpProjectsPaths = GetCsharpProjectsPaths
                        (
                            directoryArgument.Value
                        );

                        logger.Information
                        (
                            "Found {cSharpProjectsPathsLength} " +
                            ".csproj file(s).",
                            cSharpProjectsPaths.Length
                        );

                        if (!cSharpProjectsPaths.Any())
                        {
                            return;
                        }

                        foreach (var cSharpProjectPath in cSharpProjectsPaths)
                        {
                            logger.Information
                            (
                                "Updating '{cSharpProjectPath}.'",
                                cSharpProjectPath
                            );

                            var csprojContent = await File.ReadAllTextAsync
                            (
                                cSharpProjectPath
                            );

                            var packages = PackagesParser.Parse(csprojContent);

                            logger.Information
                            (
                                "Found {packagesLength} package(s).",
                                packages.Length
                            );

                            foreach (var package in packages)
                            {
                                try
                                {
                                    logger.Information
                                    (
                                        "Updating package {package}.",
                                        package
                                    );

                                    await RunDotnetAddPackageAsync
                                    (
                                        package,
                                        Path.GetDirectoryName(cSharpProjectPath)
                                    );
                                }
                                catch(Exception exception)
                                {
                                    logger.Fatal(exception, "Exception.");
                                }
                            } 
                        }

                        async Task RunDotnetAddPackageAsync
                        (
                            string package, 
                            string cSharpProjectDirectory
                        )
                        {
                            var processStartInfo = new ProcessStartInfo
                            (
                                "dotnet",
                                $"add package {package}"
                            );

                            processStartInfo.RedirectStandardError = true;
                            processStartInfo.RedirectStandardOutput = true;

                            
                            processStartInfo.WorkingDirectory = 
                                cSharpProjectDirectory;

                            var process = Process.Start(processStartInfo);

                            if (process == null)
                            {
                                throw new InvalidProgramException
                                (
                                    "dotnet doesn't exist."
                                );
                            }

                            using var processOutputReader = 
                                process.StandardOutput;

                            while (!processOutputReader.EndOfStream)
                            {
                                logger.Information
                                (
                                    await processOutputReader.ReadLineAsync()
                                );
                            }

                            using var processErrorReader = 
                                process.StandardError;

                            while (!processErrorReader.EndOfStream)
                            {
                                logger.Error
                                (
                                    await processErrorReader.ReadLineAsync()
                                );
                            }
                        }
                    }
                );

                return app;

                bool ValidateArgs
                (
                    CommandArgument directoryArgument
                )
                {
                    if (string.IsNullOrEmpty(directoryArgument.Value))
                    {
                        logger.Fatal("Directory update path can't be empty.");

                        return false;
                    }

                    if (!Directory.Exists(directoryArgument.Value))
                    {
                        logger.Fatal("Directory update path doesn't exist.");

                        return false;
                    }

                    return true;
                }

                string[] GetCsharpProjectsPaths(string directory)
                {
                    var projects = new List<string>();

                    FillProjects(projects, directory);

                    return projects.ToArray();

                    void FillProjects
                    (
                        List<string> projects, 
                        string directory
                    )
                    {
                        var cSharpProjects = Directory
                            .GetFiles(directory)
                            .Where(_ => _.ToUpper().EndsWith(".CSPROJ"));

                        projects.AddRange(cSharpProjects);

                        var subdirectories = 
                            Directory.GetDirectories(directory);

                        foreach (var subdirectory in subdirectories)
                        {
                            FillProjects(projects, subdirectory);
                        }
                    }
                }
            }
        }
    }
}
