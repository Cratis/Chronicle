// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookDeliverySigner.when_signing;

public class with_an_active_key : given.a_delivery_signer
{
    Task Because() => Sign();
    [Fact] void should_succeed() => _error.ShouldBeNull();
    [Fact] void should_sign_the_exact_non_ascii_body() => Verifies(PublicKey).ShouldBeTrue();
    [Fact] void should_emit_only_an_ed25519_entry() => _signature.Signature.StartsWith("v1a,", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_identify_the_active_key() => _signature.KeyId.ShouldEqual("active");
    [Fact] void should_emit_invariant_unix_seconds() => _signature.Timestamp.ShouldEqual("1000");
    [Fact] void should_not_expose_the_signature_in_diagnostics() => _signature.ToString().ShouldEqual(nameof(WebhookDeliverySignature));
    [Fact] void should_use_the_explicit_store() => _grainFactory.Received(1).GetGrain<IWebhookSigningKeyRing>("store");
}
