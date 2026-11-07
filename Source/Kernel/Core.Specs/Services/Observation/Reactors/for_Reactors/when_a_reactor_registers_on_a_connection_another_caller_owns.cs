// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Storage;
using Cratis.Traces;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_a_reactor_registers_on_a_connection_another_caller_owns : Specification
{
    IReactorMediator _mediator;
    IConnectionOwnership _ownership;
    IGrainFactory _grainFactory;
    Exception _error;

    void Establish()
    {
        _ownership = Substitute.For<IConnectionOwnership>();
        _mediator = Substitute.For<IReactorMediator>();
        _grainFactory = Substitute.For<IGrainFactory>();
    }

    async Task Because()
    {
        using var messages = new Subject<ReactorMessage>();
        var reactors = new global::Cratis.Chronicle.Services.Observation.Reactors.Reactors(
            _grainFactory, _mediator, Substitute.For<IStorage>(), new JsonSerializerOptions(), new ActivitySource<Reactors>(), Options.Create(new ChronicleOptions()), _ownership, NullLogger<Reactors>.Instance, Substitute.For<IObserverRegistrationGate>(), NamespaceCasingContext.Create());
        var observed = reactors.Observe(messages).ToTask();
        messages.OnNext(new ReactorMessage(new Contracts.Primitives.OneOf<Contracts.Observation.Reactors.RegisterReactor, ReactorResult>(new Contracts.Observation.Reactors.RegisterReactor
        {
            ConnectionId = "connection",
            EventStore = "store",
            Namespace = "namespace",
            Reactor = new Contracts.Observation.Reactors.ReactorDefinition { ReactorId = "reactor", EventSequenceId = "event-log" }
        })));
        _error = await Catch.Exception(() => observed.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact] void should_refuse_the_registration() => (_error is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_not_subscribe_to_the_mediator() => _mediator.DidNotReceive().Subscribe(Arg.Any<ReactorId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReactorEventsObserver>());
    [Fact] void should_not_reach_a_grain() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
}
