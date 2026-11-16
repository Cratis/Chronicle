// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;
namespace Cratis.Chronicle.Server.for_ReloadingCertificate.given;

public class a_reloading_certificate : Specification
{
    protected string _path;
    protected ManualTime _time;
    protected X509Certificate2 _first;
    protected X509Certificate2 _second;
    internal ReloadingCertificate _certificate;
    protected int _reloads;
    protected int _failures;

    void Establish()
    {
        _path = Path.Combine(Path.GetTempPath(), $"chronicle-reload-{Guid.NewGuid():N}.pfx");
        _time = new ManualTime();
        _first = Create("CN=first");
        _second = Create("CN=second");
        Write(_first);
        _certificate = new ReloadingCertificate(
            new Tls { Enabled = true, CertificatePath = _path, CertificatePassword = "pw" },
            _first,
            TimeSpan.FromSeconds(30),
            _time,
            _ => _reloads++,
            _ => _failures++);
    }

    void Destroy()
    {
        _first.Dispose();
        _second.Dispose();
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    protected void Write(X509Certificate2 certificate)
    {
        File.WriteAllBytes(_path, certificate.Export(X509ContentType.Pkcs12, "pw"));
        File.SetLastWriteTimeUtc(_path, DateTime.UtcNow.AddSeconds(Random.Shared.Next(1, 100000)));
    }

    static X509Certificate2 Create(string subject)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        return X509CertificateLoader.LoadPkcs12(created.Export(X509ContentType.Pkcs12, "pw"), "pw", X509KeyStorageFlags.Exportable);
    }
}
