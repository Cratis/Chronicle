// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_metadata_at;

public class and_a_batch_is_requested : given.an_event_sequence
{
    IImmutableDictionary<EventSequenceNumber, EventMetadata> _result;
    Contracts.Sequences.MetadataAtRequest _request;

    void Establish() => _sequences.MetadataAt(Arg.Any<Contracts.Sequences.MetadataAtRequest>(), Arg.Any<CallContext>()).Returns(call =>
    {
        _request = call.Arg<Contracts.Sequences.MetadataAtRequest>();
        return QueryResult<IEnumerable<Contracts.Sequences.EventMetadataResponse>>.Success(Guid.NewGuid(), [new()
        {
            SequenceNumber = 42,
            EventTypeId = "type",
            EventSourceId = "source",
            EventSourceType = "Source",
            EventStreamType = "Stream",
            EventStreamId = "stream-id",
            Occurred = DateTimeOffset.UtcNow,
            CausedBy = new Contracts.Sequences.ResolvedIdentity
            {
                Subject = "person",
                Name = null,
                UserName = "username",
                Resolution = Contracts.Sequences.IdentityResolution.NameUnavailable
            }
        }]);
    });

    async Task Because() => _result = await _eventSequence.GetMetadataAt([42UL, 42UL, 1000UL]);

    [Fact] void should_send_distinct_locators() => _request.SequenceNumbers.ShouldContainOnly(42UL, 1000UL);
    [Fact] void should_key_metadata_by_sequence_number() => _result.Keys.ShouldContainOnly((EventSequenceNumber)42UL);
    [Fact] void should_preserve_resolution() => _result[(EventSequenceNumber)42UL].CausedBy.Resolution.ShouldEqual(IdentityResolution.NameUnavailable);
    [Fact] void should_not_fall_back_to_subject_for_name() => _result[(EventSequenceNumber)42UL].CausedBy.Name.ShouldBeNull();
    [Fact] void should_not_deserialize_any_content() => _eventSerializer.ReceivedCalls().ShouldBeEmpty();
}
