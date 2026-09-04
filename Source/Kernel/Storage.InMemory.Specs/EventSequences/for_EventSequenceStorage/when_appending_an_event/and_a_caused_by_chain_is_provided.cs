// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.InMemory.Identities;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_an_event;

/// <summary>
/// Appending with a resolved caused-by chain must record the actual identity, not the hardcoded
/// system identity - the exact regression in #3928.
/// </summary>
public class and_a_caused_by_chain_is_provided : Specification
{
    static readonly EventSourceId _eventSourceId = "some-source";
    static readonly EventType _eventType = new("some-event-type", EventTypeGeneration.First);
    static readonly Identity _causedBy = new("issuetriage", "Scout", "issuetriage");

    IdentityStorage _identityStorage;
    EventSequenceStorage _storage;
    AppendedEvent _appended;

    void Establish()
    {
        _identityStorage = new IdentityStorage();
        _storage = new EventSequenceStorage(
            new EventStoreName("event-store"),
            EventStoreNamespaceName.Default,
            EventSequenceId.Log,
            _identityStorage);
    }

    async Task Because()
    {
        var chain = await _identityStorage.GetFor(_causedBy);

        var result = await _storage.Append(
            EventSequenceNumber.First,
            EventSourceType.Default,
            _eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            CorrelationId.New(),
            [],
            chain,
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { { EventTypeGeneration.First, new ExpandoObject() } },
            new Dictionary<EventTypeGeneration, EventHash>());

        _appended = result.AsT0;
    }

    [Fact] void should_record_the_actual_caused_by_subject() => _appended.Context.CausedBy.Subject.ShouldEqual(_causedBy.Subject);
    [Fact] void should_record_the_actual_caused_by_name() => _appended.Context.CausedBy.Name.ShouldEqual(_causedBy.Name);
    [Fact] void should_not_record_the_system_identity() => _appended.Context.CausedBy.ShouldNotEqual(Identity.System);
}
