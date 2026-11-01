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
    /// <remarks>
    /// The dev client is registered by the kernel's startup task, which can still be running when the container's
    /// health check already reports healthy - the same race <see cref="ChronicleOutOfProcessFixtureWithLocalImage.GetAccessToken"/>
    /// retries for the administrator. Retry here until the dev client exists or the deadline passes.
    /// </remarks>
    protected override async Task<string> GetAccessToken()
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        HttpResponseMessage response;
        while (true)
        {
            using var request = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = ChronicleConnectionString.DevelopmentClient,
                ["client_secret"] = ChronicleConnectionString.DevelopmentClientSecret
            });
            response = await Client.PostAsync("/connect/token", request);
            if (response.IsSuccessStatusCode || DateTimeOffset.UtcNow >= deadline)
            {
                break;
            }

            response.Dispose();
            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        using (response)
        {
            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadFromJsonAsync<JsonElement>();
            return token.GetProperty("access_token").GetString()!;
        }
    }
}
