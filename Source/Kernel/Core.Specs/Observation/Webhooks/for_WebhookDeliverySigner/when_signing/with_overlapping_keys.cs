// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookDeliverySigner.when_signing;

public class with_overlapping_keys : given.a_delivery_signer
{
    void Establish() => _keys = [Key("old", WebhookSigningKeyStatus.Overlap, Now.AddMinutes(1)), Key("active", WebhookSigningKeyStatus.Active)];
    Task Because() => Sign();
    [Fact] void should_verify_with_the_active_key() => Verifies(PublicKey).ShouldBeTrue();
    [Fact] void should_verify_with_the_overlapping_key() => Verifies(OldPublicKey).ShouldBeTrue();
    [Fact] void should_put_the_active_signature_first() => _signature.Signature.Split(' ')[0].ShouldEqual(WebhookEd25519Signing.Sign("message", 1000, Body, Seed));
    [Fact] void should_emit_two_space_separated_entries() => _signature.Signature.Split(' ').Length.ShouldEqual(2);
}
