// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

using Cratis.Chronicle.Observation.Webhooks.Signing;
using Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookDeliverySigner.given;
using ClientResult = Client::Cratis.Chronicle.Webhooks.WebhookSignatureVerificationResult;
using ClientVerifier = Client::Cratis.Chronicle.Webhooks.WebhookSignatureVerifier;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureInterop.given;

/// <summary>
/// Signs with the kernel's real delivery signer and verifies with the client's public verifier, which share no code,
/// so a symmetric bug in both is caught by the literal vector in the client specs and the RFC 8032 specs here.
/// </summary>
public class a_signer_and_client_verifier : a_delivery_signer
{
    protected static string EncodedPublicKey(byte[] publicKey) => WebhookSigningKeys.EncodePublicKey(publicKey);

    protected ClientResult VerifyWith(string publicKey, byte[]? body = null) =>
        new ClientVerifier([publicKey], timeProvider: new FixedTime()).Verify(_messageId, "1000", _signature.Signature, body ?? Body);

    sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.FromUnixTimeSeconds(1000);
    }
}
