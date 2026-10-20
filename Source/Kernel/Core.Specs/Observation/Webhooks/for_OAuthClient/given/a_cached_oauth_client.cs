// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.given;

public class a_cached_oauth_client : Specification
{
    protected OAuthClient _client;
    protected IHttpClientFactory _factory;
    protected OAuthAuthorization _authorization;
    protected Clock _clock;
    protected Endpoint _endpoint;
    protected TaskCompletionSource _release;
    protected int _lifetime = 3600;

    void Establish()
    {
        _clock = new Clock();
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _endpoint = new Endpoint(this);
        _factory = Substitute.For<IHttpClientFactory>();
        _factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(_endpoint, false));
        _client = new OAuthClient(_factory, _clock);
        _authorization = new("https://authority.example", "client", "secret");
    }

    protected class Clock : TimeProvider
    {
        DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan time) => _now += time;
    }

    protected class Endpoint(a_cached_oauth_client fixture) : HttpMessageHandler
    {
        public int TokenRequests;
        public int DiscoveryRequests;
        public int DiscoveryTimeouts;
        public string Form = "";
        public bool Block;
        public bool Fail;
        public List<string?> SentTokens = [];
        public List<string> Bodies = [];
        public List<bool> Headers = [];
        public List<HttpMethod> Methods = [];
        public Queue<HttpStatusCode> Responses = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.Host == "target.example")
            {
                SentTokens.Add(request.Headers.Authorization?.Parameter);
                Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
                Headers.Add(request.Headers.Contains("X-Test"));
                Methods.Add(request.Method);
                return new(Responses.TryDequeue(out var status) ? status : HttpStatusCode.OK);
            }
            if (request.Method == HttpMethod.Get)
            {
                DiscoveryRequests++;
                if (DiscoveryTimeouts > 0)
                {
                    DiscoveryTimeouts--;
                    throw new TaskCanceledException("Discovery timed out.");
                }
                return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { token_endpoint = "https://authority.example/token" }) };
            }
            var number = Interlocked.Increment(ref TokenRequests);
            Form = await request.Content!.ReadAsStringAsync(cancellationToken);
            if (Block)
            {
                await fixture._release.Task.WaitAsync(cancellationToken);
            }
            return new(Fail ? HttpStatusCode.BadRequest : HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { access_token = $"token-{number}", expires_in = fixture._lifetime })
            };
        }
    }
}
