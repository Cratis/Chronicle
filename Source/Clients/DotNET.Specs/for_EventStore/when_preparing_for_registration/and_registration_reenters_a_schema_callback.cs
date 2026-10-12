// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_registration_reenters_a_schema_callback : given.an_explicit_store
{
    Exception _error;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>();
        _generator.BeforeGenerate = (_, _, _) => _store.RegisterAll().GetAwaiter().GetResult();
        _store = CreateStore();
    }

    async Task Because() => _error = await Catch.Exception(_store.DiscoverAll).WaitAsync(TimeSpan.FromSeconds(10));

    [Fact] void should_reject_reentry_before_joining_a_registration_flight() => _error.ShouldBeOfExactType<ArtifactPreparationReentered>();
    [Fact] void should_send_no_registration_effects() => _services.EventStores.ReceivedCalls().ShouldBeEmpty();
}
