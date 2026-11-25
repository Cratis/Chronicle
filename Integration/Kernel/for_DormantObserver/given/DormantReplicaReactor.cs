// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.given;

/// <summary>
/// The reactor of a replica that runs another version of the service: it has the identity of <see cref="DormantObserverReactor"/>
/// and handles one event type more, so the observer sees a different registration from each replica.
/// </summary>
[Reactor("Cratis.Chronicle.Kernel.Integration.for_DormantObserver.given.DormantObserverReactor")]
[DependencyInjection.IgnoreConvention]
[ExcludeFromFixtureDiscovery]
public class DormantReplicaReactor : IReactor
{
    public ConcurrentQueue<EventSequenceNumber> Handled { get; } = new();

    public Task OnDormantObserverEvent(DormantObserverEvent evt, EventContext context)
    {
        Handled.Enqueue(context.SequenceNumber);
        return Task.CompletedTask;
    }

    public Task OnDormantGammaEvent(DormantGammaEvent evt, EventContext context) => Task.CompletedTask;
}
