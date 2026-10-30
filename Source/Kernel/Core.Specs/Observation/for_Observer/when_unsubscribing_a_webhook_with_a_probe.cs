// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_unsubscribing_a_webhook_with_a_probe : given.a_webhook_ready_to_probe
{
    async Task Because()
    {
        await _observer.Unsubscribe();
        await Tick();
    }

    [Fact] void should_remove_probe_authority() => _stateStorage.State.WebhookProbe.ShouldBeNull();
    [Fact] void should_not_deliver_on_a_stale_reminder() => _webhook.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
}
