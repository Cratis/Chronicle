// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security.for_Encryption.given;

public class two_certificates : Specification
{
    protected const string TheSecret = "the-webhook-client-secret";

    protected X509Certificate2 _firstCertificate;
    protected X509Certificate2 _secondCertificate;

    readonly List<X509Certificate2> _certificates = [];

    void Establish()
    {
        _firstCertificate = CreateCertificate("chronicle-specs-first");
        _secondCertificate = CreateCertificate("chronicle-specs-second");
    }

    void Destroy() => _certificates.ForEach(_ => _.Dispose());

    protected static string EncryptWithoutKeyId(X509Certificate2 certificate, string plainText)
    {
        // The shape Chronicle wrote before ciphertext carried a key id - bare base64, with nothing saying
        // which certificate made it.
        using var rsa = certificate.GetRSAPublicKey()!;

        return Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(plainText), RSAEncryptionPadding.OaepSHA256));
    }

    protected static Encryption EncryptionWith(params X509Certificate2[] certificates) =>
        new(EncryptionCertificateRing.For(certificates), Substitute.For<ILogger<Encryption>>());

    X509Certificate2 CreateCertificate(string name)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        _certificates.Add(certificate);

        return certificate;
    }
}
