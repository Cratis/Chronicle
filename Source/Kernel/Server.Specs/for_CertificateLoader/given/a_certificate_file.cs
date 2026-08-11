// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Server.for_CertificateLoader.given;

public class a_certificate_file : Specification
{
    protected string _certificatePath;
    protected X509Certificate2 _sourceCertificate;

    void Establish()
    {
        _certificatePath = Path.Combine(Path.GetTempPath(), $"chronicle-certificate-{Guid.NewGuid():N}.pfx");
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=chronicle-specs", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        _sourceCertificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    void Destroy()
    {
        _sourceCertificate?.Dispose();

        if (File.Exists(_certificatePath))
        {
            File.Delete(_certificatePath);
        }
    }

    protected void WritePkcs12(string? password) =>
        File.WriteAllBytes(_certificatePath, _sourceCertificate.Export(X509ContentType.Pkcs12, password));

    protected Configuration.ChronicleOptions OptionsWithTls(string? password, bool enabled = true) =>
        new()
        {
            Tls = new Configuration.Tls
            {
                Enabled = enabled,
                CertificatePath = _certificatePath,
                CertificatePassword = password
            }
        };
}
