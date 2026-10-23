// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_application_metadata;

public class and_the_event_store_name_is_declared : given.application_metadata_sharing
{
    const string DeclaredStore = "declaring-scenario-store";

    readonly InMemoryClientArtifactsProvider _declaringArtifacts = new() { EventTypes = [typeof(DeclaredStoreRecorded)] };
    EventStoreForTesting _undeclared;

    void Establish() => _undeclared = new(null, _declaringArtifacts, null, new ScenarioTime(), _name, _namespace);

    void Because()
    {
        _other = new(null, _declaringArtifacts, null, new ScenarioTime(), new(DeclaredStore), _namespace);
        _third = new(null, _declaringArtifacts, null, new ScenarioTime(), new(DeclaredStore), _namespace);
    }

    void Destroy() => _undeclared?.Dispose();

    [Fact] void should_not_share_with_a_store_of_an_undeclared_name() => _undeclared.Metadata.ShouldNotBeSame(_other.Metadata);
    [Fact] void should_share_between_stores_of_the_declared_name() => _other.Metadata.ShouldBeSame(_third.Metadata);

    [EventType, EventStore(DeclaredStore)]
    public record DeclaredStoreRecorded(string Value);
}
