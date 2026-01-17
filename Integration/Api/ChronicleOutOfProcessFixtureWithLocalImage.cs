// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;

namespace Cratis.Chronicle.Integration.Api;

/// <summary>
/// Represents a fixture for the Chronicle Out of Process integration tests using a locally built image.
/// </summary>
public class ChronicleOutOfProcessFixtureWithLocalImage : ChronicleOutOfProcessFixture
{
    const string CertificatePassword = "TestPassword123";

    /// <summary>
    /// Gets the path to the certificate file.
    /// </summary>
    public static string CertificatePath { get; }

    /// <summary>
    /// Gets the certificate password.
    /// </summary>
    public static string CertPassword => CertificatePassword;

    static ChronicleOutOfProcessFixtureWithLocalImage()
    {
        var certDir = Path.Combine(Path.GetTempPath(), "chronicle-test-certs");
        CertificatePath = Path.Combine(certDir, "chronicle-test.pfx");
        TestCertificateGenerator.GenerateAndSaveCertificate(CertificatePath, CertificatePassword);
    }

    /// <inheritdoc/>
    protected override ContainerBuilder ConfigureImage(ContainerBuilder builder) =>
        builder
            .WithImage(Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_LOCAL_IMAGE") ?? "cratis/chronicle:local-development")
            .WithBindMount(CertificatePath, "/app/certs/chronicle.pfx")
            .WithEnvironment("Cratis__Chronicle__Tls__CertificatePath", "/app/certs/chronicle.pfx")
            .WithEnvironment("Cratis__Chronicle__Tls__CertificatePassword", CertificatePassword);
}
