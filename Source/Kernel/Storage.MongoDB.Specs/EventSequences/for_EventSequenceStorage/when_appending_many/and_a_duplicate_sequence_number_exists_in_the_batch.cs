// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_many;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_duplicate_sequence_number_exists_in_the_batch(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_event_sequence_storage(fixture)
{
    Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber> _result;
    DuplicateEventSequenceNumber _duplicate;

    async Task Establish() => await _storage.AppendMany([EventAt(EventSequenceNumber.First)]);

    async Task Because()
    {
        _result = await _storage.AppendMany([EventAt(EventSequenceNumber.First), EventAt(EventSequenceNumber.First + 1)]);
        _result.TryGetError(out _duplicate);
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_recover_by_surfacing_a_duplicate_sequence_number() => _duplicate.ShouldNotBeNull();
    [Fact] void should_resolve_the_next_available_sequence_number_from_the_tail() => _duplicate.NextAvailableSequenceNumber.ShouldEqual((EventSequenceNumber)1);
}
