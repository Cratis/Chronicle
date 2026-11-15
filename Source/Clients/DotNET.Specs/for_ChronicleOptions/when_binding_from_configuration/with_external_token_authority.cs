// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleOptions.when_binding_from_configuration;

public class with_external_token_authority : Specification
{
    ChronicleOptions _result;

    void Because()
    {
        var configurationRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Chronicle:Authentication:Authority"] = "https://idp.example.com/realms/chronicle",
                ["Chronicle:Authentication:TokenEndpoint"] = "https://idp.example.com/token",
                ["Chronicle:Authentication:Scope"] = "api://chronicle/.default"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<ChronicleOptions>(configurationRoot.GetSection("Chronicle"));
        _result = services.BuildServiceProvider().GetRequiredService<IOptions<ChronicleOptions>>().Value;
    }

    [Fact] void should_bind_the_authority() => _result.Authentication.Authority.ShouldEqual("https://idp.example.com/realms/chronicle");
    [Fact] void should_bind_the_token_endpoint() => _result.Authentication.TokenEndpoint.ShouldEqual("https://idp.example.com/token");
    [Fact] void should_bind_the_scope() => _result.Authentication.Scope.ShouldEqual("api://chronicle/.default");
}
