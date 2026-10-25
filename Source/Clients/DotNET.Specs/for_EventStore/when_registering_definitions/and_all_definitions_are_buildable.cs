// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_definitions;

public class and_all_definitions_are_buildable : given.an_event_store_with_buildable_definitions
{
    Contracts.Projections.RegisterRequest _sent;
    int _registered;

    void Establish()
    {
        _services.Projections.Register(Arg.Any<Contracts.Projections.RegisterRequest>()).Returns(call =>
        {
            _sent = call.Arg<Contracts.Projections.RegisterRequest>();
            return Task.CompletedTask;
        });
        _projections.Registered += () => _registered++;
    }

    async Task Because() => _result = await _eventStore.RegisterDefinitions(new DeploymentGeneration(42));

    [Fact]
    void should_return_the_generation() => _result.Generation.ShouldEqual(new DeploymentGeneration(42));

    [Fact]
    void should_return_and_publish_the_fingerprint() => _eventStore.Registration.Fingerprint.ShouldEqual(_result.Fingerprint);

    [Fact]
    void should_mark_registration_successful() => _eventStore.Registration.IsSuccess.ShouldBeTrue();

    [Fact]
    void should_preserve_the_native_payload_and_notification()
    {
        _sent.Projections.Single().ShouldBeSame(_projections.Definitions.Single());
        _sent.FullSet.ShouldBeTrue();
        _registered.ShouldEqual(1);
    }

    [Fact]
    void should_send_the_fingerprint_and_generation() => _services.EventStores.Received(1).RegisterDeployment(Arg.Is<Contracts.EventStores.RegisterDeploymentRequest>(_ => _.Generation == 42 && _.Fingerprint == _result.Fingerprint.Value));

    [Fact]
    void should_send_in_dependency_order() => Received.InOrder(() =>
    {
        _services.EventStores.EnsureEventStore(Arg.Any<Contracts.EventStores.EnsureEventStoreRequest>());
        _services.EventStores.RegisterDeployment(Arg.Any<Contracts.EventStores.RegisterDeploymentRequest>());
        _eventStore.EventTypes.Register();
        _eventStore.EventSources.Register();
        _eventStore.ReadModels.Register();
        _eventStore.Constraints.Register();
        _services.Projections.Register(Arg.Any<Contracts.Projections.RegisterRequest>());
    });

    [Fact]
    void should_not_start_observers_or_seed()
    {
        _eventStore.Reactors.DidNotReceive().Register();
        _eventStore.Reducers.DidNotReceive().Register();
        _eventStore.ReadModelReactors.DidNotReceive().Start();
        _eventStore.Subscriptions.DidNotReceiveWithAnyArgs().Subscribe(EventStoreSubscriptionId.Unspecified, string.Empty);
        _eventStore.Seeding.DidNotReceive().Register();
    }
}
