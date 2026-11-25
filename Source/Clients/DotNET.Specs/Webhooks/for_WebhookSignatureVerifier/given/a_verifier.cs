// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Webhooks;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.given;

/// <summary>
/// Fixture around one literal vector. The seed and public key are RFC 8032 section 7.1 TEST 1; the identifier, timestamp and
/// payload are the Standard Webhooks specification's example values. The expected signature was computed once with an
/// implementation independent of BouncyCastle (Node.js crypto, which uses OpenSSL: <c language="text">crypto.sign(null, message, key)</c> over
/// the PKCS#8-wrapped seed) and pasted as a literal. The Standard Webhooks repository publishes only HMAC <c language="text">v1</c> vectors, so no
/// official <c language="text">v1a</c> vector exists; the reference is the specification's framing plus the RFC 8032 primitive.
/// </summary>
public class a_verifier : Specification
{
    protected const string Id = "msg_2KWPBgLlAfxdpx2AI54pPJ85f4W";
    protected const string Timestamp = "1614265330";
    protected const string KnownSignature = "v1a,vt2bMAvc9tv3Zyi9oJthvdIxGUYHaZkv0BHtTerC/QIAZLa04snS/FuMvM1IRJgJPB38DB1gZTs/hkv61S9eBw==";
    protected const long UnixNow = 1614265330;

    protected static readonly byte[] Seed = Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60");
    protected static readonly string PublicKey = "whpk_" + Convert.ToBase64String(Convert.FromHexString("d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a"));
    protected static readonly byte[] OtherSeed = Enumerable.Repeat((byte)7, 32).ToArray();
    protected static readonly string OtherPublicKey = "whpk_" + Convert.ToBase64String(new Ed25519PrivateKeyParameters(OtherSeed).GeneratePublicKey().GetEncoded());
    protected static readonly byte[] Payload = Encoding.UTF8.GetBytes("{\"test\": 2432232314}");

    protected FixedTimeProvider _time;
    protected WebhookSignatureVerifier _verifier;
    protected WebhookSignatureVerificationResult _result;

    protected static string Sign(byte[] seed, string id, string timestamp, byte[] payload)
    {
        var signer = new Ed25519Signer();
        signer.Init(true, new Ed25519PrivateKeyParameters(seed));
        var message = Encoding.UTF8.GetBytes($"{id}.{timestamp}.").Concat(payload).ToArray();
        signer.BlockUpdate(message, 0, message.Length);
        return "v1a," + Convert.ToBase64String(signer.GenerateSignature());
    }

    protected WebhookSignatureVerificationResult Verify(string? id, string? timestamp, string? signature, byte[]? payload = null) =>
        _verifier.Verify(id, timestamp, signature, payload ?? Payload);

    void Establish()
    {
        _time = new(UnixNow);
        _verifier = new([PublicKey], timeProvider: _time);
    }

    protected sealed class FixedTimeProvider(long unixSeconds) : TimeProvider
    {
        long _unixSeconds = unixSeconds;

        public void Advance(long seconds) => _unixSeconds += seconds;

        public override DateTimeOffset GetUtcNow() => DateTimeOffset.FromUnixTimeSeconds(_unixSeconds);
    }
}
