// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;

namespace Cratis.Chronicle.Grains.Observation.Webhooks.for_OAuthClient.given;

public class an_oauth_client : Specification
{
    protected OAuthClient _client;
    protected IHttpClientFactory _httpClientFactory;
    protected HttpClient _httpClient;
    protected FakeHttpMessageHandler _messageHandler;

    void Establish()
    {
        _messageHandler = new FakeHttpMessageHandler();
        _httpClient = new HttpClient(_messageHandler);
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _httpClientFactory.CreateClient().Returns(_httpClient);

        _client = new OAuthClient(_httpClientFactory);
    }

    protected void SetupSuccessfulTokenResponse(string accessToken, int expiresIn)
    {
        var tokenResponse = new
        {
            access_token = accessToken,
            expires_in = expiresIn,
            token_type = "Bearer"
        };
        _messageHandler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(tokenResponse)
        };
    }

    protected class FakeHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Response);
        }
    }
}
