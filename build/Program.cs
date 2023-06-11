using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Solution;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Compression;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            // .UseLifetime<BuildLifetime>()
            .Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public string DotNetBuildConfig { get; set; }
    public const string SlnFile = "../Flow.Launcher.Plugin.ClipboardR.sln";
    public Lazy<SolutionParserResult> DefaultSln { get; set; }
    public const string DeployFramework = "net7.0-windows";
    public string PublishDir = ".dist";
    public string PublishVersion = "0.2.4";

    public BuildContext(ICakeContext context)
        : base(context)
    {
        DefaultSln = new Lazy<SolutionParserResult>(() => context.ParseSolution(SlnFile));
        DotNetBuildConfig = context.Argument("configuration", "Release");
    }
}

public class BuildLifetime : FrostingLifetime<BuildContext>
{
    public override void Setup(BuildContext context, ISetupContext info)
    {
        var clean = new CleanTask();
        clean.Run(context);
    }

    public override void Teardown(BuildContext context, ITeardownContext info)
    {
        
    }
}

[TaskName("Build")]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var projects = context
            .DefaultSln.Value.Projects.Where(p => p.Name.EndsWith("ClipboardR"));
        var projectPath = projects.First().Path.FullPath;
        context.Information($"Building {projectPath}");
        context.DotNetBuild(
            projectPath,
            new DotNetBuildSettings
            {
                Configuration = context.DotNetBuildConfig,
                Verbosity = DotNetVerbosity.Minimal,
                Framework = BuildContext.DeployFramework,
                NoDependencies = false,
                NoIncremental = true,
            });
        
        
    }
}

[TaskName("Publish")]
[IsDependentOn(typeof(BuildTask))]
public class PublishTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var project = context
            .DefaultSln.Value.Projects
            .First(p => p.Name.EndsWith("ClipboardR"));
        var srcDir = project.Path.GetDirectory()
            .Combine(new DirectoryPath($"bin/publish"));
        var dstDir = $"{srcDir.GetParent().GetParent().GetParent().GetParent().FullPath}/{context.PublishDir}";
        context.DotNetPublish(
            project.Path.FullPath,
            new DotNetPublishSettings
            {
                OutputDirectory = srcDir,
                Configuration = context.DotNetBuildConfig,
                Framework = BuildContext.DeployFramework,
                Verbosity = DotNetVerbosity.Minimal,
            });
        context.CreateDirectory(dstDir);
        var files = context
            .GetFiles(@$"{srcDir}/**/(*(c|C)lipboard*.(png|json|dll)|*.png|plugin.json|(*simulator).dll)");
        foreach (var f in files)
            context.Information($"Adding: {f}");
        context.ZipCompress(
            rootPath: srcDir,
            outputPath: $"{dstDir}/ClipboardR-v{context.PublishVersion}.zip",
            filePaths: files,
            level: 9);
    }
}

[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        foreach (var project in context.DefaultSln.Value.Projects)
        {
            context.Information($"Cleaning {project.Path.GetDirectory().FullPath}...");
            context.CleanDirectory($"{project.Path.GetDirectory().FullPath}/bin/{context.DotNetBuildConfig}");
        }
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(CleanTask))]
[IsDependentOn(typeof(BuildTask))]
public class DefaultTask : FrostingTask
{
}