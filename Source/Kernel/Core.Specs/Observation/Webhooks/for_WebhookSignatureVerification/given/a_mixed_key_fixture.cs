// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureVerification.given;

public class a_mixed_key_fixture : a_verification_fixture
{
    protected byte[] _oldPublicKey;
    protected byte[] _newPublicKey;
    protected string _oldEd25519Signature;
    protected string _newEd25519Signature;

    void Establish()
    {
        // RFC 8032 section 7.1 TEST 1 and TEST 3. Public test material, not operational keys.
        var oldSeed = Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60");
        var newSeed = Convert.FromHexString("c5aa8df43f9f837bedb7442f31dcb7b166d38535076f094b85ce3a2e0b4458f7");
        _oldPublicKey = WebhookEd25519Signing.DerivePublicKey(oldSeed);
        _newPublicKey = WebhookEd25519Signing.DerivePublicKey(newSeed);
        _oldEd25519Signature = WebhookEd25519Signing.Sign(MessageId, Timestamp, _payload, oldSeed);
        _newEd25519Signature = WebhookEd25519Signing.Sign(MessageId, Timestamp, _payload, newSeed);
    }

    protected bool VerifyMixed(string? signatureHeader, params byte[][] trustedPublicKeys) =>
        WebhookSignatureVerification.Verify(MessageId, Timestamp, _payload, signatureHeader, [], trustedPublicKeys.Select(_ => (ReadOnlyMemory<byte>)_), "advisory-only");
}
