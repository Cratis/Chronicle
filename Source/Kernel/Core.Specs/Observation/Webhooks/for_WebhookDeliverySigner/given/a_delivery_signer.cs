// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookDeliverySigner.given;

public class a_delivery_signer : Specification
{
    protected static readonly byte[] Seed = Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60");
    protected static readonly byte[] PublicKey = WebhookEd25519Signing.DerivePublicKey(Seed);
    protected static readonly byte[] OldSeed = Enumerable.Repeat((byte)7, 32).ToArray();
    protected static readonly byte[] OldPublicKey = WebhookEd25519Signing.DerivePublicKey(OldSeed);
    protected static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1000);
    protected static readonly byte[] Body = System.Text.Encoding.UTF8.GetBytes("{\"name\":\"世界 café\"}");
    protected IGrainFactory _grainFactory;
    protected IWebhookSigningKeyRing _ring;
    protected IEncryption _encryption;
    protected WebhookDeliverySigner _signer;
    protected WebhookSigningKey[] _keys;
    protected WebhookDeliverySignature _signature;
    protected string _messageId = "message";
    protected Exception? _error;

    protected static WebhookSigningKey Key(string id, WebhookSigningKeyStatus status, DateTimeOffset? expires = null) =>
        new(id, WebhookSigningAlgorithm.Ed25519, status, WebhookSigningKeys.EncodePublicKey(id == "active" ? PublicKey : OldPublicKey), $"encrypted-{id}", null, Now.AddDays(-1), expires, null);

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _ring = Substitute.For<IWebhookSigningKeyRing>();
        _encryption = Substitute.For<IEncryption>();
        _grainFactory.GetGrain<IWebhookSigningKeyRing>("store").Returns(_ring);
        _keys = [Key("active", WebhookSigningKeyStatus.Active)];
        _ring.GetSigningKeys().Returns(_ => _keys);
        _encryption.Decrypt("encrypted-active").Returns(WebhookSigningKeys.EncodePrivateKey(Seed));
        _encryption.Decrypt("encrypted-old").Returns(WebhookSigningKeys.EncodePrivateKey(OldSeed));
        _signer = new(_grainFactory, _encryption, new FixedClock());
    }

    protected async Task Sign()
    {
        try
        {
            _signature = await _signer.Sign("store", _messageId, 1000, Body);
        }
        catch (Exception error)
        {
            _error = error;
        }
    }

    protected bool Verifies(byte[] publicKey) => WebhookSignatureVerification.Verify("message", 1000, Body, _signature.Signature, [], [publicKey], _signature.KeyId);

    sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
