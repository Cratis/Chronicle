// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Formats.Asn1;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.for_FederationCertificateAuthority;

public class when_chaining_a_certificate_that_names_an_issuer_to_download : Specification
{
    TcpListener _listener;
    X509Certificate2 _authority;
    X509Certificate2 _otherAuthority;
    X509Certificate2 _leaf;
    bool _chains;
    bool _connected;

    void Establish()
    {
        _listener = new(IPAddress.Loopback, 0);
        _listener.Start();
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;

        var now = DateTimeOffset.UtcNow;
        _authority = FederationCertificateAuthority.CreateAuthority("example", now.AddDays(-1), now.AddYears(1));
        _otherAuthority = FederationCertificateAuthority.CreateAuthority("other", now.AddDays(-1), now.AddYears(1));

        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=leaf", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid(FederationCertificateAuthority.ClientAuthentication)], true));
        request.CertificateExtensions.Add(CreateAuthorityInformationAccess($"http://127.0.0.1:{port}/issuer.cer"));
        using var issued = request.Create(_otherAuthority, now.AddHours(-1), now.AddDays(1), FederationCertificateAuthority.NewSerial());
        _leaf = X509CertificateLoader.LoadCertificate(issued.RawData);
    }

    void Because()
    {
        _chains = FederationCertificateAuthority.ChainsTo(_leaf, _authority, DateTimeOffset.UtcNow, FederationCertificateAuthority.ClientAuthentication);
        _connected = _listener.Pending();
    }

    void Destroy()
    {
        _listener.Stop();
        _leaf.Dispose();
        _authority.Dispose();
        _otherAuthority.Dispose();
    }

    [Fact] void should_not_chain() => _chains.ShouldBeFalse();
    [Fact] void should_not_make_any_request_to_the_named_issuer() => _connected.ShouldBeFalse();

    static X509Extension CreateAuthorityInformationAccess(string issuerUri)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        using (writer.PushSequence())
        {
            writer.WriteObjectIdentifier("1.3.6.1.5.5.7.48.2");
            writer.WriteCharacterString(UniversalTagNumber.IA5String, issuerUri, new Asn1Tag(TagClass.ContextSpecific, 6));
        }

        return new X509Extension(new Oid("1.3.6.1.5.5.7.1.1"), writer.Encode(), false);
    }
}
