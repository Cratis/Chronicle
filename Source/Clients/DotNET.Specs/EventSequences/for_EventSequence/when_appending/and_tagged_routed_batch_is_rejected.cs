// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_tagged_routed_batch_is_rejected : given.a_named_tag_append
{
    AppendedEventWithResult[] _notifications;

    void Establish()
    {
        _eventSequence.AppendOperations.Subscribe(events => _notifications = events.ToArray());
        _sequences.AppendManyForEventSourcesWithNamedTags(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest>(), CallContext.Default)
            .Returns(CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), new Contracts.Sequences.AppendManyResponse
            {
                SequenceNumbers = [],
                ConstraintViolations = [],
                ConcurrencyViolations = [],
                Errors = ["rejected"]
            }));
    }

    Task Because() => _eventSequence.AppendMany([new EventForEventSourceId(_sourceId, "event") { NamedTags = [new("name", "value")] }]);

    [Fact] void should_notify_with_effective_named_tags() => _notifications.Single().Event.Context.NamedTags.Single().Value.ShouldEqual("value");
    [Fact] void should_notify_with_the_rejection_result() => _notifications.Single().Result.Errors.ShouldNotBeEmpty();
}
