// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.Slices;

/// <summary>
/// Defers read-model inputs until a single run using the existing scenario engine.
/// </summary>
/// <typeparam name="TReadModel">The read model type.</typeparam>
public sealed class ProjectionSlice<TReadModel> : IDisposable
    where TReadModel : class
{
    readonly List<(EventSourceId Source, object[] Events)> _batches = [];
    readonly ProjectionSliceOutcome<TReadModel> _outcome;
    bool _started;
    bool _completed;
    bool _disposed;

    /// <summary>
    /// Initializes a slice owning a scenario, optionally using per-run defaults.
    /// </summary>
    /// <param name="defaults">The artifact and service defaults, or the usual scenario defaults when omitted.</param>
    public ProjectionSlice(Defaults? defaults = null)
    {
        Scenario = defaults is null ? new() : new(null, defaults);
        _outcome = new(Scenario);
    }

    /// <summary>
    /// Gets the live scenario for configuration before running and advanced assertions afterward.
    /// </summary>
    public ReadModelScenario<TReadModel> Scenario { get; }

    /// <summary>
    /// Gets the outcome only after successful completion; this never triggers execution.
    /// </summary>
    /// <exception cref="SliceNotRun">Execution has not completed successfully.</exception>
    /// <exception cref="SliceDisposed">The slice has been disposed.</exception>
    public ProjectionSliceOutcome<TReadModel> Then
    {
        get
        {
            EnsureActive();
            if (!_completed) throw new SliceNotRun();

            return _outcome;
        }
    }

    /// <summary>
    /// Copies a batch of event references for later execution, preserving declaration order.
    /// </summary>
    /// <param name="eventSourceId">The event source.</param>
    /// <param name="events">The ordered events; event objects themselves are not cloned.</param>
    /// <returns>This slice.</returns>
    /// <exception cref="SliceAlreadyRun">Execution has started.</exception>
    /// <exception cref="SliceDisposed">The slice has been disposed.</exception>
    public ProjectionSlice<TReadModel> GivenEvents(EventSourceId eventSourceId, params object[] events)
    {
        EnsureNotStarted();
        _batches.Add((eventSourceId, events.ToArray()));

        return this;
    }

    /// <summary>
    /// Collects all batches and forces materialization of all instances exactly once.
    /// </summary>
    /// <returns>The outcome after successful processing.</returns>
    /// <exception cref="SliceAlreadyRun">Execution has already started, including a previous failed run.</exception>
    /// <exception cref="SliceDisposed">The slice has been disposed.</exception>
    public Task<ProjectionSliceOutcome<TReadModel>> Run()
    {
        EnsureNotStarted();
        _started = true;
        foreach (var batch in _batches) Scenario.CollectEventsFor(batch.Source, batch.Events);
        _ = Scenario.Instances;
        _completed = true;

        return Task.FromResult(_outcome);
    }

    /// <summary>
    /// Disposes the owned scenario once without disposing caller-owned defaults or services.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Scenario.Dispose();
    }

    void EnsureActive()
    {
        if (_disposed) throw new SliceDisposed();
    }

    void EnsureNotStarted()
    {
        EnsureActive();
        if (_started) throw new SliceAlreadyRun();
    }
}
