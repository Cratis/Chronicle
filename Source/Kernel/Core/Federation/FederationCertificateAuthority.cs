// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Creates the federation authority and issues certificates from it, using only <c language="text">System.Security.Cryptography</c>.
/// </summary>
/// <remarks>
/// The identity in an issued certificate is always supplied by the caller from a trusted source (the enrollment, or the
/// registry of issued certificates). Nothing in a certificate request but its public key is ever used, so a request can
/// never choose who it is.
/// </remarks>
public static class FederationCertificateAuthority
{
    /// <summary>
    /// The extended key usage of a certificate that authenticates a client.
    /// </summary>
    const string ClientAuthentication = "1.3.6.1.5.5.7.3.2";

    const string SubjectAlternativeNameOid = "2.5.29.17";

    /// <summary>
    /// The extended key usage of a certificate that authenticates a server.
    /// </summary>
    const string ServerAuthentication = "1.3.6.1.5.5.7.3.1";

    /// <summary>
    /// Create a new authority: a P-384 key pair and a self-signed certificate that may only sign leaf certificates.
    /// </summary>
    /// <param name="trustDomain">The trust domain the authority issues identities in.</param>
    /// <param name="notBefore">When the certificate becomes valid.</param>
    /// <param name="notAfter">When the certificate expires.</param>
    /// <returns>The authority certificate, with its private key.</returns>
    public static X509Certificate2 CreateAuthority(string trustDomain, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var request = new CertificateRequest($"CN=Chronicle Federation Authority ({trustDomain})", key, HashAlgorithmName.SHA384);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        using var created = request.CreateSelfSigned(notBefore, notAfter);

        // Round-trip through PKCS#12 so the key is exportable and usable on every platform.
        return X509CertificateLoader.LoadPkcs12(created.Export(X509ContentType.Pfx), null, X509KeyStorageFlags.Exportable);
    }

