// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_last_seen_at;

public class when_the_removal_is_turned_off : given.a_client_observer
{
    protected override bool RemovalEnabled => false;

    async Task Because()
    {
        await SubscribeClient();
        _clock.Advance(TimeSpan.FromHours(2));
        await _observer.RunWatchdogAsync();
        await _observer.UnsubscribeIfMatchesClient(_client.ConnectionId);
    }

    [Fact] void should_still_record_when_the_client_was_seen() => LastSeenSaves.ShouldEqual(3);
}
