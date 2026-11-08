// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.for_FederationClientCertificates;

public class when_getting_a_named_certificate : given.certificates
{
    const string Password = "spec-only-password";

    string _directory;
    FederationClientCertificates _certificates;
    X509Certificate2 _first;
    X509Certificate2 _second;
    Exception _unknown;
    Exception _missingFile;

    void Establish()
    {
        _directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        var path = Path.Combine(_directory, "client.pfx");
        var passwordPath = Path.Combine(_directory, "client.pass");
        File.WriteAllBytes(path, _certificate.Export(X509ContentType.Pfx, Password));
        File.WriteAllText(passwordPath, Password + Environment.NewLine);

        var options = new ChronicleOptions
        {
            Federation = new Cratis.Chronicle.Configuration.Federation
            {
                Certificates = new Dictionary<string, FederationCertificate>
                {
                    ["client"] = new() { Path = path, PasswordPath = passwordPath },
                    ["gone"] = new() { Path = Path.Combine(_directory, "gone.pfx") }
                }
            }
        };
        _certificates = new(Options.Create(options));
    }

    void Because()
    {
        _first = _certificates.Get("client");
        _second = _certificates.Get("client");
        _unknown = Catch.Exception(() => _certificates.Get("unknown"));
        _missingFile = Catch.Exception(() => _certificates.Get("gone"));
    }

    void Destroy()
    {
        _certificates.Dispose();
        Directory.Delete(_directory, true);
    }

    [Fact] void should_load_the_certificate_with_its_private_key() => _first.HasPrivateKey.ShouldBeTrue();
    [Fact] void should_load_the_configured_certificate() => CertificatePin.From(_first).ShouldEqual(CertificatePin.From(_certificate));
    [Fact] void should_cache_the_certificate() => _second.ShouldBeSame(_first);
    [Fact] void should_refuse_an_unknown_name() => _unknown.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_refuse_a_missing_file() => _missingFile.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_put_the_password_in_errors() => _missingFile.Message.ShouldNotContain(Password);
}
