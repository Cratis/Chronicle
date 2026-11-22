// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Owns the terminal, activation-local lifetime of direct persistence continuations.
/// This is neither cancellation of issued work nor a durable ownership fence.
/// </summary>
internal sealed class ObserverContinuationLifetime
{
    int _invalidated;

    /// <summary>Gets whether this activation's direct continuations are still current.</summary>
    internal bool IsCurrent => Volatile.Read(ref _invalidated) == 0;

    /// <summary>Captures this lifetime without reviving an invalidated activation.</summary>
    /// <returns>A checkpoint bound to this activation.</returns>
    internal ObserverContinuationCheckpoint Capture() => new(this);

    /// <summary>Terminally invalidates this lifetime, with visibility across runtime disposal.</summary>
    internal void Invalidate() => Interlocked.Exchange(ref _invalidated, 1);
}
