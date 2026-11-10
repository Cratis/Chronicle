// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Server.Authentication.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_requesting_a_token_with_authentication_disabled : Specification
{
    WebApplication _app;
    HttpStatusCode _tokenStatus;
    HttpStatusCode _unrelatedStatus;

    async Task Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IDataProtectionKeys>(Arg.Any<string>()).Returns(Substitute.For<IDataProtectionKeys>());
        builder.Services.AddSingleton(grainFactory);
        builder.Services.AddSingleton(Substitute.For<Storage.ISystemStorage>());
        builder.Services.AddChronicleAuthentication(new Configuration.ChronicleOptions
        {
            Storage = new() { Type = Storage.StorageType.InMemory },
            Authentication = new Configuration.Authentication { Enabled = false }
        });
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(AuthorizationController).Assembly)
            .AddApplicationPart(typeof(given.UnrelatedController).Assembly);

        _app = builder.Build();
        _app.UseRouting();
        _app.UseAuthorization();
        _app.MapControllers();
        await _app.StartAsync();
    }

    async Task Because()
    {
        using var client = _app.GetTestClient();
        using var token = await client.PostAsync(
            "/connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "spec-client",
                ["client_secret"] = "spec-secret"
            }));
        _tokenStatus = token.StatusCode;

        using var unrelated = await client.GetAsync("/specs/unrelated");
        _unrelatedStatus = unrelated.StatusCode;
    }

    async Task Destroy() => await _app.DisposeAsync();

    [Fact] void should_answer_not_found_for_the_token_endpoint() => _tokenStatus.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_still_serve_other_controllers() => _unrelatedStatus.ShouldEqual(HttpStatusCode.OK);
}
