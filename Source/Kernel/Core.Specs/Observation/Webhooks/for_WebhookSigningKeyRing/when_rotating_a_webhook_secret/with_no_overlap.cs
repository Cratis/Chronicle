// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_rotating_a_webhook_secret;

public class with_no_overlap : given.a_signing_key_ring
{
    async Task Establish() => await CreateRing(Secret("active", "webhook", WebhookSigningKeyStatus.Active));

    async Task Because() => await _ring.RotateWebhookSecret("webhook", TimeSpan.Zero);

    [Fact] void should_supersede_retire_then_generate() => _appended.Select(entry => entry.Event.GetType()).ShouldEqual([typeof(WebhookSigningKeySuperseded), typeof(WebhookSigningKeyRetired), typeof(WebhookSigningKeyGenerated)]);
    [Fact] void should_retire_immediately() => (State.Keys.Single(key => key.KeyId.Value == "active").Status, State.Keys.Single(key => key.KeyId.Value == "active").RetiredAt).ShouldEqual((WebhookSigningKeyStatus.Retired, (DateTimeOffset?)Now));
    [Fact] void should_erase_the_previous_ciphertext() => (State.Keys.Single(key => key.KeyId.Value == "active").EncryptedSecret == EncryptedWebhookSigningSecret.Empty).ShouldBeTrue();
}
