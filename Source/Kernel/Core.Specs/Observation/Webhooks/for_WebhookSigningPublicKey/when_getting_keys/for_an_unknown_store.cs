// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigningPublicKey.when_getting_keys;

public class for_an_unknown_store : given.a_public_key_query
{
    void Establish() => _storage.HasEventStore("store").Returns(false);
    async Task Because() => _error = await Catch.Exception(Query);

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WebhookSigningUnavailable>();
    [Fact] void should_not_resolve_the_key_ring() => _grainFactory.DidNotReceive().GetGrain<IWebhookSigningKeyRing>(Arg.Any<string>());
    [Fact] void should_not_generate_a_key() => _ring.DidNotReceive().GetSigningKeys();
}
