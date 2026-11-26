// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Logs when each Orleans silo lifecycle stage began to stop, how long each took and which stage is still stopping when the
/// shared drain budget expires, so a stop that does not finish in time names its blocker.
/// </summary>
/// <remarks>
/// Orleans stops stages in descending order and the callbacks of one stage run concurrently, so a callback can only time itself.
/// The duration of a stage is therefore the time until the next lower stage begins, which is why a bracket is registered for every
/// <see cref="ServiceLifecycleStage"/> value, including the one where the grain catalog deactivates grains.
/// </remarks>
/// <param name="lifetime">The <see cref="IHostApplicationLifetime"/> whose stopping signal marks the start of the stop.</param>
/// <param name="budget">The shared <see cref="IShutdownDrainBudget"/> whose deadline names the stage still stopping.</param>
/// <param name="logger">The <see cref="ILogger{T}"/> for logging.</param>
/// <param name="timeProvider">The clock; defaults to the system clock.</param>
public sealed class ShutdownStageTimings(
    IHostApplicationLifetime lifetime,
    IShutdownDrainBudget budget,
    ILogger<ShutdownStageTimings> logger,
    TimeProvider? timeProvider = null) : ILifecycleParticipant<ISiloLifecycle>, IDisposable
{
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    readonly Lock _lock = new();
    CancellationTokenRegistration _stopping;
    CancellationTokenRegistration _deadline;
    long? _stopBegan;
    string? _currentStage;
    long _currentStageBegan;

    /// <summary>
    /// Gets the lifecycle stages that are bracketed: every <see cref="ServiceLifecycleStage"/> value.
    /// </summary>
    internal static IReadOnlyDictionary<string, int> Stages { get; } = typeof(ServiceLifecycleStage)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(_ => _.IsLiteral && _.FieldType == typeof(int))
        .ToDictionary(_ => _.Name, _ => (int)_.GetRawConstantValue()!);

    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle)
    {
        _stopping = lifetime.ApplicationStopping.Register(MarkStopBegan);
        _deadline = budget.Token.Register(ReportStageStillStopping);
        foreach (var (name, stage) in Stages)
        {
            lifecycle.Subscribe($"{nameof(ShutdownStageTimings)}:{name}", stage, _ => Task.CompletedTask, _ => StageStopping(name));
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _stopping.Dispose();
        _deadline.Dispose();
    }

    /// <summary>
    /// Marks that a stage began stopping, which ends the previous stage.
    /// </summary>
    /// <param name="stage">The name of the stage.</param>
    /// <returns>A completed task.</returns>
    internal Task StageStopping(string stage)
    {
        lock (_lock)
        {
            var now = _timeProvider.GetTimestamp();
            _stopBegan ??= now;
            if (_currentStage is not null)
            {
                logger.StageStopped(_currentStage, _timeProvider.GetElapsedTime(_currentStageBegan, now));
            }

            _currentStage = stage;
            _currentStageBegan = now;
            logger.StageStopBegan(stage, _timeProvider.GetElapsedTime(_stopBegan.Value, now));
        }

        return Task.CompletedTask;
    }

    void MarkStopBegan()
    {
        lock (_lock)
        {
            _stopBegan ??= _timeProvider.GetTimestamp();
        }
    }

    void ReportStageStillStopping()
    {
        lock (_lock)
        {
            var now = _timeProvider.GetTimestamp();
            if (_currentStage is null)
            {
                logger.NoStageBeganStopping(budget.Timeout);
                return;
            }

            logger.StageStillStopping(_currentStage, _timeProvider.GetElapsedTime(_currentStageBegan, now), budget.Timeout);
        }
    }
}
