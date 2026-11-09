// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents a SHA-256 pin of a certificate's SubjectPublicKeyInfo, which survives re-issuing a certificate for the same key.
/// </summary>
/// <param name="Hash">The SHA-256 hash of the SubjectPublicKeyInfo.</param>
public record CertificatePin(ReadOnlyMemory<byte> Hash)
{
    const string Prefix = "sha256:";

    /// <summary>
    /// Create a pin from a certificate.
    /// </summary>
    /// <param name="certificate">The certificate to pin.</param>
    /// <returns>The pin.</returns>
    public static CertificatePin From(X509Certificate2 certificate) =>
        new(SHA256.HashData(certificate.PublicKey.ExportSubjectPublicKeyInfo()));

    /// <summary>
    /// Parse a pin in the form <c language="text">sha256:&lt;base64&gt;</c>.
    /// </summary>
    /// <param name="value">The textual pin.</param>
    /// <returns>The pin.</returns>
    /// <exception cref="FormatException">Thrown when the value is not a valid SHA-256 pin.</exception>
    public static CertificatePin Parse(string value)
    {
        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new FormatException($"A certificate pin must start with '{Prefix}'.");
        }

        var hash = Convert.FromBase64String(value[Prefix.Length..]);
        if (hash.Length != SHA256.HashSizeInBytes)
        {
            throw new FormatException("A certificate pin must be a SHA-256 hash.");
        }

        return new CertificatePin(hash);
    }

    /// <summary>
    /// Check whether a certificate matches this pin, comparing in constant time. The certificate chain is deliberately not consulted.
    /// </summary>
    /// <param name="certificate">The presented certificate.</param>
    /// <returns>True when the certificate's public key matches the pin.</returns>
    public bool Matches(X509Certificate2 certificate) =>
        CryptographicOperations.FixedTimeEquals(Hash.Span, From(certificate).Hash.Span);

    /// <inheritdoc/>
    public virtual bool Equals(CertificatePin? other) => other is not null && Hash.Span.SequenceEqual(other.Hash.Span);

    /// <inheritdoc/>
    public override int GetHashCode() => BitConverter.ToInt32(Hash.Span);

    /// <inheritdoc/>
    public override string ToString() => $"{Prefix}{Convert.ToBase64String(Hash.Span)}";
}
