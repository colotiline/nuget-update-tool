using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using nut.Entities;
using Serilog;

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
                    .Console()
                    .CreateLogger();
            }

            CommandLineApplication CreateCommandLineApplication()
            {
                var app = new CommandLineApplication();
            
                app.HelpOption("-h|--help");

                var directoryOption = app.Option
                (
                    "-d|--directory <DIRECTORY>",
                    "Directory update path.",
                    CommandOptionType.SingleValue
                );
                
                app.OnExecuteAsync
                (
                    async cancellationToken => 
                    {
                        var isValid = ValidateArgs
                        (
                            directoryOption
                        );

                        if (!isValid)
                        {
                            return;
                        }

                        var cSharpProjectsPaths = GetCsharpProjectsPaths
                        (
                            directoryOption.Value()
                        );

                        if (!cSharpProjectsPaths.Any())
                        {
                            logger.Warning("No '.csproj' files.");
                            return;
                        }

                        logger.Information
                        (
                            "Found {cSharpProjectsPathsLength} .csproj.",
                            cSharpProjectsPaths.Length
                        );

                        foreach (var cSharpProjectPath in cSharpProjectsPaths)
                        {
                            logger.Information
                            (
                                "Update '{cSharpProjectPath}.'",
                                cSharpProjectPath
                            );

                            var csprojContent = await File.ReadAllTextAsync
                            (
                                cSharpProjectPath
                            );

                            var packages = PackagesParser.Parse(csprojContent);

                            logger.Information
                            (
                                "Got {packagesLength} package(s).",
                                packages.Length
                            );

                            try
                            {
                                foreach (var package in packages)
                                {
                                    logger.Information
                                    (
                                        "Update package '{package}'.",
                                        package
                                    );

                                    await RunDotnetAddPackageAsync
                                    (
                                        package,
                                        Path.GetDirectoryName(cSharpProjectPath)
                                    );
                                }
                            } 
                            catch(Exception exception)
                            {
                                logger.Fatal(exception, "Can't execute.");
                                break;
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

                            if (!process.HasExited)
                            {
                                process.Kill();

                                logger.Error
                                (
                                    "Process killed with code '{code}'.",
                                    process.ExitCode
                                );
                            }
                        }
                    }
                );

                return app;

                bool ValidateArgs
                (
                    CommandOption directoryOption
                )
                {
                    if (!directoryOption.HasValue())
                    {
                        logger.Fatal("Directory update path can't be empty.");

                        return false;
                    }

                    if (!Directory.Exists(directoryOption.Value()))
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
