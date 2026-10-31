// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering;

public class without_start_at_tail : given.all_dependencies
{
    ReactorDefinition _definition;

    void Establish()
    {
        _eventStore.EventTypes.Returns(_eventTypes);
        _eventTypes.AllClrTypes.Returns([]);
        _services.Reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            call.Arg<IObservable<ReactorMessage>>().Take(1).Subscribe(message => _definition = message.Content.Value0!.Reactor);
            return Observable.Never<EventsToObserve>();
        });
        _reactors.Register<MyReactor>().GetAwaiter().GetResult();
    }

    async Task Because() => await _reactors.Register();

    [Fact] void should_start_from_the_beginning_as_before() => _definition.StartAtTail.ShouldBeFalse();

    class MyReactor : IReactor;
}
