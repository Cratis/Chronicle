// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider;

public class FakeTokenEndpointHandler : HttpMessageHandler
{
    public int Requests { get; private set; }
    public bool Fail { get; set; }
    public string Token { get; set; } = "the-token";
    public int ExpiresIn { get; set; } = 3600;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests++;
        if (Fail)
        {
            throw new HttpRequestException("Simulated token endpoint failure");
        }

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"{{\"access_token\":\"{Token}\",\"expires_in\":{ExpiresIn}}}")
        };

        return Task.FromResult(response);
    }
}
