// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_event_type_visibility;

public class and_a_private_event_type_is_appended_many_to_the_outbox : given.an_event_sequence_with_a_registered_visibility
{
    AppendManyResult _result;

    protected override EventSequenceId SequenceId => EventSequenceId.Outbox;
    protected override EventTypeVisibility Visibility => EventTypeVisibility.Private;

    async Task Because() => _result = await _eventSequence.AppendMany(
        _events,
        CorrelationId.New(),
        [],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_reason() => _result.Errors.ShouldContain(AppendError.PrivateEventTypeCannotBeAppendedToOutbox);
    [Fact] void should_not_write_any_event() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
