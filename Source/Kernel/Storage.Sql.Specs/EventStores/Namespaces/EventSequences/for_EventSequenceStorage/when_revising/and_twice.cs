// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_revising;

public class and_twice : given.an_event_sequence_storage
{
    static readonly EventHash _originalHash = new("original-hash");
    static readonly EventHash _firstRevisionHash = new("first-revision-hash");
    static readonly EventHash _secondRevisionHash = new("second-revision-hash");
    AppendedEvent _afterFirstRevision;
    AppendedEvent _afterSecondRevision;

    async Task Establish()
    {
        await Append(EventSequenceNumber.First, new ExpandoObject(), _originalHash);
    }

    async Task Because()
    {
        await _storage.Revise(EventSequenceNumber.First, _eventType, CorrelationId.New(), [], [], DateTimeOffset.UtcNow, new ExpandoObject(), _firstRevisionHash);
        _afterFirstRevision = await _storage.GetEventAt(EventSequenceNumber.First);
        await _storage.Revise(EventSequenceNumber.First, _eventType, CorrelationId.New(), [], [], DateTimeOffset.UtcNow, new ExpandoObject(), _secondRevisionHash);
        _afterSecondRevision = await _storage.GetEventAt(EventSequenceNumber.First);
    }

    [Fact] void should_persist_the_first_revised_hash() => _afterFirstRevision.Context.Hash.ShouldEqual(_firstRevisionHash);
    [Fact] void should_persist_the_second_revised_hash() => _afterSecondRevision.Context.Hash.ShouldEqual(_secondRevisionHash);
    [Fact] void should_not_return_the_original_hash_after_the_second_revision() => _afterSecondRevision.Context.Hash.ShouldNotEqual(_originalHash);
}
