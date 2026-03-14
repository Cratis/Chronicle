// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Storage.MongoDB;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Integration.Cli;

/// <summary>
/// Fixture that uses the locally built Docker image for CLI integration tests.
/// </summary>
/// <remarks>
/// <para>
/// The CLI connects to the Chronicle server externally via gRPC. The management
/// port is mapped to host port 8081 (instead of the default 8080) to avoid
/// conflicts with a locally running Chronicle instance used during development.
/// Tests pass <c>--management-port 8081</c> to the CLI so that the
/// <c>OAuthTokenProvider</c> reaches <c>https://localhost:8081/connect/token</c>.
/// </para>
/// <para>
/// TLS is configured using a self-signed certificate generated at test startup.
/// The certificate is bind-mounted into the container and referenced via
/// environment variables so the Release-built server can start successfully.
/// </para>
/// <para>
/// In Release mode, the development client credentials are not auto-registered by
/// the server (guarded by <c>#if DEVELOPMENT</c>). This fixture seeds the
/// <c>chronicle-dev-client</c> application directly into MongoDB after the
/// container starts, ensuring CLI tests can authenticate in any configuration.
/// </para>
/// </remarks>
public class ChronicleOutOfProcessFixtureWithLocalImage : ChronicleOutOfProcessFixture
{
    const string CertificatePassword = "TestPassword123";

    /// <summary>
    /// Gets the path to the test certificate file on the host.
    /// </summary>
    public static string CertificatePath { get; }

    static ChronicleOutOfProcessFixtureWithLocalImage()
    {
        var certDir = Path.Combine(Path.GetTempPath(), "chronicle-cli-test-certs");
        CertificatePath = Path.Combine(certDir, "chronicle-test.pfx");
        TestCertificateGenerator.GenerateAndSaveCertificate(CertificatePath, CertificatePassword);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleOutOfProcessFixtureWithLocalImage"/> class.
    /// </summary>
    public ChronicleOutOfProcessFixtureWithLocalImage()
    {
        EnsureDevClientCredentials().GetAwaiter().GetResult();
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

    async Task EnsureDevClientCredentials()
    {
        var mongoUrl = new MongoUrl($"mongodb://localhost:{MongoDBPort}/?replicaSet=rs0&directConnection=true");
        var mongoClient = new MongoClient(mongoUrl);
        var database = mongoClient.GetDatabase(WellKnownDatabaseNames.Chronicle);
        var applicationsCollection = database.GetCollection<BsonDocument>(WellKnownCollectionNames.Applications);

        var existingApp = await applicationsCollection
            .Find(new BsonDocument("clientId", ChronicleConnectionString.DevelopmentClient))
            .FirstOrDefaultAsync();

        if (existingApp is not null)
        {
            return;
        }

        // Wait for the server to create the applications collection (it may still be initializing).
        var timeout = TimeSpan.FromSeconds(30);
        var tcs = new TaskCompletionSource<bool>();
        using var cts = new CancellationTokenSource(timeout);

        // Try to watch for the dev client application being inserted (in case we're running Debug image).
        var insertWatch = Task.Run(async () =>
        {
            try
            {
                var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
                    .Match(change =>
                        change.OperationType == ChangeStreamOperationType.Insert &&
                        change.FullDocument["clientId"] == ChronicleConnectionString.DevelopmentClient);

                using var cursor = await applicationsCollection.WatchAsync(pipeline, cancellationToken: cts.Token);
                if (await cursor.MoveNextAsync(cts.Token) && cursor.Current.Any())
                {
                    tcs.TrySetResult(true);
                }
            }
            catch (OperationCanceledException)
            {
                // Timed out waiting — we'll insert manually.
            }
        });

        // Give the Debug server a few seconds to register the dev client automatically.
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5), cts.Token));
        if (completedTask == tcs.Task)
        {
            await cts.CancelAsync();
            return;
        }

        // Re-check in case it was inserted between the initial check and starting the watch.
        existingApp = await applicationsCollection
            .Find(new BsonDocument("clientId", ChronicleConnectionString.DevelopmentClient))
            .FirstOrDefaultAsync();

        if (existingApp is not null)
        {
            await cts.CancelAsync();
            return;
        }

        // Release mode: manually seed the dev client application.
        var passwordHasher = new PasswordHasher<object>();
        var hashedSecret = passwordHasher.HashPassword(null!, ChronicleConnectionString.DevelopmentClientSecret);

        var application = new BsonDocument
        {
            { "_id", Guid.NewGuid().ToString() },
            { "clientId", ChronicleConnectionString.DevelopmentClient },
            { "clientSecret", hashedSecret },
            { "type", "confidential" },
            { "consentType", "implicit" },
            { "permissions", new BsonArray(["ept:token", "gt:client_credentials", "gt:password", "gt:refresh_token"]) }
        };

        await applicationsCollection.InsertOneAsync(application);
        await cts.CancelAsync();
        Console.WriteLine("Seeded dev client credentials for Release-mode server.");
    }
}
