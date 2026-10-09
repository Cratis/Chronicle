// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_a_publication;

public class and_the_identity_is_reused_for_a_different_intent : given.an_event_sequence_with_publication_storage
{
    AppendResult _result;

    void Establish() => _publicationStorage.TryGetPublication(Arg.Any<EventPublication>())
        .Returns<Task<Option<EventPublicationReceipt>>>(_ => throw new EventPublicationConflict());

    async Task Because() => _result = await Publish();

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_typed_publication_conflict() => _result.Errors.ShouldContainOnly(AppendError.EventPublicationConflict);
    [Fact] void should_not_enqueue_anything() => _appendedEventsQueues.DidNotReceive().Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
}
