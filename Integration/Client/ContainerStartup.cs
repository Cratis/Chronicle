// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.ExceptionServices;
using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Rebuilds containers that crash during startup, preserving evidence before disposal.
/// </summary>
/// <param name="build">Builds a fresh container for each attempt.</param>
/// <param name="inspect">Reads fresh exit state, returning null for a running container.</param>
/// <param name="readTail">Reads a bounded log tail.</param>
/// <param name="report">Reports retry evidence.</param>
/// <param name="evidenceDeadline">The shared deadline for collecting evidence from one failure.</param>
public sealed class ContainerStartup(
    Func<IContainer> build,
    Func<IContainer, CancellationToken, Task<ContainerExit?>> inspect,
    Func<IContainer, CancellationToken, Task<string>> readTail,
    Action<string> report,
    TimeSpan evidenceDeadline)
{
    /// <summary>
    /// The maximum number of starts, including the initial attempt.
    /// </summary>
    public const int MaxAttempts = 3;

    /// <summary>
    /// Creates a startup helper with Docker evidence and console reporting.
    /// </summary>
    /// <param name="build">Builds a fresh container for each attempt.</param>
    /// <returns>The configured startup helper.</returns>
    public static ContainerStartup ForDocker(Func<IContainer> build) =>
        new(build, DockerContainerEvidence.Inspect, (container, token) => DockerContainerEvidence.ReadTail(container, 64, token), Console.Out.WriteLine, TimeSpan.FromSeconds(10));

    /// <summary>
    /// Starts a fresh container, retrying only confirmed startup crashes.
    /// </summary>
    /// <param name="built">Notifies the owner as soon as each container is built.</param>
    /// <param name="retrying">Optionally records a retry after a crashed container is disposed.</param>
    /// <param name="ct">Cancels startup without rebuilding.</param>
    /// <returns>The successfully started container.</returns>
    /// <exception cref="ContainerStartupFailed">Every startup attempt crashed.</exception>
    public async Task<IContainer> Start(Action<IContainer> built, Action<ContainerStartupAttempt>? retrying, CancellationToken ct)
    {
        var attempts = new List<ContainerStartupAttempt>();
        for (var number = 1; ; number++)
        {
            ct.ThrowIfCancellationRequested();
            var container = build();
            built(container);
            try
            {
                await container.StartAsync(ct);
                return container;
            }
            catch (Exception failure) when (!ct.IsCancellationRequested)
            {
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
                deadline.CancelAfter(evidenceDeadline);
                ContainerExit? exit = null;
                var inspectUnavailable = false;
                var note = string.Empty;
                try
                {
                    exit = await inspect(container, deadline.Token).WaitAsync(deadline.Token);
                }
                catch (Exception evidenceFailure)
                {
                    inspectUnavailable = true;
                    note = $"<exit state unavailable: {evidenceFailure.GetType().Name}>{Environment.NewLine}";
                }

                string tail;
                try
                {
                    tail = await readTail(container, deadline.Token).WaitAsync(deadline.Token);
                }
                catch (Exception evidenceFailure)
                {
                    tail = $"<log tail unavailable: {evidenceFailure.GetType().Name}>";
                }

                ct.ThrowIfCancellationRequested();
                if (exit is null && !(inspectUnavailable && failure is ContainerNotRunningException))
                {
                    ExceptionDispatchInfo.Capture(failure).Throw();
                }

                var attempt = new ContainerStartupAttempt(number, MaxAttempts, container.Id, exit, note + tail, failure);
                try
                {
                    await container.DisposeAsync();
                }
                catch (Exception disposeFailure)
                {
                    attempt = attempt with { LogTail = $"{attempt.LogTail}{Environment.NewLine}<dispose unavailable: {disposeFailure.GetType().Name}>" };
                }

                ct.ThrowIfCancellationRequested();
                attempts.Add(attempt);

                if (number == MaxAttempts)
                {
                    throw new ContainerStartupFailed(container.Image.FullName, attempts);
                }

                report("::warning title=Container restarted::" + attempt.Describe());
                retrying?.Invoke(attempt);
            }
        }
    }
}
