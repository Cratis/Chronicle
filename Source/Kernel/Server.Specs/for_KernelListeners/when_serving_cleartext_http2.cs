// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Server.for_KernelListeners;

public class when_serving_cleartext_http2 : Specification
{
    WebApplication _app;
    HttpClient _client;
    HttpResponseMessage _http2Response;
    HttpResponseMessage? _http1Response;
    HttpRequestException? _http1Failure;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel(kestrel => KernelListeners.Configure(
            new() { Tls = new() { Enabled = false }, Authentication = new() { Enabled = false } },
            null,
            NullLogger<Kernel>.Instance,
            (_, protocols, _) => kestrel.Listen(IPAddress.Loopback, 0, listen => listen.Protocols = protocols)));
        _app = builder.Build();
        _app.MapGet("/health", () => "ok");
        _client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    async Task Because()
    {
        await _app.StartAsync();
        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var http2Request = new HttpRequestMessage(HttpMethod.Get, $"{address}/health")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact
        };
        _http2Response = await _client.SendAsync(http2Request);

        using var http1Request = new HttpRequestMessage(HttpMethod.Get, $"{address}/health")
        {
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact
        };
        try
        {
            _http1Response = await _client.SendAsync(http1Request);
        }
        catch (HttpRequestException exception)
        {
            _http1Failure = exception;
        }
    }

    [Fact] void should_serve_h2c() => _http2Response.Version.ShouldEqual(HttpVersion.Version20);
    [Fact] void should_answer_http2_requests() => _http2Response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_refuse_http1_requests() => (_http1Failure is not null || _http1Response?.IsSuccessStatusCode == false).ShouldBeTrue();

    async Task Destroy()
    {
        _http2Response?.Dispose();
        _http1Response?.Dispose();
        _client?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
