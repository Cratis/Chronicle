// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;

namespace Cratis.Chronicle.for_EventStore.when_computing_the_definitions_fingerprint;

public class and_diagnostic_schema_generation_fails_then_registration_recovers : when_preparing_for_registration.given.an_explicit_store
{
    Exception _diagnosticFailure;
    Exception _ordinaryFailure;
    Exception _definitionsFailure;
    bool _unchanged;
    bool _successfulVerdict;

    async Task Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("recovery"));
        _services.EventStores.RegisterDeployment(Arg.Any<Contracts.EventStores.RegisterDeploymentRequest>()).Returns(Contracts.Commands.CommandResult.Success(Guid.NewGuid()));
        _store = CreateStore();
        await _store.DiscoverAll();
        await _store.RegisterAll();
    }

    async Task Because()
    {
        var before = _store.Registration;
        _generator.BeforeGenerate = (_, _, _) => throw new InvalidOperationException("diagnostic only");
        _diagnosticFailure = await Catch.Exception(_store.ComputeDefinitionsFingerprint);
        _unchanged = ReferenceEquals(before, _store.Registration);
        _successfulVerdict = _store.LatestPreparationVerdict.IsCompletedSuccessfully;
        _generator.BeforeGenerate = null;
        _ordinaryFailure = await Catch.Exception(_store.RegisterAll);
        _definitionsFailure = await Catch.Exception(() => _store.RegisterDefinitions());
    }

    [Fact] void should_fail_only_the_diagnostic() => _diagnosticFailure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_preserve_registration_readiness() => _unchanged.ShouldBeTrue();
    [Fact] void should_preserve_the_preparation_verdict() => _successfulVerdict.ShouldBeTrue();
    [Fact] void should_allow_register_all_without_rediscovery() => _ordinaryFailure.ShouldBeNull();
    [Fact] void should_allow_definitions_registration_without_rediscovery() => _definitionsFailure.ShouldBeNull();
    [Fact] void should_send_all_three_normal_event_payloads() => _services.EventTypes.Received(3).RegisterEventTypes(Arg.Any<RegisterEventTypesRequest>());
    [Fact] void should_record_the_recovered_deployment() => _services.EventStores.Received(1).RegisterDeployment(Arg.Any<Contracts.EventStores.RegisterDeploymentRequest>());
}
