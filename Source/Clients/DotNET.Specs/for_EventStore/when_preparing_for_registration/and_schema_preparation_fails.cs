// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_schema_preparation_fails : given.an_explicit_store
{
    Exception _failure;
    Exception _discoveryError;
    Exception _registrationError;
    Exception _directError;

    void Establish()
    {
        _failure = new Exception("schema failure");
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>();
        _generator.BeforeGenerate = (_, _, _) => throw _failure;
        _store = CreateStore();
    }

    async Task Because()
    {
        _discoveryError = await Catch.Exception(_store.DiscoverAll);
        _registrationError = await Catch.Exception(_store.RegisterAll);
        _directError = await Catch.Exception(_store.EventTypes.Register);
    }

    [Fact] void should_preserve_the_discovery_failure() => ReferenceEquals(_discoveryError, _failure).ShouldBeTrue();
    [Fact] void should_fail_registration_with_the_same_verdict() => ReferenceEquals(_registrationError, _failure).ShouldBeTrue();
    [Fact] void should_block_direct_event_registration() => ReferenceEquals(_directError, _failure).ShouldBeTrue();
    [Fact] void should_not_treat_available_identities_as_readiness() => _store.EventTypes.HasFor(typeof(PlainEvent)).ShouldBeTrue();
    [Fact] void should_publish_the_exact_failed_registration_outcome() => ReferenceEquals(_store.Registration.Failure, _failure).ShouldBeTrue();
    [Fact] void should_send_no_store_registration_calls() => _services.EventStores.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_send_no_event_registration_calls() => _services.EventTypes.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_send_no_read_model_registration_calls() => _services.ReadModels.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_start_deterministic_background_retries() => _store.PendingBackgroundRegistrationRetry.ShouldBeNull();
}
