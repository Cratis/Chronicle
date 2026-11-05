// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Prepares fresh per-run containers and temporary TLS material.</summary>
internal static class ContainerLifecycle
{
    /// <summary>Creates private identities and random ephemeral credentials.</summary>
    /// <param name="session">The shared sweep settings.</param>
    /// <param name="run">The repetition identity.</param>
    /// <returns>The private run context.</returns>
    public static RunContext Context(ProfileSession session, RunIdentity run) =>
        new(session, run, Path.GetFullPath(Path.Combine(session.Results, run.Id)), new RunCredentials(Convert.ToHexString(RandomNumberGenerator.GetBytes(24)), Convert.ToHexString(RandomNumberGenerator.GetBytes(32))));

    /// <summary>Creates TLS material and a new writable replica set.</summary>
    /// <param name="context">The private run context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The startup task.</returns>
    public static async Task StartMongo(RunContext context, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(context.Directory);
        Directory.CreateDirectory(context.TraceDirectory);
        BenchmarkCertificateGenerator.GenerateAndSaveCertificate(context.Certificate, context.CertificatePassword);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(context.Certificate, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
            File.SetUnixFileMode(context.TraceDirectory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute);
        }
        await DockerCli.Require(ContainerCommands.Mongo(context), cancellationToken);
        await DockerCli.Require(ContainerCommands.Start(context.Mongo), cancellationToken);
        await WaitForMongoCommand(ContainerCommands.Initiate(context), cancellationToken);
        await WaitForMongoCommand(ContainerCommands.Primary(context), cancellationToken);
    }

    /// <summary>Measures Docker creation and warm-cache cold-kernel health readiness.</summary>
    /// <param name="context">The run context.</param>
    /// <param name="cell">The resource configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Startup timings and the client endpoint.</returns>
    /// <exception cref="HarnessFailure">The kernel does not become ready.</exception>
    public static async Task<(double CreateMs, double ReadyMs, string GrpcEndpoint)> StartKernel(RunContext context, MatrixCell cell, CancellationToken cancellationToken)
    {
        var create = await DockerCli.Run(ContainerCommands.Kernel(context, cell), cancellationToken);
        create.RequireSuccess("create kernel");
        await DockerCli.Require(ContainerCommands.Start(context.Kernel), cancellationToken);
        var watch = Stopwatch.StartNew();
        var health = await Endpoint(context, 8080, cancellationToken);
        var grpc = await Endpoint(context, 35000, cancellationToken);
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        while (watch.Elapsed < TimeSpan.FromSeconds(180))
        {
            if (await Healthy(http, $"http://{health}/health/ready", cancellationToken))
            {
                return (create.ElapsedMs, watch.Elapsed.TotalMilliseconds, grpc);
            }
            await Task.Delay(250, cancellationToken);
        }
        throw new HarnessFailure("notReady");
    }

    /// <summary>Removes the exact run containers and ephemeral certificate.</summary>
    /// <param name="context">The run context.</param>
    /// <returns>The cleanup task.</returns>
    public static async Task Finish(RunContext context)
    {
        await DockerCli.Run(CleanupCommands.Remove(context.Kernel, context.Mongo), CancellationToken.None);
        if (File.Exists(context.Certificate))
        {
            File.Delete(context.Certificate);
        }
    }

    static async Task<string> Endpoint(RunContext context, int port, CancellationToken cancellationToken) =>
        await DockerCli.Require(MetricCommands.Port(context, port), cancellationToken);

    static async Task<bool> Healthy(HttpClient http, string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.GetAsync(url, cancellationToken);
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    static async Task WaitForMongoCommand(DockerCommand command, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(90));
        while (!timeout.IsCancellationRequested)
        {
            var result = await DockerCli.Run(command, timeout.Token);
            if (result.ExitCode == 0)
            {
                return;
            }
            await Task.Delay(500, timeout.Token);
        }
        throw new HarnessFailure($"{command.Purpose}: deadline exceeded.");
    }
}
