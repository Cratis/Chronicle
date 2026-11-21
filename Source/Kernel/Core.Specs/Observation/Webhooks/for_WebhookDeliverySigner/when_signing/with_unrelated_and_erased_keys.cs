// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookDeliverySigner.when_signing;

public class with_unrelated_and_erased_keys : given.a_delivery_signer
{
    void Establish() => _keys =
    [
        Key("active", WebhookSigningKeyStatus.Active),
        Key("erased", WebhookSigningKeyStatus.Overlap, Now.AddMinutes(1)) with { EncryptedSecret = string.Empty },
        Key("webhook", WebhookSigningKeyStatus.Active) with { Webhook = new("webhook"), Algorithm = WebhookSigningAlgorithm.HmacSha256 }
    ];

    Task Because() => Sign();
    [Fact] void should_sign_only_with_the_store_key() => _signature.Signature.ShouldEqual(WebhookEd25519Signing.Sign("message", 1000, Body, Seed));
    [Fact] void should_not_decrypt_erased_keys() => _encryption.DidNotReceive().Decrypt(string.Empty);
    [Fact] void should_not_decrypt_per_webhook_secrets() => _encryption.DidNotReceive().Decrypt("encrypted-webhook");
}
