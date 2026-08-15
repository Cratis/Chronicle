// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionCertificateRing"/> resolved from <see cref="ChronicleOptions"/>.
/// </summary>
[Singleton]
public sealed class EncryptionCertificateRing : IEncryptionCertificateRing
{
    readonly Lazy<List<EncryptionCertificateRingEntry>> _entries;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionCertificateRing"/> class.
    /// </summary>
    /// <param name="chronicleOptions"><see cref="IOptions{ChronicleOptions}"/> holding the certificate configuration.</param>
    public EncryptionCertificateRing(IOptions<ChronicleOptions> chronicleOptions)
        : this(chronicleOptions.Value.EncryptionCertificate)
    {
    }

    EncryptionCertificateRing(EncryptionCertificate configuration)
    {
        IsConfigured = configuration.IsConfigured;
        _entries = new Lazy<List<EncryptionCertificateRingEntry>>(() => Resolve(configuration));
    }

    EncryptionCertificateRing(List<EncryptionCertificateRingEntry> entries)
    {
        IsConfigured = entries.Count > 0;
        _entries = new Lazy<List<EncryptionCertificateRingEntry>>(entries);
    }

    /// <inheritdoc/>
    public bool IsConfigured { get; }

    /// <inheritdoc/>
    public EncryptionCertificateRingEntry Active =>
        _entries.Value.FirstOrDefault(_ => _.Role == EncryptionCertificateRole.Active) ?? throw new EncryptionCertificateNotConfigured();

    /// <inheritdoc/>
    public IEnumerable<EncryptionCertificateRingEntry> Previous =>
        _entries.Value.Where(_ => _.Role == EncryptionCertificateRole.Previous);

    /// <inheritdoc/>
    public IEnumerable<EncryptionCertificateRingEntry> All => _entries.Value;

    /// <summary>
    /// Creates a ring from Chronicle configuration, for use before the service provider exists.
    /// </summary>
    /// <param name="options">The <see cref="ChronicleOptions"/> to read the certificate configuration from.</param>
    /// <returns>A new <see cref="EncryptionCertificateRing"/>.</returns>
    /// <remarks>
    /// Data Protection and OpenIddict are configured while the service collection is still being built, which
    /// is before anything can be resolved from it. They get the ring from here, and the same instance is then
    /// registered so everything else sees exactly the certificates they were given.
    /// </remarks>
    public static EncryptionCertificateRing From(ChronicleOptions options) => new(options.EncryptionCertificate);

    /// <summary>
    /// Creates a ring from already-loaded certificates, the active one first.
    /// </summary>
    /// <param name="certificates">The certificates, active first.</param>
    /// <returns>A new <see cref="EncryptionCertificateRing"/>.</returns>
    public static EncryptionCertificateRing For(params X509Certificate2[] certificates) =>
        new([.. certificates.Select((certificate, index) => new EncryptionCertificateRingEntry(
            certificate.Thumbprint,
            index == 0 ? EncryptionCertificateRole.Active : EncryptionCertificateRole.Previous,
            string.Empty,
            certificate))]);

    /// <inheritdoc/>
    public EncryptionCertificateRingEntry? Find(string keyId) =>
        _entries.Value.FirstOrDefault(_ => string.Equals(_.KeyId, keyId, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc/>
    public EncryptionCertificateRingStatus GetStatus()
    {
        if (!IsConfigured)
        {
            return EncryptionCertificateRingStatus.NotConfigured;
        }

        var entries = _entries.Value;
        return new(true, Active.KeyId, [.. entries.Select(_ => _.ToStatus())]);
    }

    static List<EncryptionCertificateRingEntry> Resolve(EncryptionCertificate configuration)
    {
        var previous = configuration.Previous.ToArray();

        if (!configuration.IsConfigured)
        {
            return previous.Length > 0 ? throw new PreviousEncryptionCertificatesWithoutActive() : [];
        }

        List<EncryptionCertificateRingEntry> entries =
        [
            Load(configuration.CertificatePath, configuration.CertificatePassword, EncryptionCertificateRole.Active),
            .. previous.Select(_ => Load(_.CertificatePath, _.CertificatePassword, EncryptionCertificateRole.Previous))
        ];

        var duplicate = entries
            .GroupBy(_ => _.KeyId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(_ => _.Count() > 1);

        return duplicate is null ? entries : throw new DuplicateEncryptionCertificateInRing(duplicate.Key);
    }

    static EncryptionCertificateRingEntry Load(string? certificatePath, string? certificatePassword, EncryptionCertificateRole role)
    {
        if (string.IsNullOrEmpty(certificatePath))
        {
            throw new EncryptionCertificateWithoutPath();
        }

        if (!File.Exists(certificatePath))
        {
            throw new EncryptionCertificateFileNotFound(certificatePath, role);
        }

        // Read as PKCS#12 with or without a password: the ring needs the private key, which only PKCS#12
        // carries, and LoadCertificateFromFile reads DER/PEM only - a password-less .pfx fails there with
        // "ASN1 corrupted data" rather than loading.
        var certificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath, certificatePassword);

        return certificate.HasPrivateKey
            ? new(certificate.Thumbprint, role, certificatePath, certificate)
            : throw new EncryptionCertificateWithoutPrivateKey(certificate.Thumbprint, certificatePath);
    }
}
