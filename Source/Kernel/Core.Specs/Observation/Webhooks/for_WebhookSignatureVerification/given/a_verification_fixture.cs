// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureVerification.given;

public class a_verification_fixture : Specification
{
    protected const string MessageId = "msg_fixture";
    protected const long Timestamp = 1700000000;

    protected byte[] _oldSecret;
    protected byte[] _newSecret;
    protected byte[] _payload;
    protected string _oldSignature;
    protected string _newSignature;

    void Establish()
    {
        // Public test material, not operational keys.
        _oldSecret = Convert.FromHexString("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F");
        _newSecret = Convert.FromHexString("1F1E1D1C1B1A191817161514131211100F0E0D0C0B0A09080706050403020100");
        _payload = "{\"ok\":true}"u8.ToArray();
        _oldSignature = WebhookSigning.Sign(MessageId, Timestamp, _payload, _oldSecret);
        _newSignature = WebhookSigning.Sign(MessageId, Timestamp, _payload, _newSecret);
    }

    protected bool Verify(string? signatureHeader, string? keyId, params byte[][] trusted) =>
        WebhookSignatureVerification.Verify(MessageId, Timestamp, _payload, signatureHeader, trusted.Select(_ => (ReadOnlyMemory<byte>)_), keyId);
}
