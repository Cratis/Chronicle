// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Owns private, temporary TLS material for one out-of-process fixture.
/// </summary>
internal sealed class KernelFixtureCertificate
{
    const UnixFileMode DirectoryMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    const UnixFileMode FileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    readonly string _directory;

    KernelFixtureCertificate(string directory, string path, string hash, ImmutableArray<byte> publicCertificate)
    {
        _directory = directory;
        Path = path;
        Hash = hash;
        PublicCertificate = publicCertificate;
    }

    /// <summary>
    /// Gets the host path of the private PFX.
    /// </summary>
    internal string Path { get; }

    /// <summary>
    /// Gets the certificate digest used by the existing client validation callback.
    /// </summary>
    internal string Hash { get; }

    /// <summary>
    /// Gets an immutable public-only DER snapshot of the certificate exported to the PFX.
    /// </summary>
    internal ImmutableArray<byte> PublicCertificate { get; }

    /// <summary>
    /// Creates isolated material, restricting permissions before exporting the key.
    /// </summary>
    /// <param name="createCertificate">Optional certificate factory for failure specifications.</param>
    /// <param name="exportCertificate">Optional exporter for failure specifications.</param>
    /// <returns>The owned certificate material.</returns>
    /// <exception cref="PlatformNotSupportedException">POSIX permissions are unavailable.</exception>
    internal static KernelFixtureCertificate Create(
        Func<string, X509Certificate2>? createCertificate = null,
        Action<X509Certificate2, FileStream>? exportCertificate = null)
    {
        // The integration runners use Linux/macOS. Never fall back to permissive key files on another platform.
        if (OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Fixture TLS material requires POSIX file permissions.");
        }

        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"chronicle-kernel-tls-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(directory, DirectoryMode);
            createCertificate ??= _ => CreateCertificate("localhost");
            using var certificate = createCertificate(directory);
            var path = System.IO.Path.Combine(directory, "kernel.pfx");
            using var stream = new FileStream(path, new FileStreamOptions
            {
                Mode = System.IO.FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                UnixCreateMode = FileMode
            });
            (exportCertificate ?? ExportCertificate)(certificate, stream);
            return new(directory, path, certificate.GetCertHashString(), [.. certificate.Export(X509ContentType.Cert)]);
        }
        catch (Exception failure)
        {
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
            catch (Exception cleanup)
            {
                ReportCleanupFailure(failure, cleanup);
            }
            throw;
        }
    }

    /// <summary>
    /// Creates owned material for an alternate DNS name in TLS refusal specifications.
    /// </summary>
    /// <param name="hostName">The sole DNS name to include.</param>
    /// <returns>The owned certificate material.</returns>
    internal static KernelFixtureCertificate CreateForHost(string hostName) => Create(_ => CreateCertificate(hostName));

    /// <summary>
    /// Keeps cleanup failures attached to the original failure without replacing it.
    /// </summary>
    /// <param name="failure">The original failure.</param>
    /// <param name="cleanup">The subsequent cleanup failure.</param>
    internal static void ReportCleanupFailure(Exception failure, Exception cleanup)
    {
        failure.Data["FixtureCleanupFailure"] = failure.Data["FixtureCleanupFailure"] is Exception previous
            ? new AggregateException(previous, cleanup)
            : cleanup;
        Console.Error.WriteLine("Fixture cleanup also failed; details are attached to the original failure.");
    }

    /// <summary>
    /// Removes material only after container disposal has been confirmed.
    /// </summary>
    /// <param name="disposeContainer">Disposes the owning container.</param>
    /// <returns>The cleanup operation.</returns>
    internal async ValueTask DisposeAfter(Func<ValueTask> disposeContainer)
    {
        try
        {
            await disposeContainer();
        }
        catch
        {
            await Console.Error.WriteLineAsync($"Kernel disposal was not confirmed; fixture TLS material retained at '{_directory}'.");
            throw;
        }

        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    static X509Certificate2 CreateCertificate(string hostName)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={hostName}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName(hostName);
        if (string.Equals(hostName, "localhost", StringComparison.Ordinal))
        {
            names.AddIpAddress(IPAddress.Loopback);
            names.AddIpAddress(IPAddress.IPv6Loopback);
        }
        request.CertificateExtensions.Add(names.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, critical: true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], critical: true));
        var now = DateTimeOffset.UtcNow;
        return request.CreateSelfSigned(now.AddMinutes(-5), now.AddDays(1));
    }

    static void ExportCertificate(X509Certificate2 certificate, FileStream stream)
    {
        var bytes = certificate.Export(X509ContentType.Pfx);
        try
        {
            stream.Write(bytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }
}
