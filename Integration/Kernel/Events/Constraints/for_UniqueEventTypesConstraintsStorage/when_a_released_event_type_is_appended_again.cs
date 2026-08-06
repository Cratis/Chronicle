// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;

using ConceptsEventStoreName = Cratis.Chronicle.Concepts.EventStoreName;
using ConceptsEventStoreNamespaceName = Cratis.Chronicle.Concepts.EventStoreNamespaceName;
using context = Cratis.Chronicle.Kernel.Integration.Events.Constraints.for_UniqueEventTypesConstraintsStorage.when_a_released_event_type_is_appended_again.context;

namespace Cratis.Chronicle.Kernel.Integration.Events.Constraints.for_UniqueEventTypesConstraintsStorage;

/// <summary>
/// The unique event type constraint keeps no index - it is answered by querying the event sequence collection - so
/// "has this event source been released" is a database query, and the query is the whole behavior. A spec against
/// the in-memory storage settles the semantics but says nothing about whether the MongoDB one expresses them: the
/// two share a signature, not a contract.
/// <para>
/// This runs the real storage over a real store, with both event sources in one collection so the per-event-source
/// filtering is exercised too. One borrower has an open loan, the other returned theirs.
/// </para>
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class when_a_released_event_type_is_appended_again(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public bool IsAllowedForTheBorrowerWithAnOpenLoan;
        public EventSequenceNumber SequenceNumberOfTheViolation = default!;
        public bool IsAllowedForTheBorrowerWhoReturned;

        readonly EventSequenceId _eventSequenceId = $"unique-event-type-release-{Guid.NewGuid():N}";
        readonly EventSourceId _borrowerWithAnOpenLoan = $"borrower-open-{Guid.NewGuid():N}";
        readonly EventSourceId _borrowerWhoReturned = $"borrower-returned-{Guid.NewGuid():N}";
        readonly EventType _checkedOutEventType = new($"LoanCheckedOut-{Guid.NewGuid():N}", EventTypeGeneration.First);
        readonly EventType _returnedEventType = new($"LoanReturned-{Guid.NewGuid():N}", EventTypeGeneration.First);

        IEventSequenceStorage _eventSequence = default!;
        IUniqueEventTypesConstraintsStorage _constraints = default!;

        async Task Establish()
        {
            var eventStoreStorage = Services
                .GetRequiredService<IStorage>()
                .GetEventStore((ConceptsEventStoreName)Constants.EventStore);

            // Appending goes through the schema, so the event types have to exist before there is anything to
            // constrain. An empty schema is enough - the constraint reads the event's type and event source, never
            // its content.
            await eventStoreStorage.EventTypes.Register(_checkedOutEventType, new JsonSchema());
            await eventStoreStorage.EventTypes.Register(_returnedEventType, new JsonSchema());

            var namespaceStorage = eventStoreStorage.GetNamespace(ConceptsEventStoreNamespaceName.Default);

            _eventSequence = namespaceStorage.GetEventSequence(_eventSequenceId);
            _constraints = namespaceStorage.GetUniqueEventTypesConstraints(_eventSequenceId);

            // One borrower opened a loan, returned it and opened another - so the current cycle is held by the
            // event at sequence number 2, not the one at 0.
            await Append(0, _checkedOutEventType, _borrowerWithAnOpenLoan);
            await Append(1, _returnedEventType, _borrowerWithAnOpenLoan);
            await Append(2, _checkedOutEventType, _borrowerWithAnOpenLoan);

            // The other opened a loan and returned it, so nothing holds their constraint.
            await Append(3, _checkedOutEventType, _borrowerWhoReturned);
            await Append(4, _returnedEventType, _borrowerWhoReturned);
        }

        async Task Because()
        {
            (IsAllowedForTheBorrowerWithAnOpenLoan, SequenceNumberOfTheViolation) = await _constraints.IsAllowed(Definition, _borrowerWithAnOpenLoan);
            (IsAllowedForTheBorrowerWhoReturned, _) = await _constraints.IsAllowed(Definition, _borrowerWhoReturned);
        }

        UniqueEventTypeConstraintDefinition Definition =>
            new($"loan-open-{_eventSequenceId}", [_checkedOutEventType.Id], _returnedEventType.Id);

        Task Append(EventSequenceNumber sequenceNumber, EventType eventType, EventSourceId eventSourceId) =>
            _eventSequence.Append(
                sequenceNumber,
                EventSourceType.Default,
                eventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                eventType,
                CorrelationId.New(),
                [],
                [],
                [],
                DateTimeOffset.UtcNow,
                new Dictionary<EventTypeGeneration, ExpandoObject> { { EventTypeGeneration.First, new ExpandoObject() } },
                new Dictionary<EventTypeGeneration, EventHash>());
    }

    [Fact] void should_not_allow_a_covered_event_while_the_cycle_is_open() => Context.IsAllowedForTheBorrowerWithAnOpenLoan.ShouldBeFalse();
    [Fact] void should_report_the_violation_from_the_open_cycle() => Context.SequenceNumberOfTheViolation.ShouldEqual((EventSequenceNumber)2U);
    [Fact] void should_allow_a_covered_event_once_the_cycle_is_closed() => Context.IsAllowedForTheBorrowerWhoReturned.ShouldBeTrue();
}
