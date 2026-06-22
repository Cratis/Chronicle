// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

/// <summary>
/// Captures reactor invocation state so clustering specs can assert on it without static fields.
/// </summary>
/// <remarks>
/// Registered as a singleton in the silo that hosts the in-process Chronicle client (silo1).
/// The reactor injects this and records every handled event here.
/// Specs obtain the instance from <see cref="ClusteringFixture.SiloServices"/> and call
/// <see cref="Reset"/> in Establish to start each context with a clean slate.
/// </remarks>
public class ClusteredReactorSignal
{
    int _handledCount;
    TaskCompletionSource _handled = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the number of times the reactor has been invoked.
    /// </summary>
    public int HandledCount => _handledCount;

    /// <summary>
    /// Gets the reference value from the last handled event.
    /// </summary>
    public Guid LastHandledReference { get; private set; }

    /// <summary>
    /// Gets a <see cref="Task"/> that completes when the reactor handles its first event.
    /// </summary>
    public Task Handled => _handled.Task;

    /// <summary>
    /// Resets all state so the next test context starts from a clean baseline.
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _handledCount, 0);
        LastHandledReference = Guid.Empty;
        _handled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// Records that the reactor handled an event with the given reference value.
    /// </summary>
    /// <param name="reference">The reference value from the handled event.</param>
    public void RecordHandled(Guid reference)
    {
        LastHandledReference = reference;
        Interlocked.Increment(ref _handledCount);
        _handled.TrySetResult();
    }
}
