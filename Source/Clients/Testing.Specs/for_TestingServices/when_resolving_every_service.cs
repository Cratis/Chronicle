// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Captures;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSources;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.ExternalServices;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Contracts.Identities;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Patterns;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Contracts.Seeding;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.Occurrences;

namespace Cratis.Chronicle.Testing.for_TestingServices;

public class when_resolving_every_service : given.testing_services
{
    IObservers _observers;
    IFailedPartitions _failedPartitions;
    IReactors _reactors;
    IReducers _reducers;
    IProjections _projections;
    IWebhooks _webhooks;
    IExternalServices _externalServices;
    ICaptures _captures;
    Contracts.Occurrences.IOccurrences _occurrences;
    IEventStoreSubscriptions _eventStoreSubscriptions;
    IJobs _jobs;
    IEventSeeding _seeding;
    Contracts.Sequences.IEventSequences _sequences;
    IEventStores _eventStores;
    INamespaces _namespaces;
    IIdentities _identities;
    IEventTypes _eventTypes;
    IEventSources _eventSources;
    IPatterns _patterns;
    IRecommendations _recommendations;
    IConstraints _constraints;
    IUsers _users;
    IApplications _applications;
    IServer _server;
    IReadModels _readModels;
    ICompliance _compliance;

    void Because()
    {
        _observers = _services.Observers;
        _failedPartitions = _services.FailedPartitions;
        _reactors = _services.Reactors;
        _reducers = _services.Reducers;
        _projections = _services.Projections;
        _webhooks = _services.Webhooks;
        _externalServices = _services.ExternalServices;
        _captures = _services.Captures;
        _occurrences = _services.Occurrences;
        _eventStoreSubscriptions = _services.EventStoreSubscriptions;
        _jobs = _services.Jobs;
        _seeding = _services.Seeding;
        _sequences = _services.Sequences;
        _eventStores = _services.EventStores;
        _namespaces = _services.Namespaces;
        _identities = _services.Identities;
        _eventTypes = _services.EventTypes;
        _eventSources = _services.EventSources;
        _patterns = _services.Patterns;
        _recommendations = _services.Recommendations;
        _constraints = _services.Constraints;
        _users = _services.Users;
        _applications = _services.Applications;
        _server = _services.Server;
        _readModels = _services.ReadModels;
        _compliance = _services.Compliance;
    }

    [Fact] void should_resolve_observers() => _observers.ShouldBeOfExactType<ObserversForTesting>();
    [Fact] void should_resolve_failed_partitions() => _failedPartitions.ShouldNotBeNull();
    [Fact] void should_resolve_reactors() => _reactors.ShouldNotBeNull();
    [Fact] void should_resolve_reducers() => _reducers.ShouldNotBeNull();
    [Fact] void should_resolve_projections() => _projections.ShouldNotBeNull();
    [Fact] void should_resolve_webhooks() => _webhooks.ShouldNotBeNull();
    [Fact] void should_resolve_external_services() => _externalServices.ShouldNotBeNull();
    [Fact] void should_resolve_captures() => _captures.ShouldNotBeNull();
    [Fact] void should_resolve_occurrences_with_replies() => _occurrences.ShouldBeOfExactType<OccurrenceReplies>();
    [Fact] void should_resolve_event_store_subscriptions() => _eventStoreSubscriptions.ShouldNotBeNull();
    [Fact] void should_resolve_jobs() => _jobs.ShouldNotBeNull();
    [Fact] void should_resolve_seeding() => _seeding.ShouldNotBeNull();
    [Fact] void should_resolve_sequences_with_replies() => _sequences.ShouldBeOfExactType<SequenceReplies>();
    [Fact] void should_resolve_event_stores() => _eventStores.ShouldNotBeNull();
    [Fact] void should_resolve_namespaces() => _namespaces.ShouldNotBeNull();
    [Fact] void should_resolve_identities() => _identities.ShouldNotBeNull();
    [Fact] void should_resolve_event_types() => _eventTypes.ShouldNotBeNull();
    [Fact] void should_resolve_event_sources() => _eventSources.ShouldNotBeNull();
    [Fact] void should_resolve_patterns() => _patterns.ShouldNotBeNull();
    [Fact] void should_resolve_recommendations() => _recommendations.ShouldNotBeNull();
    [Fact] void should_resolve_constraints() => _constraints.ShouldNotBeNull();
    [Fact] void should_resolve_users() => _users.ShouldNotBeNull();
    [Fact] void should_resolve_applications() => _applications.ShouldNotBeNull();
    [Fact] void should_resolve_server() => _server.ShouldNotBeNull();
    [Fact] void should_resolve_read_models() => _readModels.ShouldNotBeNull();
    [Fact] void should_resolve_compliance() => _compliance.ShouldNotBeNull();
}
