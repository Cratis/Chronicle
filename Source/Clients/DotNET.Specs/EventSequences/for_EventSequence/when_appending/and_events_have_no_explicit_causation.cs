// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_events_have_no_explicit_causation : given.an_event_sequence_with_metadata
{
    Causation _ambient;

    void Establish()
    {
        _ambient = new(DateTimeOffset.UnixEpoch, "ambient", new Dictionary<string, string>());
        _causationManager.GetCurrentChain().Returns(ImmutableList.Create(_ambient));
    }

    async Task Because() => await _eventSequence.AppendMany([new(_source, "first"), new(_source, "second")]);

    [Fact] void should_send_the_ambient_chain_for_the_batch() => _batchRequest.Causation.ToClient().ShouldEqual([_ambient]);
    [Fact] void should_not_override_the_first_events_causation() => _batchRequest.Events.First().Causation.ShouldBeNull();
    [Fact] void should_not_override_the_second_events_causation() => _batchRequest.Events.Last().Causation.ShouldBeNull();
    [Fact] void should_notify_with_the_ambient_chain() => _notifications.Select(_ => _.Event.Context.Causation.Single()).ShouldEqual([_ambient, _ambient]);
}
