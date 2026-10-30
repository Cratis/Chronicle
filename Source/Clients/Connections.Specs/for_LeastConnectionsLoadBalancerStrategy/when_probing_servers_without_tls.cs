// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Connections.for_LeastConnectionsLoadBalancerStrategy;

public class when_probing_servers_without_tls : Specification
{
    readonly List<HttpRequestMessage> _requests = [];
    LeastConnectionsLoadBalancerStrategy _strategy;
    HttpClient _client;

    void Establish()
    {
        _client = new HttpClient(new RecordingHandler(_requests));
        _strategy = new LeastConnectionsLoadBalancerStrategy(skipTlsValidation: true, _client, maxSelectionJitter: TimeSpan.Zero, disableTls: true);
    }

    void Destroy() => _strategy.Dispose();

    Task Because() => _strategy.Next([new ChronicleServerAddress("host1"), new ChronicleServerAddress("host2")]);

    [Fact] void should_probe_over_http() => _requests.ShouldEachConformTo(_ => _.RequestUri!.Scheme == Uri.UriSchemeHttp);
    [Fact] void should_ask_for_http2_outright() => _client.DefaultRequestVersion.ShouldEqual(HttpVersion.Version20);
    [Fact] void should_not_fall_back_to_http1() => _client.DefaultVersionPolicy.ShouldEqual(HttpVersionPolicy.RequestVersionExact);

    sealed class RecordingHandler(List<HttpRequestMessage> requests) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("0") });
        }
    }
}
