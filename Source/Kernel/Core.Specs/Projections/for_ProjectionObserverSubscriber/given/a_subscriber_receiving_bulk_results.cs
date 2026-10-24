// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.given;

public class a_subscriber_receiving_bulk_results : a_pinned_subscriber
{
    protected static readonly Key _partition = (Key)"the-partition";
    protected ObserverSubscriberResult _result;

    void Establish() => _event = _event with { Context = _event.Context with { EventType = _pin, SequenceNumber = 1UL } };

    protected static ProjectionEventContext ContextFor(AppendedEvent @event) => ProjectionEventContext.Empty(Substitute.For<IObjectComparer>(), @event);
}
