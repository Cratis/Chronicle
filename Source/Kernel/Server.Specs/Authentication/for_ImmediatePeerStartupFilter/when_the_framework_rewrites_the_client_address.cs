// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Cratis.Chronicle.Server.Authentication.for_ImmediatePeerStartupFilter;

[Collection(nameof(for_ForwardedHeadersTrust.given.automatic_forwarding_collection))]
public class when_the_framework_rewrites_the_client_address : Specification
{
    IPAddress? _peer;
    IPAddress? _remoteIp;

    async Task Because()
    {
        const string flag = "ASPNETCORE_FORWARDEDHEADERS_ENABLED";
        var previous = Environment.GetEnvironmentVariable(flag);
        Environment.SetEnvironmentVariable(flag, "true");
        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            var options = new Configuration.ChronicleOptions { ForwardedHeaders = new() { KnownProxies = ["192.0.2.10"] } };
            ForwardedHeadersTrust.Configure(builder.Services, options);
            ImmediatePeerStartupFilter.Register(builder.Services);
            await using var app = builder.Build();
            app.Run(async context =>
            {
                _peer = context.Features.Get<IImmediatePeerFeature>()?.Address;
                _remoteIp = context.Connection.RemoteIpAddress;
                await context.Response.StartAsync();
            });

            await app.StartAsync();
            var requestContext = await app.GetTestServer().SendAsync(context =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");
                context.Request.Method = HttpMethods.Get;
                context.Request.Path = "/check";
                context.Request.Headers["X-Forwarded-For"] = "198.51.100.42";
            });
            await requestContext.Response.CompleteAsync();
            await app.StopAsync();
        }
        finally
        {
            Environment.SetEnvironmentVariable(flag, previous);
        }
    }

    [Fact] void should_let_the_framework_rewrite_the_remote_address() => _remoteIp.ShouldEqual(IPAddress.Parse("198.51.100.42"));
    [Fact] void should_record_the_proxy_as_the_immediate_peer() => _peer.ShouldEqual(IPAddress.Parse("192.0.2.10"));
}
