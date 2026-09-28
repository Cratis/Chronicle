// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_empty_named_tags : given.a_named_tag_append
{
    void Establish()
    {
        _sequences.Append(Arg.Any<Contracts.Sequences.AppendRequest>(), CallContext.Default)
            .Returns(CommandResult<Contracts.Sequences.AppendResponse>.Success(Guid.NewGuid(), new Contracts.Sequences.AppendResponse
            {
                SequenceNumber = 42,
                ConstraintViolations = [],
                Errors = []
            }));
    }

    Task Because() => _eventSequence.AppendWithNamedTags(_sourceId, "one", []);

    [Fact] void should_use_the_original_rpc() => _sequences.Received(1).Append(Arg.Any<Contracts.Sequences.AppendRequest>(), CallContext.Default);
    [Fact] void should_not_use_tagged_rpc() => _singleRequest.ShouldBeNull();
}
