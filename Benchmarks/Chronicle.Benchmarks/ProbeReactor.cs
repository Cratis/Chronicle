// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Records exact delivery identities for the local measurement spike.
/// </summary>
[Reactor("probe-4748")]
public class ProbeReactor : IReactor
{
    /// <summary>
    /// Records one observed event, including redeliveries.
    /// </summary>
    /// <param name="evt">The measured payload.</param>
    /// <param name="context">The delivered identity and sequence number.</param>
    /// <returns>A completed task.</returns>
    public Task Observe(BenchmarkEvent evt, EventContext context)
    {
        ProbeDelivery.Current.Record(new(context.EventSourceId.Value, context.SequenceNumber.Value), evt.Value, new(evt.Value, evt.Timestamp));
        return Task.CompletedTask;
    }
}
