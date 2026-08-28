// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.ExternalServices;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Seeding;
using Cratis.Chronicle.Transactions;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Patterns.for_IEventStore;

/// <summary>
/// An event store written before patterns existed - a test double, a scenario harness in a consuming framework -
/// must keep compiling and loading. It says it does not support patterns by throwing, never by answering with
/// something that looks like an absence of behavior.
/// </summary>
public class when_an_implementation_does_not_support_patterns : Specification
{
    Exception _exception;
    IPatterns _resolved;

    IEventStore _eventStore;

    void Establish() => _eventStore = new an_event_store_from_before_patterns();

    void Because() => _exception = Catch.Exception(() => _resolved = _eventStore.Patterns);

    [Fact] void should_throw_that_patterns_are_not_supported() => _exception.ShouldBeOfExactType<PatternsNotSupported>();

    class an_event_store_from_before_patterns : IEventStore
    {
        public EventStoreName Name => EventStoreName.NotSet;
        public EventStoreNamespaceName Namespace => EventStoreNamespaceName.NotSet;
        public IChronicleConnection Connection => throw new NotSupportedException();
        public IUnitOfWorkManager UnitOfWorkManager => throw new NotSupportedException();
        public IEventTypes EventTypes => throw new NotSupportedException();
        public IConstraints Constraints => throw new NotSupportedException();
        public IEventLog EventLog => throw new NotSupportedException();
        public IJobs Jobs => throw new NotSupportedException();
        public IReactors Reactors => throw new NotSupportedException();
        public IReducers Reducers => throw new NotSupportedException();
        public IProjections Projections => throw new NotSupportedException();
        public IWebhooks Webhooks => throw new NotSupportedException();
        public IExternalServices ExternalServices => throw new NotSupportedException();
        public IEventStoreSubscriptions Subscriptions => throw new NotSupportedException();
        public IFailedPartitions FailedPartitions => throw new NotSupportedException();
        public IReadModels ReadModels => throw new NotSupportedException();
        public IReadModelReactors ReadModelReactors => throw new NotSupportedException();
        public IEventSeeding Seeding => throw new NotSupportedException();
        public IPIIManager PII => throw new NotSupportedException();
        public IIdentityManager Identities => throw new NotSupportedException();
        public RegistrationOutcome Registration => RegistrationOutcome.NotRun;

        public Task DiscoverAll() => Task.CompletedTask;
        public Task RegisterAll() => Task.CompletedTask;
        public IEventSequence GetEventSequence(EventSequenceId id) => throw new NotSupportedException();
        public Task<IEnumerable<EventStoreNamespaceName>> GetNamespaces(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
