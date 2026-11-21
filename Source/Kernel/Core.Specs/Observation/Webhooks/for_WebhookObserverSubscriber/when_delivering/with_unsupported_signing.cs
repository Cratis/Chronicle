// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_unsupported_signing : given.a_subscriber_with_signing
{
    ObserverSubscriberResult _result;
    protected override WebhookSigningMode Mode => WebhookSigningMode.HmacSha256;

    async Task Because() => _result = await _subscriber.OnNext("partition", _events, new(null));

    [Fact] void should_fail_closed() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_deliver() => _identities.ShouldBeEmpty();
}
