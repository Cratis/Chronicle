// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf.Grpc;

using ContractReactors = Cratis.Chronicle.Contracts.Observation.Reactors.IReactors;

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_replaying_by_reactor_type : given.all_dependencies
{
    readonly ReactorId _reactorId = "73c0c8ed-f2cd-49a2-b5b9-f2f4e1b7b5d4";
    void Establish()
    {
        _eventStore.EventTypes.Returns(_eventTypes);
        _eventTypes.AllClrTypes.Returns([]);
        var reactors = Substitute.For<ContractReactors>();
        _services.Reactors.Returns(reactors);
        reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>())
            .Returns(Observable.Never<EventsToObserve>());
        _reactors.Register<MyReactor>().GetAwaiter().GetResult();

        _observers.Replay(Arg.Any<Replay>()).Returns(new ReplayResponse { JobId = Guid.NewGuid().ToString() });
    }

    async Task Because() => await _reactors.Replay<MyReactor>();

    [Fact]
    void should_call_replay_with_correct_parameters() =>
        _observers
            .Received(1)
            .Replay(Arg.Is<Replay>(r =>
                r.EventStore == _eventStore.Name.Value &&
                r.Namespace == _eventStore.Namespace.Value &&
                r.ObserverId == _reactorId.Value &&
                r.EventSequenceId == string.Empty));

    [Reactor("73c0c8ed-f2cd-49a2-b5b9-f2f4e1b7b5d4")]
    class MyReactor : IReactor;
}
