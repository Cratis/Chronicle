// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

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

        // Retry logic with exponential backoff
        const int maxRetries = 10;
        var retryDelayMs = 500;
        Exception? lastException = null;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var response = await httpClient.PostAsync("https://localhost:8081/connect/token", tokenRequest);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorContent}");
                }

                var tokenResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                _accessToken = tokenResponse.GetProperty("access_token").GetString()!;

                return _accessToken;
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt < maxRetries - 1)
                {
                    Console.WriteLine($"Failed to get access token (attempt {attempt + 1}/{maxRetries}): {ex.Message}. Retrying in {retryDelayMs}ms...");
                    await Task.Delay(retryDelayMs);
                    retryDelayMs *= 2;
                }
            }
        }

        throw new InvalidOperationException($"Failed to get access token after {maxRetries} attempts. Last error: {lastException?.Message}", lastException);
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
            .WithEnvironment("Cratis__Chronicle__Authentication__EncryptionCertificate__CertificatePath", "/app/certs/chronicle.pfx")
            .WithEnvironment("Cratis__Chronicle__Authentication__EncryptionCertificate__CertificatePassword", CertificatePassword);
}
