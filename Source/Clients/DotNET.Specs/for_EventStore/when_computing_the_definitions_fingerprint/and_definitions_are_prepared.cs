// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.for_EventStore.when_computing_the_definitions_fingerprint;

public class and_definitions_are_prepared : when_preparing_for_registration.given.an_explicit_store
{
    ArtifactFingerprint _fingerprint;
    DefinitionsRegistration _registered;
    bool _noWrites;
    bool _unchangedReadiness;
    bool _preciseParity;
    bool _eventTargetUsesRegistryIdentity;
    string _compensationFor;

    async Task Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("fingerprint"))
            .RegisterEventType<Compensation>(_ => _.WithId("compensation"))
            .RegisterEventType<Output>(_ => _.WithId("explicit-output"))
            .RegisterProjection<Output>(_ => { })
            .RegisterProjection<Model>(_ => { })
            .RegisterEventSource<Source>("source")
            .RegisterConstraint<UniqueName>();
        _services.EventStores.RegisterDeployment(Arg.Any<Contracts.EventStores.RegisterDeploymentRequest>()).Returns(Contracts.Commands.CommandResult.Success(Guid.NewGuid()));
        _services.EventSources.RegisterEventSources(Arg.Any<Contracts.EventSources.RegisterEventSourcesRequest>()).Returns(Contracts.Commands.CommandResult.Success(Guid.NewGuid()));
        _activator.ActivateNonDisposable(typeof(UniqueName)).Returns(new UniqueName());
        _store = CreateStore();
        await _store.DiscoverAll();
    }

    async Task Because()
    {
        var before = _store.Registration;
        _fingerprint = await _store.ComputeDefinitionsFingerprint();
        _unchangedReadiness = ReferenceEquals(before, _store.Registration);
        _noWrites = !_services.EventStores.ReceivedCalls().Any() && !_services.EventTypes.ReceivedCalls().Any() &&
            !_services.ReadModels.ReceivedCalls().Any() && !_services.Constraints.ReceivedCalls().Any() &&
            !_services.EventSources.ReceivedCalls().Any() && !_services.Projections.ReceivedCalls().Any();
        _registered = await _store.RegisterDefinitions();
        ((IKernelCapabilities)_connection).Capabilities.Returns([KernelCapabilities.PreciseEventTypeSchemas]);
        var precise = await _store.ComputeDefinitionsFingerprint();
        var registration = await _store.RegisterDefinitions();
        _preciseParity = precise == registration.Fingerprint;
        var models = ((ReadModels.ReadModels)_store.ReadModels).PrepareRegistration();
        _eventTargetUsesRegistryIdentity = models.ReadModels.Any(_ => _.Sink.EventSequence?.EventType.Id == "explicit-output");
        var eventTypes = await ((Events.EventTypes)_store.EventTypes).PrepareRegistration();
        _compensationFor = (await JsonSchema.FromJsonAsync(eventTypes.Types.Single(_ => _.Type.Id == "compensation").Schema)).GetCompensationFor();
    }

    [Fact] void should_match_deployment_registration() => _fingerprint.ShouldEqual(_registered.Fingerprint);
    [Fact] void should_not_write_anything_during_the_check() => _noWrites.ShouldBeTrue();
    [Fact] void should_preserve_readiness() => _unchangedReadiness.ShouldBeTrue();
    [Fact] void should_match_precise_registration_after_the_capability_transition() => _preciseParity.ShouldBeTrue();
    [Fact] void should_include_registry_bound_compensation_metadata() => _compensationFor.ShouldEqual("fingerprint");
    [Fact] void should_include_the_registry_bound_event_target() => _eventTargetUsesRegistryIdentity.ShouldBeTrue();
    [Fact] void should_include_event_sources() => ((EventSources.EventSources)_store.EventSources).PrepareRegistration()!.Sources.Count().ShouldEqual(1);
    [Fact] void should_include_constraints() => ((Events.Constraints.Constraints)_store.Constraints).PrepareRegistration().Constraints.Count.ShouldEqual(1);

    [Events.Public]
    public record Output(string Name);
    public record Model(string Name);
    public class Source : IEventSource;
    public class UniqueName : IConstraint
    {
        public void Define(IConstraintBuilder builder) => builder.Unique<PlainEvent>();
    }
}
