// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_tagged_append_is_rejected : given.a_named_tag_append
{
    AppendedEventWithResult _notification;

    void Establish()
    {
        _eventSequence.AppendOperations.Subscribe(events => _notification = events.Single());
        _sequences.AppendWithNamedTags(Arg.Any<Contracts.Sequences.AppendWithNamedTagsRequest>(), CallContext.Default)
            .Returns(CommandResult<Contracts.Sequences.AppendResponse>.Success(Guid.NewGuid(), new Contracts.Sequences.AppendResponse
            {
                SequenceNumber = 42,
                ConstraintViolations = [],
                Errors = ["rejected"]
            }));
    }

    Task Because() => _eventSequence.Append(_sourceId, "event", [new("name", "value")]);

    [Fact] void should_notify_with_effective_named_tags() => _notification.Event.Context.NamedTags.Single().Value.ShouldEqual("value");
    [Fact] void should_notify_with_the_rejection_result() => _notification.Result.Errors.ShouldNotBeEmpty();
}
