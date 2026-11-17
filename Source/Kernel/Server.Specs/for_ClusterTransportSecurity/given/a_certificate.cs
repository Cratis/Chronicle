// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.for_ClusterTransportSecurity.given;

public class a_certificate : Specification
{
    protected X509Certificate2 _x509;
    internal ReloadingCertificate _certificate;

    void Establish()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=chronicle", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        _x509 = X509CertificateLoader.LoadPkcs12(created.Export(X509ContentType.Pkcs12, "pw"), "pw", X509KeyStorageFlags.Exportable);
        _certificate = new ReloadingCertificate(new Tls { CertificatePath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.pfx") }, _x509, TimeSpan.FromSeconds(30), TimeProvider.System);
    }

    void Destroy() => _x509.Dispose();
}
