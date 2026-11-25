// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_last_seen_at;

public class when_a_client_subscribes : given.a_client_observer
{
    async Task Because() => await SubscribeClient();

    [Fact] async Task should_record_that_a_client_was_seen() =>
        await _eventStoreNamespaceStorage.Observers.Received(1).SaveLastSeenAt(_observerId, _clock.GetUtcNow(), _observerKey.EventSequenceId);
}