    /// <summary>
    /// Issue a client certificate for an identity from a certificate request.
    /// </summary>
    /// <param name="authority">The authority certificate, with its private key.</param>
    /// <param name="certificateRequest">The PKCS#10 certificate request. Only its public key, and its proof of possession, are used.</param>
    /// <param name="identity">The identity to put in the certificate, as a URI.</param>
    /// <param name="notBefore">When the certificate becomes valid.</param>
    /// <param name="notAfter">When the certificate expires.</param>
    /// <returns>The issued certificate, without a private key.</returns>
    /// <exception cref="InvalidFederationCertificateRequest">Thrown when the request is malformed, its signature is wrong or its key is too weak.</exception>
    public static X509Certificate2 IssueClient(X509Certificate2 authority, byte[] certificateRequest, string identity, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var request = Create(certificateRequest, "CN=chronicle-federation-peer");
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid(ClientAuthentication)], true));
        var names = new SubjectAlternativeNameBuilder();
        names.AddUri(new Uri(identity));
        request.CertificateExtensions.Add(names.Build(true));
        return Sign(request, authority, notBefore, notAfter);
    }

    /// <summary>
    /// Issue a server certificate for a host name from a certificate request.
    /// </summary>
    /// <param name="authority">The authority certificate, with its private key.</param>
    /// <param name="certificateRequest">The PKCS#10 certificate request. Only its public key, and its proof of possession, are used.</param>
    /// <param name="host">The DNS name or IP address the server is reached on.</param>
    /// <param name="notBefore">When the certificate becomes valid.</param>
    /// <param name="notAfter">When the certificate expires.</param>
    /// <returns>The issued certificate, without a private key.</returns>
    /// <exception cref="InvalidFederationCertificateRequest">Thrown when the request is malformed, its signature is wrong or its key is too weak.</exception>
    public static X509Certificate2 IssueServer(X509Certificate2 authority, byte[] certificateRequest, string host, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var request = Create(certificateRequest, $"CN={host}");
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid(ServerAuthentication)], true));
        var names = new SubjectAlternativeNameBuilder();
        if (System.Net.IPAddress.TryParse(host, out var address))
        {
            names.AddIpAddress(address);
        }
        else
        {
            names.AddDnsName(host);
        }

        request.CertificateExtensions.Add(names.Build(true));
        return Sign(request, authority, notBefore, notAfter);
    }

    /// <summary>
    /// Get the SHA-256 fingerprint of a certificate.
    /// </summary>
    /// <param name="certificate">The certificate.</param>
    /// <returns>The fingerprint, as lowercase hex.</returns>
    public static string Fingerprint(X509Certificate2 certificate) => Convert.ToHexStringLower(SHA256.HashData(certificate.RawData));

    /// <summary>
    /// Get the identity URI of a certificate.
    /// </summary>
    /// <param name="certificate">The certificate.</param>
    /// <returns>The first URI in the subject alternative name, or null when there is none.</returns>
    public static string? GetIdentity(X509Certificate2 certificate)
    {
        var extension = certificate.Extensions[SubjectAlternativeNameOid];
        if (extension is null)
        {
            return null;
        }

        try
        {
            var names = new AsnReader(extension.RawData, AsnEncodingRules.DER).ReadSequence();
            var uriTag = new Asn1Tag(TagClass.ContextSpecific, 6);
            while (names.HasData)
            {
                if (names.PeekTag() == uriTag)
                {
                    return names.ReadCharacterString(UniversalTagNumber.IA5String, uriTag);
                }

                names.ReadEncodedValue();
            }
        }
        catch (AsnContentException)
        {
            return null;
        }

        return null;
    }

    /// <summary>
    /// Check that a certificate was signed by an authority and is valid at a moment in time.
    /// </summary>
    /// <param name="certificate">The certificate to check.</param>
    /// <param name="authority">The authority certificate. Only its public part is used.</param>
    /// <param name="at">The moment to check validity at.</param>
    /// <returns>True when the certificate chains to the authority and is within its validity period.</returns>
    public static bool ChainsTo(X509Certificate2 certificate, X509Certificate2 authority, DateTimeOffset at)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(authority);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationTime = at.UtcDateTime;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
        if (!chain.Build(certificate))
        {
            return false;
        }

        return chain.ChainElements.Count >= 2
            && chain.ChainElements[^1].Certificate.RawData.AsSpan().SequenceEqual(authority.RawData);
    }

    /// <summary>
    /// Generate a serial number: 16 random bytes that are positive and never zero.
    /// </summary>
    /// <returns>The serial number bytes.</returns>
    public static byte[] NewSerial()
    {
        var serial = RandomNumberGenerator.GetBytes(16);
        serial[0] &= 0x7F;
        serial[0] |= 0x40;
        return serial;
    }

    static CertificateRequest Create(byte[] certificateRequest, string subject)
    {
        CertificateRequest loaded;
        try
        {
            loaded = CertificateRequest.LoadSigningRequest(certificateRequest, HashAlgorithmName.SHA256);
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        {
            throw new InvalidFederationCertificateRequest("the certificate request could not be read or its signature is not valid");
        }

        using var ecdsa = loaded.PublicKey.GetECDsaPublicKey();
        using var rsa = loaded.PublicKey.GetRSAPublicKey();
        var strong = ecdsa is not null ? ecdsa.KeySize >= 256 : rsa?.KeySize >= 2048;
        if (!strong)
        {
            throw new InvalidFederationCertificateRequest("the key must be ECDSA with at least 256 bits or RSA with at least 2048 bits");
        }

        var request = new CertificateRequest(new X500DistinguishedName(subject), loaded.PublicKey, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(loaded.PublicKey, false));
        return request;
    }

    static X509Certificate2 Sign(CertificateRequest request, X509Certificate2 authority, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using var issued = request.Create(authority, notBefore, notAfter, NewSerial());
        return X509CertificateLoader.LoadCertificate(issued.RawData);
    }
}
