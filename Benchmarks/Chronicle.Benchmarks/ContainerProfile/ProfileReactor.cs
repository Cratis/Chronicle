// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>
/// Counts distinct deliveries per store and namespace for the container profiling workload.
/// </summary>
[Reactor("5b0d80b4-6aac-485b-9e38-36721c40c911")]
public class ProfileReactor : IReactor
{
    static readonly ConcurrentDictionary<(string Store, string Namespace), ConcurrentDictionary<string, byte>> _delivered = new();

    /// <summary>
    /// Tracks a distinct event-sequence number, without counting redelivery twice.
    /// </summary>
    /// <param name="event">The benchmark event.</param>
    /// <param name="context">The event context.</param>
    public void Handle(BenchmarkEvent @event, EventContext context)
    {
        ArgumentNullException.ThrowIfNull(@event);
        _delivered.GetOrAdd((context.EventStore.ToString(), context.Namespace.ToString()), _ => new()).TryAdd(context.SequenceNumber.ToString(), 0);
    }

    /// <summary>Counts unique events for one isolated pair.</summary>
    /// <param name="store">The store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <returns>The distinct count.</returns>
    internal static int Count(string store, string @namespace) =>
        _delivered.TryGetValue((store, @namespace), out var delivered) ? delivered.Count : 0;

    /// <summary>Clears counters before starting a fresh run.</summary>
    internal static void Reset() => _delivered.Clear();
}
