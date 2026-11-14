// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A federation authority with hosted federation enabled, an in-memory registry and a clock that moves only when told to.
/// </summary>
public class a_federation_authority : Specification
{
    protected FederationAuthority _authority;
    protected FakeCertificateRegistry _registry;
    protected FakeAuthorityKeys _keys;
    protected ChronicleOptions _options;
    protected ControllableTimeProvider _time;

    void Establish()
    {
        _registry = new();
        _keys = new();
        _time = new();
        _options = new()
        {
            Federation = new()
            {
                Enabled = true,
                Hosted = new()
                {
                    Enabled = true,
                    PublicEndpoint = "https://federation.example.com:35100",
                    TrustDomain = "example"
                }
            }
        };
        _authority = Create(_options);
    }

    protected FederationAuthority Create(ChronicleOptions options) =>
        new(_registry, _keys, Options.Create(options), _time, NullLogger<FederationAuthority>.Instance);

    protected static byte[] Csr(ECDsa key, string subject = "CN=chronicle-federation-peer") =>
        new CertificateRequest(subject, key, HashAlgorithmName.SHA256).CreateSigningRequest();

    protected static ECDsa NewKey() => ECDsa.Create(ECCurve.NamedCurves.nistP256);

    protected static FederationEnrollmentCode CodeOf(FederationEnrollmentCreatedResult created)
    {
        FederationEnrollmentCode.TryParse(created.Code, out var code).ShouldBeTrue();
        return code;
    }

    protected async Task<FederationEnrollmentCode> Enroll(string subject = "acme", string instance = "prod")
    {
        var created = await _authority.CreateEnrollment(subject, instance);
        return CodeOf(created);
    }

    protected async Task<(FederationIssuance Issuance, ECDsa Key)> Redeem(FederationEnrollmentCode code)
    {
        var key = NewKey();
        return (await _authority.Redeem(code.EnrollmentId, code.Secret, Csr(key)), key);
    }

    protected static X509Certificate2 CertificateOf(FederationIssuance issuance) =>
        X509CertificateLoader.LoadCertificate(Convert.FromBase64String(issuance.Certificate));
}
