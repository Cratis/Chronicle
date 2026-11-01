// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverDeliveryRuns.given;

public class a_controlled_subscription : Specification
{
    protected static readonly EventType _a = new("529bd8fb-d0ce-4d40-992b-205ee551f217", 1);
    protected static readonly EventType _e = new("758264df-adc9-4745-9b3c-85ea6920b2df", 2);
    protected ObserverSubscription _subscription;
    protected ObserverSubscriberTarget _old;
    protected ObserverSubscriberTarget _new;
    protected IObserverSubscriberSelector _selector;

    protected static AppendedEvent Event(EventType type, ulong sequenceNumber, EventObservationState mode = EventObservationState.None) =>
        AppendedEvent.EmptyWithEventType(type) with
        {
            Context = EventContext.Empty with { EventType = type, SequenceNumber = sequenceNumber, ObservationState = mode }
        };

    void Establish()
    {
        _old = new(SiloAddress.Zero) { DeclaredEventTypes = [_a], GenerationDelivery = EventGenerationDelivery.Compatibility };
        _new = new(SiloAddress.Zero) { DeclaredEventTypes = [_a, _e], GenerationDelivery = EventGenerationDelivery.Pinned };
        _subscription = ObserverSubscription.Unsubscribed with
        {
            TypeSetRevision = new(2),
            TypeSetMembers = new Dictionary<EventTypeId, ObserverTypeSetMember>
            {
                [_a.Id] = new(new(1), null),
                [_e.Id] = new(new(2), 10)
            },
            Targets = [_old, _new]
        };
        _selector = Substitute.For<IObserverSubscriberSelector>();
        _selector.Select(Arg.Any<ObserverSubscription>(), Arg.Any<Cratis.Chronicle.Concepts.Keys.Key>())
            .Returns(call => call.Arg<ObserverSubscription>().Targets[0]);
    }
}
