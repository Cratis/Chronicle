// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.InMemory.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueEventTypesConstraintsStorage.given;

public class a_unique_event_types_constraints_storage : Specification
{
    protected const string ConstraintNameValue = "loan-open";
    protected static readonly EventType _checkedOutEventType = new("LoanCheckedOut", EventTypeGeneration.First);
    protected static readonly EventType _returnedEventType = new("LoanReturned", EventTypeGeneration.First);
    protected static readonly EventType _writtenOffEventType = new("LoanWrittenOff", EventTypeGeneration.First);
    protected static readonly EventSourceId _borrower = "borrower";
    protected static readonly EventSourceId _anotherBorrower = "another-borrower";

    protected EventSequenceStorage _eventSequenceStorage;
    protected UniqueEventTypesConstraintsStorage _storage;

    void Establish()
    {
        _eventSequenceStorage = new(
            new EventStoreName("event-store"),
            EventStoreNamespaceName.Default,
            EventSequenceId.Log);

        _storage = new(_eventSequenceStorage);
    }

    protected static UniqueEventTypeConstraintDefinition DefinitionReleasedByReturn =>
        new(ConstraintNameValue, [_checkedOutEventType.Id], [_returnedEventType.Id]);

    protected static UniqueEventTypeConstraintDefinition DefinitionWithoutRemovalEvent =>
        new(ConstraintNameValue, [_checkedOutEventType.Id]);

    protected static UniqueEventTypeConstraintDefinition DefinitionReleasedByReturnOrWriteOff =>
        new(ConstraintNameValue, [_checkedOutEventType.Id], [_returnedEventType.Id, _writtenOffEventType.Id]);

    protected Task Append(ulong sequenceNumber, EventType eventType, EventSourceId eventSourceId) =>
        _eventSequenceStorage.Append(
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
