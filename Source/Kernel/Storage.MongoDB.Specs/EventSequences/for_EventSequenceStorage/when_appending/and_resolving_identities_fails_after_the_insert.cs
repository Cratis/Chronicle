// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_resolving_identities_fails_after_the_insert(ReplicaSetMongoDBFixture fixture) : given.a_storage_for_appended_generation(fixture)
{
    Exception _error;
    Event _stored;
    void Establish() => _identityStorage.GetFor(Arg.Any<IEnumerable<IdentityId>>()).ThrowsAsync(new given.AcknowledgmentLost());
    async Task Because()
    {
        var e = GenerationalEvent(EventSequenceNumber.First, 1);
        _error = await Catch.Exception(() => _storage.Append(e.SequenceNumber, e.EventSourceType, e.EventSourceId, e.EventStreamType, e.EventStreamId, e.EventType, e.CorrelationId, e.Causation, e.CausedByChain, e.Tags, e.Occurred, e.GenerationalContent, e.ContentHashes));
        _stored = await Stored(e.SequenceNumber);
    }
    [Fact] void should_throw_the_original_error() => _error.ShouldBeOfExactType<given.AcknowledgmentLost>();
    [Fact] void should_not_claim_the_write_was_aborted() => (_error is EventsNotCommitted).ShouldBeFalse();
    [Fact] void should_have_stored_the_event() => _stored.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
}
