// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_last_seen_at;

public class when_the_last_client_leaves : given.a_client_observer
{
    async Task Establish() => await SubscribeClient();

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromMinutes(5));
        await _observer.UnsubscribeIfMatchesClient(_client.ConnectionId);
    }

    [Fact] async Task should_record_when_the_client_was_last_seen() =>
        await _eventStoreNamespaceStorage.Observers.Received(1).SaveLastSeenAt(_observerId, _clock.GetUtcNow(), _observerKey.EventSequenceId);
    [Fact] void should_have_recorded_both_the_subscription_and_the_departure() => LastSeenSaves.ShouldEqual(2);
}
