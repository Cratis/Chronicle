// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors;

namespace Cratis.Chronicle.Testing.Slices;

/// <summary>
/// Defers reactor inputs until a single awaited run through the existing scenario invoker.
/// </summary>
/// <typeparam name="TReactor">The reactor type.</typeparam>
public sealed class ReactionSlice<TReactor> : IDisposable
    where TReactor : class
{
    readonly List<(EventSourceId Source, object[] Events)> _batches = [];
    readonly ReactionSliceOutcome<TReactor> _outcome;
    readonly Lock _executionLock = new();
    bool _started;
    bool _completed;
    bool _disposed;

    /// <summary>
    /// Initializes a slice owning a scenario, optionally using per-run defaults.
    /// </summary>
    /// <param name="defaults">The artifact and service defaults, or the usual scenario defaults when omitted.</param>
    public ReactionSlice(Defaults? defaults = null)
    {
        Scenario = defaults is null ? new() : new(defaults);
        _outcome = new(Scenario);
    }

    /// <summary>
    /// Gets the live scenario for dependencies, seeds, routing, and advanced assertions.
    /// </summary>
    public ReactorScenario<TReactor> Scenario { get; }

    /// <summary>
    /// Gets the outcome only after successful completion; this never invokes the reactor.
    /// </summary>
    /// <exception cref="SliceNotRun">Execution has not completed successfully.</exception>
    /// <exception cref="SliceDisposed">The slice has been disposed.</exception>
    public ReactionSliceOutcome<TReactor> Then
    {
        get
        {
            EnsureActive();
            if (!_completed) throw new SliceNotRun();

            return _outcome;
        }
    }

    /// <summary>
    /// Copies a batch of event references without delivering it to the reactor yet.
    /// </summary>
    /// <param name="eventSourceId">The event source.</param>
    /// <param name="events">The ordered events; event objects themselves are not cloned.</param>
    /// <returns>This slice.</returns>
    /// <exception cref="SliceAlreadyRun">Execution has started.</exception>
    /// <exception cref="SliceDisposed">The slice has been disposed.</exception>
    public ReactionSlice<TReactor> GivenEvents(EventSourceId eventSourceId, params object[] events)
    {
        lock (_executionLock)
        {
            EnsureNotStarted();
            _batches.Add((eventSourceId, events.ToArray()));
        }

        return this;
    }

    /// <summary>
    /// Delivers each queued batch in declaration order, awaiting every delivery, exactly once.
    /// </summary>
    /// <returns>The outcome after every delivery completes successfully.</returns>
    /// <exception cref="SliceAlreadyRun">Execution has already started, including a previous failed run.</exception>
    /// <exception cref="SliceDisposed">The slice has been disposed.</exception>
    public async Task<ReactionSliceOutcome<TReactor>> Run()
    {
        lock (_executionLock)
        {
            EnsureNotStarted();
            _started = true;
        }
        foreach (var batch in _batches) await Scenario.Given.ForEventSource(batch.Source).Events(batch.Events);
        _completed = true;

        return _outcome;
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
