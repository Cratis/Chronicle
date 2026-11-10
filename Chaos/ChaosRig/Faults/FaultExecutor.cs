// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Applies scheduled faults and restores the dedicated workload environment.
/// </summary>
/// <param name="environment">The fault-target containers.</param>
/// <param name="producer">The producer process.</param>
/// <param name="consumer">The consumer process.</param>
internal sealed class FaultExecutor(DockerEnvironment environment, ChildWorker producer, ChildWorker consumer)
{
    readonly HashSet<ChildWorker> _killed = [];
    readonly HashSet<string> _paused = [];

    /// <summary>
    /// Applies the fault schedule over the workload duration.
    /// </summary>
    /// <param name="schedule">The ordered fault schedule.</param>
    /// <param name="duration">The workload duration.</param>
    /// <param name="cancellation">The run cancellation token.</param>
    /// <returns>The schedule execution task.</returns>
    public async Task Run(IReadOnlyList<ScheduledFault> schedule, TimeSpan duration, CancellationToken cancellation)
    {
        var clock = Stopwatch.StartNew();
        foreach (var fault in schedule)
        {
            await DelayUntil(fault.At, clock, cancellation);
            EnsureWorkers();
            Console.WriteLine($"FAULT {fault.At.TotalSeconds:F1}s {fault.Kind} resume={fault.Resume}");
            await Execute(fault, cancellation);
        }
        await DelayUntil(duration, clock, cancellation);
        EnsureWorkers();
    }

    /// <summary>
    /// Resumes or restarts all fault targets before settling.
    /// </summary>
    /// <param name="cancellation">The recovery cancellation token.</param>
    /// <returns>The recovery task.</returns>
    public async Task Recover(CancellationToken cancellation)
    {
        foreach (var container in new[] { environment.Mongo, environment.Kernel })
        {
            if (_paused.Contains(container.Id))
            {
                await ContainerSignals.Unpause(container.Id, cancellation);
                _paused.Remove(container.Id);
            }
            await ContainerSignals.Restart(container.Id, cancellation);
        }
        await environment.WaitForKernel(cancellation);
        if (consumer.IsRunning) await consumer.Signal("CONT", cancellation);
        else await consumer.Start(cancellation);
        if (producer.IsRunning) await producer.Signal("CONT", cancellation);
        else await producer.Start(cancellation);
    }

    static bool IsKill(FaultKind kind) => kind is FaultKind.KillKernel or FaultKind.KillMongo or FaultKind.KillProducer or FaultKind.KillConsumer;

    static Task DelayUntil(TimeSpan at, Stopwatch clock, CancellationToken cancellation) => Task.Delay(at > clock.Elapsed ? at - clock.Elapsed : TimeSpan.Zero, cancellation);

    async Task Execute(ScheduledFault fault, CancellationToken cancellation)
    {
        if (fault.Kind is FaultKind.KillKernel or FaultKind.StopKernel or FaultKind.KillMongo or FaultKind.StopMongo)
        {
            await ContainerFault(fault, cancellation);
            return;
        }
        var worker = fault.Kind is FaultKind.KillProducer or FaultKind.StopProducer ? producer : consumer;
        if (!IsKill(fault.Kind))
        {
            await worker.Signal(fault.Resume ? "CONT" : "STOP", cancellation);
            return;
        }
        await WorkerKill(worker, fault.Resume, cancellation);
    }

    async Task ContainerFault(ScheduledFault fault, CancellationToken cancellation)
    {
        var id = fault.Kind is FaultKind.KillKernel or FaultKind.StopKernel ? environment.Kernel.Id : environment.Mongo.Id;
        if (!IsKill(fault.Kind))
        {
            await ContainerPause(id, fault.Resume, cancellation);
            return;
        }
        if (fault.Resume) await ContainerSignals.Restart(id, cancellation);
        else await ContainerSignals.Send(id, "KILL", cancellation);
    }

    async Task ContainerPause(string id, bool resume, CancellationToken cancellation)
    {
        // Docker pause freezes the whole cgroup, including Chronicle behind the image's tini PID 1.
        if (resume)
        {
            await ContainerSignals.Unpause(id, cancellation);
            _paused.Remove(id);
            return;
        }
        await ContainerSignals.Pause(id, cancellation);
        _paused.Add(id);
    }

    async Task WorkerKill(ChildWorker worker, bool resume, CancellationToken cancellation)
    {
        if (resume)
        {
            await worker.Start(cancellation);
            _killed.Remove(worker);
            return;
        }
        _killed.Add(worker);
        await worker.Kill();
    }

    void EnsureWorkers()
    {
        foreach (var worker in new[] { producer, consumer })
        {
            if (!worker.IsRunning && !_killed.Contains(worker)) throw new InvalidOperationException("A worker exited outside its scheduled kill; inspect child logs.");
        }
    }
}
