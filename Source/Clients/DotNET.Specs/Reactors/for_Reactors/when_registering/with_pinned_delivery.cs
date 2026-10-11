// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering;

public class with_pinned_delivery : given.all_dependencies
{
    ReactorDefinition _definition;
    protected override Observation.EventGenerationDelivery GenerationDelivery => Observation.EventGenerationDelivery.Pinned;
    void Establish() => _services.Reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>()).Returns(call =>
    {
        call.Arg<IObservable<ReactorMessage>>().Take(1).Subscribe(message => _definition = message.Content.Value0!.Reactor);
        return Observable.Never<EventsToObserve>();
    });
    async Task Because() => await _reactors.Register("pinned", builder => builder.WithEventType(new EventType("person-registered", 2)), (_, _) => Task.CompletedTask);
    [Fact] void should_send_pinned_delivery() => _definition.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Pinned);
}
