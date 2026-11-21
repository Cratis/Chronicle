// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

internal sealed class TokenCacheEndpointHandler : HttpMessageHandler
{
    internal string TokenEndpoint { get; set; } = "https://idp.example/oauth/token";

    internal string AccessToken { get; set; } = "acquired-token";

    internal int DiscoveryRequests { get; private set; }

    internal int TokenRequests { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string response;
        if (request.Method == HttpMethod.Get)
        {
            DiscoveryRequests++;
            response = JsonSerializer.Serialize(new { token_endpoint = TokenEndpoint });
        }
        else
        {
            TokenRequests++;
            response = JsonSerializer.Serialize(new { access_token = AccessToken, expires_in = 3600 });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response) });
    }
}
