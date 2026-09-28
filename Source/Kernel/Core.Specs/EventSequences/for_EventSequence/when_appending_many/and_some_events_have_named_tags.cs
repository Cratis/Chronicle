// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_some_events_have_named_tags : given.appending_many_events
{
    static readonly NamedTag[] _firstEventTags = [new(new TagName("account"), "one"), new(new TagName("region"), "north")];
    static readonly NamedTag[] _thirdEventTags = [new(new TagName("account"), "three")];

    AppendManyResult _result;
    EventToAppendToStorage[] _eventsSentToStorage;

    void Establish()
    {
        _events =
        [
            EventToAppendFor("source-1") with { NamedTags = _firstEventTags },
            EventToAppendFor("source-2"),
            EventToAppendFor("source-3") with { NamedTags = _thirdEventTags }
        ];

        _eventSequenceStorage.AppendManyWithNamedTags(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(callInfo =>
            {
                _eventsSentToStorage = callInfo.Arg<IEnumerable<EventToAppendToStorage>>().ToArray();
                return Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(AppendedEventsFrom(_eventsSentToStorage)));
            });
    }

    async Task Because() => _result = await _eventSequence.AppendMany(
        _events,
        CorrelationId.New(),
        [],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_use_the_untagged_storage_append() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
    [Fact] void should_send_every_event_to_storage() => string.Join(',', _eventsSentToStorage.Select(_ => _.EventSourceId.Value)).ShouldEqual("source-1,source-2,source-3");
    [Fact] void should_send_the_named_tags_of_the_first_event() => _eventsSentToStorage[0].NamedTags.ShouldContainOnly(_firstEventTags);
    [Fact] void should_send_no_named_tags_for_the_untagged_event() => _eventsSentToStorage[1].NamedTags.ShouldBeEmpty();
    [Fact] void should_send_the_named_tags_of_the_third_event() => _eventsSentToStorage[2].NamedTags.ShouldContainOnly(_thirdEventTags);
}
