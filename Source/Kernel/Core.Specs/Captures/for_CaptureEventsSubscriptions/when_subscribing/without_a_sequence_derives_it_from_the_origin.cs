// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriptions.when_subscribing;

public class without_a_sequence_derives_it_from_the_origin : given.a_capture_events_subscriptions
{
    void Establish() => _definition = _definition with { Source = _definition.Source with { Sequence = null } };

    async Task Because() => await _subscriptions.Subscribe(_eventStore, _definition);

    [Fact] void should_observe_the_inbox_of_the_origin() => _keys.Values.ShouldEachConformTo(key => key.EventSequenceId.Value == "inbox-fulfillment");
}
