// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Provides helper methods for loading TLS certificates for the Chronicle client.
/// </summary>
public static class CertificateLoader
{
    const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";
    const string CertificatePemLabel = "CERTIFICATE";
    const string Pkcs7PemLabel = "PKCS7";
    const string TrustedCertificatePemLabel = "TRUSTED CERTIFICATE";

    /// <summary>
    /// Loads a certificate based on the priority: ChronicleOptions → Embedded Certificate → Dev Certificate.
    /// </summary>
    /// <param name="certificatePath">The certificate path from options.</param>
    /// <param name="certificatePassword">The certificate password from options.</param>
    /// <returns>The loaded certificate or null if TLS is disabled or no certificate is available.</returns>
    /// <exception cref="CertificateDoesNotExist">Thrown when the specified certificate file does not exist.</exception>
    /// <exception cref="InvalidCertificateOrPassword">Thrown when the specified certificate file is invalid or the password is incorrect.</exception>
    public static X509Certificate2 LoadCertificate(string certificatePath, string? certificatePassword)
    {
        if (!File.Exists(certificatePath))
        {
            throw new CertificateDoesNotExist(certificatePath);
        }

        return LoadCertificateFromPath(certificatePath, certificatePassword) ??
            throw new InvalidCertificateOrPassword(certificatePath);
    }

    /// <summary>
    /// Loads the certificate authorities used to validate the server certificate from a PEM or PKCS#7 file.
    /// </summary>
    /// <param name="caCertificatePath">The path to a file with one or more CA certificates, in PEM (one or more certificates) or PKCS#7 (PEM or DER) format.</param>
    /// <returns>The loaded CA certificates.</returns>
    /// <exception cref="CertificateAuthorityDoesNotExist">Thrown when the specified file does not exist.</exception>
    /// <exception cref="InvalidCertificateAuthority">Thrown when the specified file cannot be read or contains no certificates.</exception>
    /// <exception cref="UnsupportedCertificateAuthorityFormat">Thrown when the specified file contains an OpenSSL TRUSTED CERTIFICATE block.</exception>
    public static X509Certificate2Collection LoadCertificateAuthorities(string caCertificatePath)
    {
        if (!File.Exists(caCertificatePath))
        {
            throw new CertificateAuthorityDoesNotExist(caCertificatePath);
        }

        X509Certificate2Collection certificateAuthorities;
        try
        {
            certificateAuthorities = ReadCertificateAuthorities(caCertificatePath, File.ReadAllBytes(caCertificatePath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException or FormatException)
        {
            throw new InvalidCertificateAuthority(caCertificatePath);
        }

        return certificateAuthorities.Count > 0 ? certificateAuthorities : throw new InvalidCertificateAuthority(caCertificatePath);
    }

    /// <summary>
    /// Creates the server certificate validation callback used for the client's TLS connections.
    /// </summary>
    /// <param name="skipTlsValidation">Whether to skip validation and accept any server certificate.</param>
    /// <param name="pinnedCertificateHash">Optional certificate hash to pin the server certificate to (from a configured client certificate).</param>
    /// <returns>A <see cref="RemoteCertificateValidationCallback"/> that validates the server certificate.</returns>
    /// <remarks>
    /// The default is secure: a certificate that fails validation is rejected. It is accepted only when the
    /// certificate is valid, when <paramref name="skipTlsValidation"/> is set, or when only the chain fails
    /// validation and the certificate matches <paramref name="pinnedCertificateHash"/>.
    /// Skipping validation accepts any certificate — including self-signed ones — so only use it for a trusted server on a trusted network.
    /// </remarks>
    public static RemoteCertificateValidationCallback CreateServerCertificateValidationCallback(bool skipTlsValidation, string? pinnedCertificateHash) =>
        CreateServerCertificateValidationCallback(skipTlsValidation, pinnedCertificateHash, certificateAuthorities: null);

    /// <summary>
    /// Creates the server certificate validation callback used for the client's TLS connections, optionally trusting a set of certificate authorities.
    /// </summary>
    /// <param name="skipTlsValidation">Whether to skip validation and accept any server certificate.</param>
    /// <param name="pinnedCertificateHash">Optional certificate hash to pin the server certificate to (from a configured client certificate).</param>
    /// <param name="certificateAuthorities">Optional certificate authorities the server certificate must chain to, see <see cref="LoadCertificateAuthorities"/>.</param>
    /// <returns>A <see cref="RemoteCertificateValidationCallback"/> that validates the server certificate.</returns>
    /// <remarks>
    /// Without <paramref name="certificateAuthorities"/>, this behaves as <see cref="CreateServerCertificateValidationCallback(bool, string?)"/>.
    /// With them, and unless <paramref name="skipTlsValidation"/> is set, the server certificate is accepted only when its chain
    /// builds to one of the certificate authorities, or when it matches <paramref name="pinnedCertificateHash"/>. Any error other
    /// than a chain error, such as a host-name mismatch, still rejects the certificate. Revocation is not checked against the certificate authorities.
    /// </remarks>
    public static RemoteCertificateValidationCallback CreateServerCertificateValidationCallback(bool skipTlsValidation, string? pinnedCertificateHash, X509Certificate2Collection? certificateAuthorities)
    {
        if (skipTlsValidation || certificateAuthorities is null)
        {
            return CreateSystemTrustValidationCallback(skipTlsValidation, pinnedCertificateHash);
        }

        return (sender, certificate, chain, sslPolicyErrors) =>
        {
            if ((sslPolicyErrors & ~SslPolicyErrors.RemoteCertificateChainErrors) != SslPolicyErrors.None || certificate is null)
            {
                return false;
            }

            if (pinnedCertificateHash is not null && certificate.GetCertHashString() == pinnedCertificateHash)
            {
                return true;
            }

            return ChainsToCertificateAuthority(certificate, chain, certificateAuthorities);
        };
    }

    static RemoteCertificateValidationCallback CreateSystemTrustValidationCallback(bool skipTlsValidation, string? pinnedCertificateHash) =>
        (sender, certificate, chain, sslPolicyErrors) =>
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (skipTlsValidation)
            {
                return true;
            }

            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors &&
                pinnedCertificateHash is not null && certificate is not null)
            {
                return certificate.GetCertHashString() == pinnedCertificateHash;
            }

            return false;
        };

