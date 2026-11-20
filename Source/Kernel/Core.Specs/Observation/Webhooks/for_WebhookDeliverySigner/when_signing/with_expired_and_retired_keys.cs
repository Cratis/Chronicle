// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookDeliverySigner.when_signing;

public class with_expired_and_retired_keys : given.a_delivery_signer
{
    void Establish() => _keys = [Key("active", WebhookSigningKeyStatus.Active), Key("old", WebhookSigningKeyStatus.Overlap, Now), Key("retired", WebhookSigningKeyStatus.Retired)];
    Task Because() => Sign();
    [Fact] void should_sign_only_with_the_active_key() => _signature.Signature.ShouldEqual(WebhookEd25519Signing.Sign("message", 1000, Body, Seed));
    [Fact] void should_not_decrypt_expired_keys() => _encryption.DidNotReceive().Decrypt("encrypted-old");
    [Fact] void should_not_decrypt_retired_keys() => _encryption.DidNotReceive().Decrypt("encrypted-retired");
}
