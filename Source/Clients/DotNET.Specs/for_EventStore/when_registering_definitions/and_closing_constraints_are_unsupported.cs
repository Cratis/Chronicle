// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.for_EventStore.when_registering_definitions;

public class and_closing_constraints_are_unsupported : given.an_event_store_with_buildable_definitions
{
    Exception _registrationFailure;

    async Task Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor, IKernelCapabilities>();
        ((IChronicleServicesAccessor)connection).Services.Returns(_services);
        ((IKernelCapabilities)connection).CapabilitiesAreKnown.Returns(true);
        ((IKernelCapabilities)connection).Capabilities.Returns([]);
        var store = Substitute.For<IEventStore>();
        store.Connection.Returns(connection);
        var provider = Substitute.For<ICanProvideConstraints>();
        provider.Provide().Returns([new ClosesStreamConstraintDefinition("closing", _ => "closed", ["closing-event"], default, [])]);
        var constraints = new Events.Constraints.Constraints(store, [provider]);
        await constraints.Discover();
        SetAutoProperty(nameof(EventStore.Constraints), constraints);
    }

    async Task Because()
    {
        _failure = await Catch.Exception(() => _eventStore.RegisterDefinitions());
        _registrationFailure = await Catch.Exception(_eventStore.RegisterAll);
    }

    [Fact]
    void should_preserve_the_existing_capability_failure() => _failure.ShouldBeOfExactType<ClosesStreamConstraintsNotSupported>();

    [Fact]
    void should_keep_the_payload_failure_authoritative() => _registrationFailure.ShouldBeSame(_failure);

    [Fact]
    void should_not_make_any_registration_rpc()
    {
        _services.EventStores.DidNotReceive().EnsureEventStore(Arg.Any<Contracts.EventStores.EnsureEventStoreRequest>());
        _services.EventStores.DidNotReceive().RegisterDeployment(Arg.Any<Contracts.EventStores.RegisterDeploymentRequest>());
        _services.Constraints.DidNotReceive().Register(Arg.Any<Contracts.Events.Constraints.RegisterConstraintsRequest>());
        _services.Projections.DidNotReceive().Register(Arg.Any<Contracts.Projections.RegisterRequest>());
    }
}
