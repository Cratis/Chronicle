// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_TriggerRegistry;

public class when_registering_discovered_triggers : given.an_occurrence_client
{
    void Establish() => _registry.Discover();

    Task Because() => _registry.Register();

    [Fact] void should_bind_the_event_store() => _registration.EventStore.ShouldEqual("store");
    [Fact] void should_keep_the_explicit_identity() => _registration.Triggers.Single().Id.ShouldEqual("test-trigger");
    [Fact] void should_keep_the_type_name() => _registration.Triggers.Single().Name.ShouldEqual(nameof(given.TestTrigger));
    [Fact] void should_send_the_generated_schema() => _registration.Triggers.Single().Schema.ShouldEqual(_schemas.Generate(typeof(given.TestTrigger)).ToJson());
}