    static bool ChainsToCertificateAuthority(X509Certificate certificate, X509Chain? presentedChain, X509Certificate2Collection certificateAuthorities)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.ApplicationPolicy.Add(new Oid(ServerAuthenticationOid));
        chain.ChainPolicy.CustomTrustStore.AddRange(certificateAuthorities);
        chain.ChainPolicy.ExtraStore.AddRange(certificateAuthorities);
        if (presentedChain is not null)
        {
            foreach (var element in presentedChain.ChainElements)
            {
                chain.ChainPolicy.ExtraStore.Add(element.Certificate);
            }
        }

        if (certificate is X509Certificate2 serverCertificate)
        {
            return chain.Build(serverCertificate);
        }

        using var copy = new X509Certificate2(certificate);
        return chain.Build(copy);
    }

    static X509Certificate2Collection ReadCertificateAuthorities(string path, byte[] content)
    {
        var certificateAuthorities = new X509Certificate2Collection();
        if (!ReadPemCertificateAuthorities(certificateAuthorities, path, content))
        {
            ReadBinaryCertificateAuthorities(certificateAuthorities, content);
        }

        return certificateAuthorities;
    }

    static bool ReadPemCertificateAuthorities(X509Certificate2Collection certificateAuthorities, string path, byte[] content)
    {
        var remaining = Encoding.ASCII.GetString(content).AsSpan();
        var foundPem = false;
        while (PemEncoding.TryFind(remaining, out var fields))
        {
            foundPem = true;
            AddPemBlock(certificateAuthorities, path, remaining[fields.Label].ToString(), Convert.FromBase64String(remaining[fields.Base64Data].ToString()));
            remaining = remaining[fields.Location.End..];
        }

        return foundPem;
    }

    static void AddPemBlock(X509Certificate2Collection certificateAuthorities, string path, string label, byte[] data)
    {
        if (string.Equals(label, TrustedCertificatePemLabel, StringComparison.Ordinal))
        {
            throw new UnsupportedCertificateAuthorityFormat(path);
        }

        if (string.Equals(label, CertificatePemLabel, StringComparison.Ordinal))
        {
            certificateAuthorities.Add(LoadDerCertificate(data));
        }
        else if (string.Equals(label, Pkcs7PemLabel, StringComparison.Ordinal))
        {
            ImportPkcs7(certificateAuthorities, data);
        }
    }

    static void ReadBinaryCertificateAuthorities(X509Certificate2Collection certificateAuthorities, byte[] content)
    {
        switch (X509Certificate2.GetCertContentType(content))
        {
            case X509ContentType.Cert:
                certificateAuthorities.Add(LoadDerCertificate(content));
                break;
            case X509ContentType.Pkcs7:
                ImportPkcs7(certificateAuthorities, content);
                break;
        }
    }

    static X509Certificate2 LoadDerCertificate(byte[] content)
    {
#if NET8_0
        return new X509Certificate2(content);
#else
        return X509CertificateLoader.LoadCertificate(content);
#endif
    }

    static void ImportPkcs7(X509Certificate2Collection certificateAuthorities, byte[] content)
    {
        if (X509Certificate2.GetCertContentType(content) != X509ContentType.Pkcs7)
        {
            throw new CryptographicException("The content is not PKCS#7.");
        }

        // X509CertificateLoader has no PKCS#7 support; importing a PKCS#7 bundle needs the collection import.
#pragma warning disable SYSLIB0057
        certificateAuthorities.Import(content);
#pragma warning restore SYSLIB0057
    }

    static X509Certificate2 LoadCertificateFromPath(string path, string? password)
    {
        // A client certificate needs the private key; read PKCS#12 whether or not a password is configured.
#if NET8_0
        return new X509Certificate2(path, password);
#else
        return X509CertificateLoader.LoadPkcs12FromFile(path, password);
#endif
    }
}
