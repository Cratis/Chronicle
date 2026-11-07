// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_concurrent_requests_use_scoped_checks : Specification
{
    [Fact]
    public async Task should_authorize_each_request_with_its_own_state()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
        builder.Services.AddSingleton(Options.Create(new Configuration.ChronicleOptions()));
        builder.Services.AddSingleton(Substitute.For<IAdministratorPrivileges>());
        builder.Services.AddAuthorization();
        builder.Services.AddChronicleAdministratorAuthorization();
        builder.Services.AddScoped<CallerState>();
        builder.Services.AddScoped<IAdministratorAuthorization, RequestCheck>();
        await using var app = builder.Build();
        app.Run(async context =>
        {
            var name = context.Request.Path.Value!;
            context.RequestServices.GetRequiredService<CallerState>().Name = name;
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], "spec"));
            var authorization = context.RequestServices.GetRequiredService<IAuthorizationService>();
            context.Response.StatusCode = (await authorization.AuthorizeAsync(context.User, context, WellKnownAuthorizationPolicies.Administrator)).Succeeded ? 200 : 403;
        });
        await app.StartAsync();
        using var client = app.GetTestClient();
        var responses = await Task.WhenAll(client.GetAsync("/first"), client.GetAsync("/second"));
        foreach (var response in responses)
        {
            using (response) response.IsSuccessStatusCode.ShouldBeTrue();
        }
    }

    [IgnoreConvention]
    public class CallerState
    {
        public string? Name { get; set; }
    }

    [IgnoreConvention]
    public class RequestCheck(CallerState state) : IAdministratorAuthorization
    {
        public async Task<bool> IsAuthorized(ClaimsPrincipal principal)
        {
            await Task.Yield();
            return state.Name is not null && state.Name == principal.Identity?.Name;
        }
    }
}
