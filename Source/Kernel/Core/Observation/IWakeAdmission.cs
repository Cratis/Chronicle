// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Bounds how many dormant observers one silo wakes at the same time (decision 0034).
/// </summary>
/// <remarks>
/// One event that many dormant observers watch for wakes all of them. The queue takes a place here before it activates a
/// dormant observer and gives it back once the observer has resumed, so a burst becomes a short wait instead of a spike of
/// activations that read storage and call clients together.
/// </remarks>
public interface IWakeAdmission
{
    /// <summary>
    /// Gets the number of dormant observers this silo wakes at the same time.
    /// </summary>
    int Limit { get; }

    /// <summary>
    /// Gets the number of wakes in progress.
    /// </summary>
    int InProgress { get; }

    /// <summary>
    /// Waits for a place to wake a dormant observer.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that stops waiting.</param>
    /// <returns>A task that completes once the caller holds a place, which it gives back with <see cref="Release"/>.</returns>
    Task WaitAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gives back a place taken by <see cref="WaitAsync"/>.
    /// </summary>
    void Release();
}
