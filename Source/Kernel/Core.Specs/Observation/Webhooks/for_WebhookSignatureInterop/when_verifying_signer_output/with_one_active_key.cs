// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

using ClientResult = Client::Cratis.Chronicle.Webhooks.WebhookSignatureVerificationResult;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureInterop.when_verifying_signer_output;

public class with_one_active_key : given.a_signer_and_client_verifier
{
    ClientResult _valid;
    ClientResult _tampered;

    async Task Because()
    {
        await Sign();
        _valid = VerifyWith(EncodedPublicKey(PublicKey));
        _tampered = VerifyWith(EncodedPublicKey(PublicKey), [.. Body, 0]);
    }

    [Fact] void should_verify_with_the_client_verifier() => _valid.IsValid.ShouldBeTrue();
    [Fact] void should_not_verify_a_different_body() => _tampered.IsValid.ShouldBeFalse();
}
