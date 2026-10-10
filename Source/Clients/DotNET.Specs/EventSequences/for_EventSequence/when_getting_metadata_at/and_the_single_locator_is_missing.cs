// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_metadata_at;

public class and_the_single_locator_is_missing : given.an_event_sequence
{
    EventMetadata _result;

    void Establish() => _sequences.MetadataAt(Arg.Any<Contracts.Sequences.MetadataAtRequest>(), Arg.Any<CallContext>()).Returns(QueryResult<IEnumerable<Contracts.Sequences.EventMetadataResponse>>.Success(Guid.NewGuid(), []));

    async Task Because() => _result = await _eventSequence.GetMetadataAt((EventSequenceNumber)42UL);

    [Fact] void should_return_null() => _result.ShouldBeNull();
}
