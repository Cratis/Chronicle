// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class without_signing : given.a_subscriber_with_signing
{
    protected override WebhookSigningMode Mode => WebhookSigningMode.None;
    async Task Because() => await _subscriber.OnNext("partition", _events, new(null));
    [Fact] void should_request_an_unsigned_delivery() => _identities.Single().ShouldBeNull();
}
