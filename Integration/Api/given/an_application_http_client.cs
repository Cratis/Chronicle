// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using System.Text.Json;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Integration.Api.given;

/// <summary>
/// Represents an HTTP client authenticated as an application rather than an administrator.
/// </summary>
/// <param name="fixture">The out-of-process fixture.</param>
public class an_application_http_client(ChronicleOutOfProcessFixtureWithLocalImage fixture) : an_http_client(fixture)
{
    /// <inheritdoc/>
    protected override async Task<string> GetAccessToken()
    {
        var clientId = $"api-spec-{Guid.NewGuid():N}";
        var clientSecret = Guid.NewGuid().ToString("N");
        await RegisterApplication(clientId, clientSecret);

        using var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        });
        using var response = await Client.PostAsync("/connect/token", request);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<JsonElement>();
        return token.GetProperty("access_token").GetString()!;
    }

    async Task RegisterApplication(string clientId, string clientSecret)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var mongoClient = new MongoClient($"mongodb://{MongoDBContainer.Hostname}:{MongoDBContainer.GetMappedPublicPort(27017)}/?replicaSet=rs0&directConnection=true");
        var applications = mongoClient.GetDatabase(WellKnownDatabaseNames.Chronicle).GetCollection<BsonDocument>("applications");
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
            .Match(change => change.OperationType == ChangeStreamOperationType.Insert && change.FullDocument["clientId"] == clientId);

        // Subscribe before registration so the asynchronous application reactor cannot outrun the readiness signal.
        using var changes = await applications.WatchAsync(pipeline, cancellationToken: deadline.Token);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await base.GetAccessToken());
        try
        {
            var result = await Client.ExecuteCommand("/api/security/add-application", new AddApplication(Guid.NewGuid(), clientId, clientSecret));
            result!.IsSuccess.ShouldBeTrue();
            while (await changes.MoveNextAsync(deadline.Token))
            {
                if (changes.Current.Any())
                {
                    return;
                }
            }

            throw new InvalidOperationException("Application registration ended before the application was available.");
        }
        finally
        {
            Client.DefaultRequestHeaders.Authorization = null;
        }
    }
}
