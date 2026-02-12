// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.MongoDB;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Integration.Api;

/// <summary>
/// Represents a fixture for the Chronicle Out of Process integration tests using a locally built image.
/// </summary>
public class ChronicleOutOfProcessFixtureWithLocalImage : ChronicleOutOfProcessFixture
{
    const string CertificatePassword = "TestPassword123";
    const string DefaultAdminUsername = "admin";
    const string DefaultAdminPassword = "ChangeMeNow!";

    string? _accessToken;
    readonly ILogger<ChronicleOutOfProcessFixtureWithLocalImage> _logger;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleOutOfProcessFixtureWithLocalImage"/> class.
    /// </summary>
    public ChronicleOutOfProcessFixtureWithLocalImage()
    {
        _logger = LoggerFactory.CreateLogger<ChronicleOutOfProcessFixtureWithLocalImage>();
    }

    /// <summary>
    /// Gets the bearer token for authenticating API requests.
    /// </summary>
    /// <returns>The access token.</returns>
    /// <exception cref="InvalidOperationException">Thrown when unable to retrieve the access token after multiple attempts.</exception>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
    public async Task<string> GetAccessToken()
    {
        if (!string.IsNullOrEmpty(_accessToken))
        {
            return _accessToken;
        }

        using var handler = new HttpClientHandler();
#pragma warning disable MA0039 // Do not write your own certificate validation method
        handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
#pragma warning restore MA0039 // Do not write your own certificate validation method

        using var httpClient = new HttpClient(handler);
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        using var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = DefaultAdminUsername,
            ["password"] = DefaultAdminPassword
        });

        var response = await httpClient.PostAsync("https://localhost:8081/connect/token", tokenRequest);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogAttemptingToAuthenticateFailed();
            await EnsureAdminUserHasPassword();

            using var retryTokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = DefaultAdminUsername,
                ["password"] = DefaultAdminPassword
            });

            response = await httpClient.PostAsync("https://localhost:8081/connect/token", retryTokenRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorContent}");
            }
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
        _accessToken = tokenResponse.GetProperty("access_token").GetString()!;

        return _accessToken;
    }

    async Task EnsureAdminUserHasPassword()
    {
        var mongoUrl = new MongoUrl($"mongodb://localhost:{MongoDBPort}/?replicaSet=rs0&directConnection=true");
        var mongoClient = new MongoClient(mongoUrl);
        var database = mongoClient.GetDatabase(WellKnownDatabaseNames.Chronicle);
        var usersCollection = database.GetCollection<BsonDocument>("users");

        var existingUser = await usersCollection.Find(new BsonDocument("username", DefaultAdminUsername)).FirstOrDefaultAsync();
        if (existingUser is null)
        {
            // Watch for user creation
            var tcs = new TaskCompletionSource<BsonDocument>();
            var timeout = TimeSpan.FromSeconds(30);
            using var cts = new CancellationTokenSource(timeout);

            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
                .Match(change =>
                    change.OperationType == ChangeStreamOperationType.Insert &&
                    change.FullDocument["username"] == DefaultAdminUsername);

            _logger.LogWaitingForAdminUserToBeCreated();

            var watchTask = Task.Run(async () =>
            {
                using var cursor = await usersCollection.WatchAsync(pipeline, cancellationToken: cts.Token);
                await cursor.MoveNextAsync(cts.Token);
                var change = cursor.Current.FirstOrDefault();
                if (change is not null)
                {
                    tcs.TrySetResult(change.FullDocument);
                }
            });

            try
            {
                existingUser = await tcs.Task.WaitAsync(timeout);
            }
            catch (TimeoutException)
            {
                throw new InvalidOperationException("Admin user was not created within the expected timeframe.");
            }
            catch (OperationCanceledException)
            {
                throw new InvalidOperationException("Admin user was not created within the expected timeframe.");
            }
        }

        // Check if password is already set (handle BsonNull case)
        if (existingUser.Contains("passwordHash") &&
            existingUser["passwordHash"] is not BsonNull &&
            !string.IsNullOrEmpty(existingUser["passwordHash"].AsString))
        {
            _logger.LogAdminUserAlreadyHasPassword();
            return;
        }

        _logger.LogAdminUserExistsSettingPassword();

        var passwordHasher = new PasswordHasher<object>();
        var passwordHash = passwordHasher.HashPassword(null!, DefaultAdminPassword);
        await SetUserPassword(usersCollection, passwordHash);
    }

    async Task SetUserPassword(IMongoCollection<BsonDocument> usersCollection, string passwordHash)
    {
        var update = Builders<BsonDocument>.Update
            .Set("passwordHash", passwordHash)
            .Set("isActive", true)
            .Set("requiresPasswordChange", false)
            .Set("lastModifiedAt", new BsonDateTime(DateTime.UtcNow));

        var result = await usersCollection.UpdateOneAsync(
            new BsonDocument("username", DefaultAdminUsername),
            update);

        if (result.ModifiedCount > 0)
        {
            _logger.LogPasswordSetSuccessfully();
        }
        else
        {
            _logger.LogFailedToSetPassword();
        }
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
