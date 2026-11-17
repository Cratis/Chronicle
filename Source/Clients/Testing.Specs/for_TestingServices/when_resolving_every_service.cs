// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.Occurrences;

namespace Cratis.Chronicle.Testing.for_TestingServices;

public class when_resolving_every_service : given.testing_services
{
    [Fact] void should_resolve_observers() => _services.Observers.ShouldBeOfExactType<ObserversForTesting>();
    [Fact] void should_resolve_failed_partitions() => _services.FailedPartitions.ShouldNotBeNull();
    [Fact] void should_resolve_reactors() => _services.Reactors.ShouldNotBeNull();
    [Fact] void should_resolve_reducers() => _services.Reducers.ShouldNotBeNull();
    [Fact] void should_resolve_projections() => _services.Projections.ShouldNotBeNull();
    [Fact] void should_resolve_webhooks() => _services.Webhooks.ShouldNotBeNull();
    [Fact] void should_resolve_external_services() => _services.ExternalServices.ShouldNotBeNull();
    [Fact] void should_resolve_captures() => _services.Captures.ShouldNotBeNull();
    [Fact] void should_resolve_occurrences_with_replies() => _services.Occurrences.ShouldBeOfExactType<OccurrenceReplies>();
    [Fact] void should_resolve_event_store_subscriptions() => _services.EventStoreSubscriptions.ShouldNotBeNull();
    [Fact] void should_resolve_jobs() => _services.Jobs.ShouldNotBeNull();
    [Fact] void should_resolve_seeding() => _services.Seeding.ShouldNotBeNull();
    [Fact] void should_resolve_sequences_with_replies() => _services.Sequences.ShouldBeOfExactType<SequenceReplies>();
    [Fact] void should_resolve_event_stores() => _services.EventStores.ShouldNotBeNull();
    [Fact] void should_resolve_namespaces() => _services.Namespaces.ShouldNotBeNull();
    [Fact] void should_resolve_identities() => _services.Identities.ShouldNotBeNull();
    [Fact] void should_resolve_event_types() => _services.EventTypes.ShouldNotBeNull();
    [Fact] void should_resolve_event_sources() => _services.EventSources.ShouldNotBeNull();
    [Fact] void should_resolve_patterns() => _services.Patterns.ShouldNotBeNull();
    [Fact] void should_resolve_recommendations() => _services.Recommendations.ShouldNotBeNull();
    [Fact] void should_resolve_constraints() => _services.Constraints.ShouldNotBeNull();
    [Fact] void should_resolve_users() => _services.Users.ShouldNotBeNull();
    [Fact] void should_resolve_applications() => _services.Applications.ShouldNotBeNull();
    [Fact] void should_resolve_server() => _services.Server.ShouldNotBeNull();
    [Fact] void should_resolve_read_models() => _services.ReadModels.ShouldNotBeNull();
    [Fact] void should_resolve_compliance() => _services.Compliance.ShouldNotBeNull();
}
