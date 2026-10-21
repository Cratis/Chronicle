// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_rediscovery_reenters_payload_building : given.an_explicit_store
{
    Exception _error;

    async Task Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>();
        _store = CreateStore();
        await _store.DiscoverAll();
        _generator.BeforeGenerate = (_, _, _) => _store.DiscoverAll().GetAwaiter().GetResult();
    }

    async Task Because() => _error = await Catch.Exception(_store.RegisterAll).WaitAsync(TimeSpan.FromSeconds(10));

    [Fact] void should_reject_reentry_before_joining_rediscovery() => _error.ShouldBeOfExactType<ArtifactPreparationReentered>();
    [Fact] void should_publish_the_reentry_failure() => ReferenceEquals(_store.Registration.Failure, _error).ShouldBeTrue();
    [Fact] void should_send_no_registration_effects() => _services.EventStores.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_retry_deterministic_payload_errors() => _store.PendingBackgroundRegistrationRetry.ShouldBeNull();
}
