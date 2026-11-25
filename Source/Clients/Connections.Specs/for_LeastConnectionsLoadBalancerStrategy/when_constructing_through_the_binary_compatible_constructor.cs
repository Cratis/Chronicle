// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;

namespace Cratis.Chronicle.Connections.for_LeastConnectionsLoadBalancerStrategy;

public class when_constructing_through_the_binary_compatible_constructor : Specification
{
    readonly List<HttpRequestMessage> _requests = [];
    ConstructorInfo _constructor;
    LeastConnectionsLoadBalancerStrategy _strategy;
    HttpClient _client;
    ChronicleServerAddress _selected;

    void Establish()
    {
        _constructor = typeof(LeastConnectionsLoadBalancerStrategy).GetConstructor([typeof(bool), typeof(HttpClient), typeof(TimeSpan?)])!;
        _client = new HttpClient(new RecordingHandler(_requests));
    }

    async Task Because()
    {
        _constructor.ShouldNotBeNull();
        _strategy = (LeastConnectionsLoadBalancerStrategy)_constructor.Invoke([true, _client, TimeSpan.Zero]);
        _selected = await _strategy.Next([new ChronicleServerAddress("host1"), new ChronicleServerAddress("host2")]);
    }

    void Destroy()
    {
        _strategy?.Dispose();
        _client.Dispose();
    }

    [Fact] void should_require_all_three_parameters() => _constructor.GetParameters().ShouldEachConformTo(_ => !_.IsOptional);
    [Fact] void should_probe_and_reserve_over_https() => _requests.ShouldEachConformTo(_ => _.RequestUri!.Scheme == Uri.UriSchemeHttps);
    [Fact] void should_probe_both_servers_and_reserve_the_selected_server() => _requests.Count.ShouldEqual(3);
    [Fact] void should_select_the_least_loaded_server() => _selected.Host.ShouldEqual("host1");
    [Fact] void should_not_require_http2_without_negotiation() => _client.DefaultVersionPolicy.ShouldEqual(HttpVersionPolicy.RequestVersionOrLower);
    [Fact] void should_keep_the_current_optional_defaults() => typeof(LeastConnectionsLoadBalancerStrategy).GetConstructor([typeof(bool), typeof(HttpClient), typeof(TimeSpan?), typeof(bool)])!.GetParameters().Skip(1).Select(_ => _.DefaultValue).ShouldEqual([null, null, false]);

    sealed class RecordingHandler(List<HttpRequestMessage> requests) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(request.RequestUri!.Host == "host1" ? "0" : "1") });
        }
    }
}
