// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Builds scratch images using the unchanged production Dockerfile and copy script.</summary>
internal static class ProfileImages
{
    /// <summary>Publishes selected variants for the Docker engine architecture.</summary>
    /// <param name="options">The build options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The build task.</returns>
    /// <exception cref="HarnessFailure">The Docker architecture is unsupported.</exception>
    public static async Task Build(ProfileOptions options, CancellationToken cancellationToken)
    {
        var architecture = await DockerCli.Require(MetricCommands.Architecture(), cancellationToken);
        var arch = architecture switch
        {
            "arm64" => "arm64",
            "amd64" => "x64",
            _ => throw new HarnessFailure("Docker architecture must be arm64 or amd64.")
        };
        var sha = await ProfileRepository.Sha(cancellationToken);
        var context = new ImageBuildContext(options.Results, arch, sha, Guid.NewGuid().ToString("N"));
        foreach (var variant in new[] { ImageVariant.R2R, ImageVariant.Jit }.Where(variant => options.Variants.Contains(variant.Name)))
        {
            await BuildVariant(context, variant, cancellationToken);
        }
    }

    /// <summary>Verifies ReadyToRun headers in an image without starting it.</summary>
    /// <param name="options">The image and destination options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Zero only if every Chronicle assembly is ReadyToRun.</returns>
    public static async Task<int> Check(ProfileOptions options, CancellationToken cancellationToken)
    {
        var directory = Path.GetFullPath(Path.Combine(options.Results, $"r2r-check-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(directory);
        var name = $"chronicle-cp-check-{Guid.NewGuid():N}";
        try
        {
            await DockerCli.Require(ImageCommands.Create(options.Image, name, "check"), cancellationToken);
            await DockerCli.Require(ImageCommands.Copy(name, directory), cancellationToken);
            var headers = ManagedNativeHeader.Read(directory);
            foreach (var header in headers)
            {
                Console.WriteLine($"{header.Assembly}\tReadyToRun={header.ReadyToRun}");
            }
            return ManagedNativeHeader.Matches(headers, true) ? 0 : 1;
        }
        finally
        {
            await DockerCli.Run(CleanupCommands.Remove(name), CancellationToken.None);
        }
    }

    static async Task BuildVariant(ImageBuildContext context, ImageVariant variant, CancellationToken cancellationToken)
    {
        var directory = Path.GetFullPath(Path.Combine(context.Results, "images", variant.Name));
        var publish = Path.Combine(directory, "Source", "Kernel", "Server", "out", context.Architecture);
        Directory.CreateDirectory(publish);
        var root = ProfileRepository.Root();
        var project = Path.Combine(root, "Source", "Kernel", "Server", "Server.csproj");
        var arguments = new[] { "publish", project, "-c", "Release", "-f", "net11.0", "-r", $"linux-{context.Architecture}", "--self-contained", "-m:2", "-nodeReuse:false", "-o", publish, $"-p:PublishReadyToRun={variant.ReadyToRun.ToString().ToLowerInvariant()}", "-p:DisableProxyGenerator=true" };
        (await ProcessRunner.Run("dotnet", arguments, TimeSpan.FromSeconds(1800), cancellationToken)).RequireSuccess("publish kernel");
        var headers = ManagedNativeHeader.Read(publish);
        if (!ManagedNativeHeader.Matches(headers, variant.ReadyToRun))
        {
            throw new HarnessFailure($"{variant.Name}: Chronicle ManagedNativeHeader check failed.");
        }
        Directory.CreateDirectory(Path.Combine(directory, "Docker"));
        File.Copy(Path.Combine(root, "Docker", "copy-server-files.sh"), Path.Combine(directory, "Docker", "copy-server-files.sh"), true);
        Directory.CreateDirectory(Path.Combine(directory, "Source", "Workbench", "wwwroot"));
        await DockerCli.Require(ImageCommands.Build(directory, variant.Reference(context.Sha), context.Session), cancellationToken);
        var identity = await DockerCli.Require(MetricCommands.Image(variant.Reference(context.Sha)), cancellationToken);
        await ResultFiles.Write(Path.Combine(directory, "metadata.json"), new { context.Sha, Runtime = $"linux-{context.Architecture}", Variant = variant, Image = identity, Headers = headers, WorkbenchAssets = false }, cancellationToken);
    }
}
