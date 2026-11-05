// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_rotating;

public class with_a_negative_overlap : given.a_signing_key_ring
{
    Exception _error;

    async Task Establish() => await CreateRing(Key("active", WebhookSigningKeyStatus.Active));

    async Task Because() => _error = await Catch.Exception(() => _ring.Rotate(TimeSpan.FromSeconds(-1)));

    [Fact] void should_throw_invalid_overlap() => _error.ShouldBeOfExactType<InvalidWebhookSigningKeyOverlap>();
    [Fact] void should_append_nothing() => _appended.ShouldBeEmpty();
}
