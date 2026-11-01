// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.ExternalServices;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Seeding;
using Cratis.Chronicle.Transactions;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Registrations.for_IEventStore;

public class when_an_implementation_does_not_support_definitions_registration : Specification
{
    IEventStore _eventStore;
    Exception _failure;

    void Establish() => _eventStore = new RuntimeOnlyEventStore();

    async Task Because() => _failure = await Catch.Exception(() => _eventStore.RegisterDefinitions());

    [Fact]
    void should_fail_explicitly() => _failure.ShouldBeOfExactType<DefinitionsRegistrationNotSupported>();

    [Fact]
    void should_identify_the_implementation() => _failure.Message.Contains(nameof(RuntimeOnlyEventStore)).ShouldBeTrue();

    class RuntimeOnlyEventStore : IEventStore
    {
        public EventStoreName Name => "runtime-only";
        public IChronicleConnection Connection => throw new NotSupportedException();
        public EventStoreNamespaceName Namespace => EventStoreNamespaceName.Default;
        public IEventTypes EventTypes => throw new NotSupportedException();
        public IEventSources EventSources => throw new NotSupportedException();
        public IEventLog EventLog => throw new NotSupportedException();
        public IUnitOfWorkManager UnitOfWorkManager => throw new NotSupportedException();
        public IJobs Jobs => throw new NotSupportedException();
        public IWebhooks Webhooks => throw new NotSupportedException();
        public IExternalServices ExternalServices => throw new NotSupportedException();
        public IFailedPartitions FailedPartitions => throw new NotSupportedException();
        public IPIIManager PII => throw new NotSupportedException();
        public IIdentityManager Identities => throw new NotSupportedException();
        public IReactors Reactors => throw new NotSupportedException();
        public IReducers Reducers => throw new NotSupportedException();
        public IReadModelReactors ReadModelReactors => throw new NotSupportedException();
        public IProjections Projections => throw new NotSupportedException();
        public IReadModels ReadModels => throw new NotSupportedException();
        public IConstraints Constraints => throw new NotSupportedException();
        public IEventSeeding Seeding => throw new NotSupportedException();
        public IEventStoreSubscriptions Subscriptions => throw new NotSupportedException();
        public RegistrationOutcome Registration => RegistrationOutcome.NotRun;
        public Task DiscoverAll() => Task.CompletedTask;
        public Task RegisterAll() => Task.CompletedTask;
        public IEventSequence GetEventSequence(EventSequenceId id) => throw new NotSupportedException();
        public Task<IEnumerable<EventStoreNamespaceName>> GetNamespaces(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
