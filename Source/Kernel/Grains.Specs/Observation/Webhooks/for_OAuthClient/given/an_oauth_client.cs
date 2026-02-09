// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
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
        _messageHandler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(tokenResponse)
        });
    }

    protected void SetupSuccessfulDiscoveryResponse(string tokenEndpoint)
    {
        var discoveryResponse = new
        {
            token_endpoint = tokenEndpoint
        };
        _messageHandler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(discoveryResponse)
        });
    }

    protected class FakeHttpMessageHandler : HttpMessageHandler
    {
        readonly Queue<HttpResponseMessage> _responses = new();

        public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK);
        public List<HttpRequestMessage> Requests { get; } = [];

        public void EnqueueResponse(HttpResponseMessage response) => _responses.Enqueue(response);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_responses.Count > 0 ? _responses.Dequeue() : Response);
        }
    }
}
