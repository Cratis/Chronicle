// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Server.for_ForwardedHeadersTrust.given;

public class a_host_with_automatic_forwarding : Specification
{
    protected string? _scheme;
    protected IPAddress? _remoteIp;
    protected bool _automaticFilterRegistered;

    protected async Task Send(
        Configuration.ChronicleOptions options,
        string remoteIp,
        string forwardedFor,
        string forwardedProto)
    {
        const string flag = "ASPNETCORE_FORWARDEDHEADERS_ENABLED";
        var previous = Environment.GetEnvironmentVariable(flag);
        Environment.SetEnvironmentVariable(flag, "true");
        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            ForwardedHeadersTrust.Configure(builder.Services, options);
            await using var app = builder.Build();
            _automaticFilterRegistered = app.Services.GetServices<IStartupFilter>()
                .Any(_ => _.GetType().Name.Contains("ForwardedHeadersStartupFilter", StringComparison.Ordinal));
            ForwardedHeadersTrust.Use(app, builder.Configuration);
            app.Run(async context =>
            {
                _scheme = context.Request.Scheme;
                _remoteIp = context.Connection.RemoteIpAddress;
                await context.Response.StartAsync();
            });

            await app.StartAsync();
            var requestContext = await app.GetTestServer().SendAsync(context =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
                context.Request.Method = HttpMethods.Get;
                context.Request.Path = "/check";
                context.Request.Scheme = "http";
                context.Request.Headers["X-Forwarded-Proto"] = forwardedProto;
                context.Request.Headers["X-Forwarded-For"] = forwardedFor;
            });
            await requestContext.Response.CompleteAsync();
            await app.StopAsync();
        }
        finally
        {
            Environment.SetEnvironmentVariable(flag, previous);
        }
    }
}
