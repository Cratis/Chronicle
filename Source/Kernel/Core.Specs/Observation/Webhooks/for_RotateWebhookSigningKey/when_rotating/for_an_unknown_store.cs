// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_RotateWebhookSigningKey.when_rotating;

public class for_an_unknown_store : given.a_key_rotation
{
    void Establish() => _storage.HasEventStore("store").Returns(false);
    async Task Because() => _error = await Catch.Exception(() => _command.Handle(_grainFactory, _storage));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WebhookSigningUnavailable>();
    [Fact] void should_not_resolve_the_key_ring() => _grainFactory.DidNotReceive().GetGrain<IWebhookSigningKeyRing>(Arg.Any<string>());
    [Fact] void should_not_rotate() => _ring.DidNotReceive().Rotate(Arg.Any<WebhookSigningKeyOverlap>());
}
