// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.Integration.Cli;

/// <summary>
/// Fixture that uses the locally built Docker image for CLI integration tests.
/// </summary>
public class ChronicleOutOfProcessFixtureWithLocalImage : ChronicleOutOfProcessFixture
{
    const string CertificatePassword = "TestPassword123";

    /// <summary>
    /// Gets the path to the certificate file.
    /// </summary>
    public static string CertificatePath { get; }

    static ChronicleOutOfProcessFixtureWithLocalImage()
    {
        var certDir = Path.Combine(Path.GetTempPath(), "chronicle-test-certs");
        CertificatePath = Path.Combine(certDir, "chronicle-test.pfx");
        TestCertificateGenerator.GenerateAndSaveCertificate(CertificatePath, CertificatePassword);
    }

    /// <inheritdoc/>
    protected override IContainer BuildContainer(INetwork network)
    {
        var waitStrategy = Wait.ForUnixContainer()
            .AddCustomWaitStrategy(new HttpsHealthWait(8080));

        var builder = new ContainerBuilder("cratis/chronicle:latest-development");
        builder = ConfigureImage(builder)
            .WithEnvironment("Storage__ConnectionDetails", $"mongodb://localhost:{MongoDBPort}")
            .WithPortBinding(MongoDBPort, 27017)
            .WithPortBinding(8081, 8080)
            .WithPortBinding(35001, 35000)
            .WithHostname(HostName)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(network)
            .WithWaitStrategy(waitStrategy)
            .WithStartupCallback((container, ct) =>
            {
                Console.WriteLine($"Chronicle container {container.Id} started successfully");
                return Task.CompletedTask;
            });

        return builder.Build();
    }

    /// <inheritdoc/>
    protected override ContainerBuilder ConfigureImage(ContainerBuilder builder) =>
        builder
            .WithImage(Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_LOCAL_IMAGE") ?? "cratis/chronicle:local-development")
            .WithBindMount(CertificatePath, "/app/certs/chronicle.pfx")
            .WithEnvironment("Cratis__Chronicle__Tls__CertificatePath", "/app/certs/chronicle.pfx")
            .WithEnvironment("Cratis__Chronicle__Tls__CertificatePassword", CertificatePassword)
            .WithEnvironment("Cratis__Chronicle__EncryptionCertificate__CertificatePath", "/app/certs/chronicle.pfx")
            .WithEnvironment("Cratis__Chronicle__EncryptionCertificate__CertificatePassword", CertificatePassword);
}
