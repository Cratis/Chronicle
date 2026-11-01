// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;

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
        using var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = ChronicleConnectionString.DevelopmentClient,
            ["client_secret"] = ChronicleConnectionString.DevelopmentClientSecret
        });
        using var response = await Client.PostAsync("/connect/token", request);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<JsonElement>();
        return token.GetProperty("access_token").GetString()!;
    }
}
