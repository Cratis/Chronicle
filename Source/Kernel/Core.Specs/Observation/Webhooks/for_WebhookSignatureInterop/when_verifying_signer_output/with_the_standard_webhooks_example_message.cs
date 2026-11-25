// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

using System.Text;
using Cratis.Chronicle.Observation.Webhooks.Signing;
using ClientResult = Client::Cratis.Chronicle.Webhooks.WebhookSignatureVerificationResult;
using ClientVerifier = Client::Cratis.Chronicle.Webhooks.WebhookSignatureVerifier;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureInterop.when_verifying_signer_output;

/// <summary>
/// The literal signature was computed independently of BouncyCastle (Node.js crypto over the RFC 8032 TEST 1 seed). The
/// Standard Webhooks repository publishes only HMAC vectors, so there is no official <c language="text">v1a</c> vector.
/// </summary>
public class with_the_standard_webhooks_example_message : Specification
{
    const string Literal = "v1a,vt2bMAvc9tv3Zyi9oJthvdIxGUYHaZkv0BHtTerC/QIAZLa04snS/FuMvM1IRJgJPB38DB1gZTs/hkv61S9eBw==";
    const string Id = "msg_2KWPBgLlAfxdpx2AI54pPJ85f4W";

    static readonly byte[] _seed = Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60");
    static readonly byte[] _payload = Encoding.UTF8.GetBytes("{\"test\": 2432232314}");

    string _signature;
    ClientResult _result;

    void Because()
    {
        _signature = WebhookEd25519Signing.Sign(Id, 1614265330, _payload, _seed);
        _result = new ClientVerifier([WebhookSigningKeys.EncodePublicKey(WebhookEd25519Signing.DerivePublicKey(_seed))], timeProvider: new FixedTime())
            .Verify(Id, "1614265330", _signature, _payload);
    }

    [Fact] void should_produce_the_literal_signature_byte_for_byte() => _signature.ShouldEqual(Literal);
    [Fact] void should_verify_with_the_client_verifier() => _result.IsValid.ShouldBeTrue();

    sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.FromUnixTimeSeconds(1614265330);
    }
}
