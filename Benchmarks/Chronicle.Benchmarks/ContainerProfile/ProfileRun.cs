// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Owns one bounded repetition and persists failure as data.</summary>
internal static class ProfileRun
{
    /// <summary>Executes one fresh-kernel repetition and always attempts cleanup.</summary>
    /// <param name="context">The private run context.</param>
    /// <param name="cell">The resource configuration.</param>
    /// <param name="options">The workload options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result, including unavailable fields on failure.</returns>
    public static async Task<ProfileResult> Execute(RunContext context, MatrixCell cell, ProfileOptions options, CancellationToken cancellationToken)
    {
        var result = new ProfileResult
        {
            RunId = context.RunId,
            Cell = cell.Id,
            Sha = context.Sha,
            Repetition = context.Run.Repetition,
            ImageReference = cell.Image.Reference(context.Sha),
            GcEnvironment = cell.Gc.Environment.ToArray(),
            WorkloadSettings = options.Workload,
            TraceRepetition = context.Trace,
            AllowNoisyHost = options.AllowNoisyHost,
            MaxHostLoad = options.MaxHostLoad
        };
        var attemptedContainers = false;
        try
        {
            result.Host = await HostConditions.Capture(context, cell, options, cancellationToken);
            if (result.Host.NoisyHost && !options.AllowNoisyHost)
            {
                result.Status = "skipped-host-load";
                return result;
            }
            attemptedContainers = true;
            await ContainerLifecycle.StartMongo(context, cancellationToken);
            var startup = await ContainerLifecycle.StartKernel(context, cell, cancellationToken);
            result.DockerCreateMs = startup.CreateMs;
            result.ColdReadyMs = startup.ReadyMs;
            result.Memory.Add(await MemorySample.Capture(context, "ready", cancellationToken));
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            result.Idle = await MemorySample.Capture(context, "idle", cancellationToken);
            result.Memory.Add(result.Idle);
            await Measure(context, startup.GrpcEndpoint, result, cancellationToken);
            result.Status = "completed";
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Runtime/library errors can embed credentials. Persist only their type or our safe status.
            result.FailureKind = exception.GetType().Name;
            result.Status = FailureStatus(exception);
        }
        finally
        {
            try
            {
                if (attemptedContainers)
                {
                    await Stop(context, result);
                }
            }
            finally
            {
                try
                {
                    await ResultFiles.Write(Path.Combine(context.Directory, "result.json"), result, CancellationToken.None);
                }
                finally
                {
                    if (attemptedContainers)
                    {
                        await ContainerLifecycle.Finish(context);
                    }
                }
            }
        }
        return result;
    }

    static string FailureStatus(Exception exception)
    {
        if (exception is HarnessFailure)
        {
            return KnownFailureStatus(exception.Message);
        }
        return exception is OperationCanceledException ? "deadline-or-cancelled" : "failed";
    }

    static string KnownFailureStatus(string message) => message switch
    {
        "notReady" => message,
        "notCaughtUp" => message,
        _ => "failed"
    };

    static async Task Measure(RunContext context, string endpoint, ProfileResult result, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(15));
        using var sampler = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        var sampling = Sample(context, result, sampler.Token);
        try
        {
            async Task SamplePhase(string phase)
            {
                AddSample(result, await MemorySample.Capture(context, phase, timeout.Token));
            }
            var connection = new WorkloadConnection(endpoint, "container-profile", context.ClientSecret);
            result.Workload = await ProfileWorkload.Run(connection, result.WorkloadSettings, SamplePhase, timeout.Token);
        }
        finally
        {
            await sampler.CancelAsync();
            await sampling;
        }
    }

    static async Task Sample(RunContext context, ProfileResult result, CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                AddSample(result, await MemorySample.Capture(context, "active", cancellationToken));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal sampler shutdown.
        }
    }

    static void AddSample(ProfileResult result, MemorySample sample)
    {
        lock (result.Memory)
        {
            result.Memory.Add(sample);
        }
    }

    static async Task Stop(RunContext context, ProfileResult result)
    {
        result.StateBeforeStop = await KernelState.Read(context, CancellationToken.None);
        if (result.StateBeforeStop.Running == true)
        {
            var stop = await DockerCli.Run(MetricCommands.Stop(context), CancellationToken.None);
            result.StopMs = stop.ExitCode == 0 ? stop.ElapsedMs : null;
        }
        result.StateAfterStop = await KernelState.Read(context, CancellationToken.None);
        if (result.StateAfterStop.OomKilled == true)
        {
            result.Status = "oom";
        }
        else if (result.StateAfterStop.ExitCode == 137)
        {
            result.Status = "killed";
        }
        if (context.Trace && result.StateAfterStop is { Running: false, OomKilled: false, ExitCode: not 137 })
        {
            try
            {
                result.Gc = GcSummary.Read(Path.Combine(context.TraceDirectory, "kernel.nettrace"));
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                result.TraceFailureKind = exception.GetType().Name;
            }
        }
    }
}
