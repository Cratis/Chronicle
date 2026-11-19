// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Signing;
using ClientResult = Client::Cratis.Chronicle.Webhooks.WebhookSignatureVerificationResult;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureInterop.when_verifying_signer_output;

public class with_overlapping_keys : given.a_signer_and_client_verifier
{
    ClientResult _active;
    ClientResult _old;
    ClientResult _unrelated;

    void Establish() => _keys = [Key("old", WebhookSigningKeyStatus.Overlap, Now.AddMinutes(1)), Key("active", WebhookSigningKeyStatus.Active)];

    async Task Because()
    {
        await Sign();
        _active = VerifyWith(EncodedPublicKey(PublicKey));
        _old = VerifyWith(EncodedPublicKey(OldPublicKey));
        _unrelated = VerifyWith(EncodedPublicKey(WebhookEd25519Signing.DerivePublicKey(Enumerable.Repeat((byte)9, 32).ToArray())));
    }

    [Fact] void should_verify_with_only_the_active_key_configured() => _active.IsValid.ShouldBeTrue();
    [Fact] void should_verify_with_only_the_overlapping_key_configured() => _old.IsValid.ShouldBeTrue();
    [Fact] void should_not_verify_with_an_unrelated_key() => _unrelated.IsValid.ShouldBeFalse();
}
