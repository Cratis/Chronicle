// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery;

/// <summary>
/// The same reactor shape, observing the inbox of another event store instead of the local event log.
/// </summary>
[EventStore(UpstreamOrderConfirmations.SourceEventStore)]
public class UpstreamOrderConfirmations : IReactor
{
    public const string SourceEventStore = "upstream";

    public Task Handle(OrderPlaced @event) => Task.CompletedTask;
}
