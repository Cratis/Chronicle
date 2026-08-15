// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_EncryptionCertificateRing.given;

public class certificate_files : Specification
{
    protected const string Password = "the-certificate-password";

    protected string _activeCertificatePath;
    protected string _previousCertificatePath;
    protected X509Certificate2 _activeCertificate;
    protected X509Certificate2 _previousCertificate;

    readonly List<string> _files = [];
    readonly List<X509Certificate2> _certificates = [];

    void Establish()
    {
        _activeCertificate = CreateCertificate("chronicle-specs-active");
        _previousCertificate = CreateCertificate("chronicle-specs-previous");
        _activeCertificatePath = WritePkcs12(_activeCertificate, Password);
        _previousCertificatePath = WritePkcs12(_previousCertificate, Password);
    }

    void Destroy()
    {
        _certificates.ForEach(_ => _.Dispose());
        _files.Where(File.Exists).ToList().ForEach(File.Delete);
    }

    protected static EncryptionCertificate Configuration(string? activePath, params string?[] previousPaths) =>
        new()
        {
            CertificatePath = activePath,
            CertificatePassword = Password,
            Previous = [.. previousPaths.Select(_ => new PreviousEncryptionCertificate
            {
                CertificatePath = _,
                CertificatePassword = Password
            })]
        };

    protected X509Certificate2 CreateCertificate(string name)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        _certificates.Add(certificate);

        return certificate;
    }

    protected string WritePkcs12(X509Certificate2 certificate, string? password)
    {
        var path = Path.Combine(Path.GetTempPath(), $"chronicle-ring-{Guid.NewGuid():N}.pfx");
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pkcs12, password));
        _files.Add(path);

        return path;
    }

    protected string PathThatHoldsNoFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"chronicle-ring-{Guid.NewGuid():N}.pfx");
        _files.Add(path);

        return path;
    }

    protected string WritePkcs12WithoutPrivateKey(X509Certificate2 certificate)
    {
        using var publicOnly = X509CertificateLoader.LoadCertificate(certificate.RawData);

        return WritePkcs12(publicOnly, Password);
    }
}
