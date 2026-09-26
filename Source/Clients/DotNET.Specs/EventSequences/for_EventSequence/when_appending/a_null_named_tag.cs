// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class a_null_named_tag : given.a_named_tag_append
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.Append(_sourceId, "event", [null!]));

    [Fact] void should_reject_the_invalid_tag_by_name() => _error.ShouldBeOfExactType<InvalidNamedTag>();
    [Fact] void should_not_send_the_invalid_tag() => _sequences.DidNotReceive().AppendWithNamedTags(Arg.Any<Contracts.Sequences.AppendWithNamedTagsRequest>(), CallContext.Default);
}
