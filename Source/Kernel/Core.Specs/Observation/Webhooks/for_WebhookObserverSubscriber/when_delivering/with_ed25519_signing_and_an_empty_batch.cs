// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_ed25519_signing_and_an_empty_batch : given.a_subscriber_with_signing
{
    ObserverSubscriberResult _result;

    async Task Because() => _result = await _subscriber.OnNext("partition", [], new(null));

    [Fact] void should_not_deliver_anything() => _identities.ShouldBeEmpty();
    [Fact] void should_accept_the_batch() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}
