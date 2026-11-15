// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_rotating_a_webhook_secret;

public class with_an_overlap_beyond_the_maximum : given.a_signing_key_ring
{
    Exception _error;

    async Task Establish() => await CreateRing(Secret("active", "webhook", WebhookSigningKeyStatus.Active));

    async Task Because() => _error = await Catch.Exception(() => _ring.RotateWebhookSecret("webhook", WebhookSigningKeyOverlap.Maximum.Value + TimeSpan.FromSeconds(1)));

    [Fact] void should_reject_the_overlap() => _error.ShouldBeOfExactType<InvalidWebhookSigningKeyOverlap>();
    [Fact] void should_not_append_events() => _appended.ShouldBeEmpty();
    [Fact] void should_not_encrypt() => _encrypted.ShouldBeEmpty();
}
