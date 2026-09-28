// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class a_named_tagged_batch_with_distinct_causation : given.a_named_tag_append
{
    Causation _ambient;
    Causation _first;
    Causation _second;
    EventContext[] _observedContexts;

    void Establish()
    {
        _ambient = new(DateTimeOffset.UnixEpoch, "ambient", new Dictionary<string, string>());
        _first = new(DateTimeOffset.UnixEpoch, "first", new Dictionary<string, string>());
        _second = new(DateTimeOffset.UnixEpoch, "second", new Dictionary<string, string>());
        _causationManager.GetCurrentChain().Returns(ImmutableList.Create(_ambient));
        _eventSequence.AppendOperations.Subscribe(events => _observedContexts = events.Select(_ => _.Event.Context).ToArray());
    }

    async Task Because() => await _eventSequence.AppendManyWithNamedTags(
        [new(_sourceId, "first", _first), new(_sourceId, "second", _second), new(_sourceId, "third")],
        [new("account", "one")]);

    [Fact] void should_send_ambient_causation_for_batch() => _routedRequest.Causation.ToClient().ShouldEqual([_ambient]);
    [Fact] void should_send_first_events_causation() => _routedRequest.Events.First().Causation.ToClient().ShouldEqual([_ambient, _first]);
    [Fact] void should_send_second_events_causation() => _routedRequest.Events.Skip(1).First().Causation.ToClient().ShouldEqual([_ambient, _second]);
    [Fact] void should_let_third_event_inherit_batch_causation() => _routedRequest.Events.Last().Causation.ShouldBeNull();
    [Fact] void should_keep_named_tags_on_all_events() => _routedRequest.Events.All(_ => _.NamedTags.Single().Value == "one").ShouldBeTrue();
    [Fact] void should_notify_event_specific_causation() => _observedContexts.Select(_ => string.Join(',', _.Causation.Select(cause => cause.Type.Name))).ShouldEqual(["ambient,first", "ambient,second", "ambient"]);
}
