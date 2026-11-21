// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_Encryption.given;

public class a_forged_signed_value : a_signed_value
{
    protected Exception _verificationFailure;
    protected Exception _releaseFailure;
    protected ReleasedProtectedValue _released;
    protected bool _attackerSignatureVerifies;

    protected async Task VerifyForgery(Func<byte[], byte[]> certificateFor)
    {
        using var attacker = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var signingKey = attacker.ExportSubjectPublicKeyInfo();
        var original = EncryptionEnvelope.Read(_encrypted);
        var commitment = EnvelopeSigning.Commit(_address);
        var certificate = certificateFor(EnvelopeSigning.CertificateMessage(original.Revision, commitment, _key.Public, signingKey));
        var forged = original with
        {
            Header = EncryptionEnvelope.CreateSignedHeader(original.Revision, (_key.Public, certificate, signingKey), original.WrappedKey.Length),
            SigningKey = signingKey,
            Certificate = certificate
        };
        var message = EnvelopeSigning.ValueMessage(forged, commitment);
        forged = forged with { Signature = attacker.SignData(message, HashAlgorithmName.SHA256, EnvelopeSigning.ValueSignatureFormat) };
        _attackerSignatureVerifies = attacker.VerifyData(message, forged.Signature, HashAlgorithmName.SHA256, EnvelopeSigning.ValueSignatureFormat);
        var storage = Substitute.For<IEncryptionKeyStorage>();
        storage.GetErasureFor(_address.EventStore, _address.Namespace, _address.Identifier)
            .Returns(new EncryptionKeyErasure(original.Revision, [_key.Fingerprint], false));

        _verificationFailure = Catch.Exception(() => EnvelopeSigning.Verify(forged, _address));
        _releaseFailure = await Catch.Exception(async () => _released = await ErasedValueClassifier.Classify(
            storage, _encryption, (_address.EventStore, _address.Namespace, _address.Identifier), forged.Bytes(), JsonValue.Create("stored")));
    }
}
