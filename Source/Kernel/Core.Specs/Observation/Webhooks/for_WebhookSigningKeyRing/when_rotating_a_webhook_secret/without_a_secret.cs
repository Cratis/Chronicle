// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_rotating_a_webhook_secret;

public class without_a_secret : given.a_signing_key_ring
{
    WebhookSigningKey _result;

    async Task Establish() => await CreateRing();

    async Task Because() => _result = await _ring.RotateWebhookSecret("webhook", WebhookSigningKeyOverlap.Default);

    [Fact] void should_append_only_generated() => _appended.Select(entry => entry.Event).ShouldEqual([new WebhookSigningKeyGenerated(_result.KeyId, WebhookSigningAlgorithm.HmacSha256, string.Empty, "webhook")]);
    [Fact] void should_return_an_active_webhook_secret() => (_result.Algorithm, _result.Status, _result.Webhook!.Value, _result.PublicKey).ShouldEqual((WebhookSigningAlgorithm.HmacSha256, WebhookSigningKeyStatus.Active, "webhook", string.Empty));
    [Fact] void should_encrypt_a_standard_webhooks_secret() => WebhookSigningKeys.TryParseHmacSecret(_encrypted.Single(), out _).ShouldBeTrue();
    [Fact] void should_generate_32_random_bytes()
    {
        WebhookSigningKeys.TryParseHmacSecret(_encrypted.Single(), out var bytes).ShouldBeTrue();
        bytes.Length.ShouldEqual(32);
    }
    [Fact] void should_return_only_ciphertext() => (_result.EncryptedSecret.Value == "enc:" + _encrypted.Single()).ShouldBeTrue();
    [Fact] void should_persist_before_returning() => State.Keys.Any(key => key.KeyId == _result.KeyId).ShouldBeTrue();
}
