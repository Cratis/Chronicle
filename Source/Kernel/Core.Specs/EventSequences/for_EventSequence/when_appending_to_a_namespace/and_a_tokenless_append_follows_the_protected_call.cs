// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_to_a_namespace;

public class and_a_tokenless_append_follows_the_protected_call : given.a_namespace_bound_sequence
{
    AppendResult _result = null!;
    int _before;
    int _afterProtected;

    void Establish() => _before = TokenlessAppendCount;

    async Task Because()
    {
        await AppendProtected();
        _afterProtected = TokenlessAppendCount;
        var @event = _events[0];
        _result = await _eventSequence.Append(@event.EventSourceType, @event.EventSourceId, @event.eventStreamType, @event.eventStreamId, @event.EventType, @event.Content, _correlationId, [], Identity.System, @event.Tags, ConcurrencyScope.None, @event.Occurred, @event.Subject, @event.NamedTags, @event.EventSource);
    }

    [Fact] void should_not_count_setup_calls_as_tokenless_appends() => _before.ShouldEqual(0);
    [Fact] void should_not_count_the_fenced_append_as_tokenless() => _afterProtected.ShouldEqual(0);
    [Fact] void should_still_detect_a_real_tokenless_append() => TokenlessAppendCount.ShouldEqual(1);
    [Fact] void should_leave_the_legacy_call_unbound() => _contexts.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_legacy_append_result() => _result.IsSuccess.ShouldBeTrue();
}
