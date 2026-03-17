// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Events.EventSequences.Migrations;
using Cratis.Chronicle.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeGenerationMigrationReactor;

public class when_event_type_generation_is_added : given.a_reactor
{
    EventTypeGenerationAdded _event;
    EventContext _context;
    EventTypeId _eventTypeId;
    EventStoreName _eventStoreName;
    EventStoreNamespaceName _namespaceName;

    void Establish()
    {
        _eventTypeId = new EventTypeId(Guid.NewGuid().ToString());
        _eventStoreName = new EventStoreName("some-event-store");
        _namespaceName = EventStoreNamespaceName.Default;
        _event = new EventTypeGenerationAdded(_eventTypeId, 2, "{}");
        _context = EventContext.From(
            _eventStoreName,
            _namespaceName,
            EventType.Unknown,
            EventSourceType.Default,
            new EventSourceId("migration"),
            EventStreamType.All,
            EventStreamId.Default,
            EventSequenceNumber.First,
            new CorrelationId(Guid.NewGuid()));

        _jobsManager
            .Start<IMigrateExistingEventsForType, MigrateExistingEventsForTypeRequest>(Arg.Any<MigrateExistingEventsForTypeRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
    }

    async Task Because() => await _reactor.EventTypeGenerationAdded(_event, _context);

    [Fact] void should_start_migration_job() =>
        _jobsManager.Received(1).Start<IMigrateExistingEventsForType, MigrateExistingEventsForTypeRequest>(
            Arg.Is<MigrateExistingEventsForTypeRequest>(r => r.EventTypeId == _eventTypeId));
}
