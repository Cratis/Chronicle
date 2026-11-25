// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.given;

public class a_controlled_oauth_client : Specification
{
    protected OAuthClient _client;
    protected OAuthAuthorization _authorization;
    protected IWebhookTargetPolicy _policy;
    protected controlled_endpoint _endpoint;
    protected controlled_clock _clock;
    protected List<string> _calls;
    HttpClient _httpClient;

    void Establish()
    {
        _calls = [];
        _endpoint = new controlled_endpoint(_calls);
        _httpClient = new HttpClient(_endpoint);
        _clock = new controlled_clock();
        _policy = Substitute.For<IWebhookTargetPolicy>();
        _policy.EvaluateForDelivery(Arg.Any<string>(), Arg.Any<Concepts.EventStoreName?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _calls.Add($"validate:{call.ArgAt<string>(0)}");
            return WebhookTargetValidation.Allowed;
        });
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(OAuthClient.HttpClientName).Returns(_ => new HttpClient(_endpoint, false));
        _client = new OAuthClient(factory, _policy, _clock);
        _authorization = new OAuthAuthorization("https://authority.example", "client", "secret");
    }

    void Destroy() => _httpClient.Dispose();

    protected static HttpResponseMessage Discovery(string endpoint = "https://authority.example/token") =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { token_endpoint = endpoint })) };

    protected static HttpResponseMessage Token(int lifetime = 3600) =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { access_token = "token", expires_in = lifetime })) };

    protected class controlled_endpoint(List<string> calls) : HttpMessageHandler
    {
        public Queue<Func<Task<HttpResponseMessage>>> Responses { get; } = new();
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            calls.Add($"send:{request.RequestUri}");
            return Responses.Dequeue()();
        }
    }

    protected class controlled_clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
