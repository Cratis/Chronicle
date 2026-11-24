// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation.Multiplexed;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Integration.Clustering.for_MultiplexedObservation;

/// <summary>
/// Passes the kernel's multiplexed observation through and counts the calls made to it and the messages each write carried.
/// </summary>
/// <param name="inner">The kernel's <see cref="IMultiplexedObservation"/>.</param>
public class RecordedMultiplexedObservation(IMultiplexedObservation inner) : IMultiplexedObservation
{
    readonly Lock _lock = new();
    readonly List<int> _writes = [];
    int _calls;

    /// <summary>
    /// Gets how many calls were made to the kernel, which is how many streams the connection used.
    /// </summary>
    public int Calls => Volatile.Read(ref _calls);

    /// <summary>
    /// Gets how many messages each write carried.
    /// </summary>
    public IReadOnlyList<int> Writes
    {
        get
        {
            lock (_lock)
            {
                return [.. _writes];
            }
        }
    }

    /// <inheritdoc/>
    public IObservable<MultiplexedServerMessage> Observe(IObservable<MultiplexedClientMessage> messages, CallContext context = default)
    {
        Interlocked.Increment(ref _calls);
        return inner.Observe(
            messages.Do(write =>
            {
                lock (_lock)
                {
                    _writes.Add(write.Messages.Count);
                }
            }),
            context);
    }
}
