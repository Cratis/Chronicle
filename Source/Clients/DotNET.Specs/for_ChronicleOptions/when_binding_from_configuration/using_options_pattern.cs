// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleOptions.when_binding_from_configuration;

public class using_options_pattern : Specification
{
    ChronicleOptions _result;

    void Because()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Chronicle:ConnectionString"] = "chronicle://options-server:7070"
        };

        var configurationRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(configuration)
            .Build();

        var services = new ServiceCollection();
        services.Configure<ChronicleOptions>(configurationRoot.GetSection("Chronicle"));

        var serviceProvider = services.BuildServiceProvider();
        _result = serviceProvider.GetRequiredService<IOptions<ChronicleOptions>>().Value;
    }

    [Fact] void should_bind_url_property() => _result.ConnectionString.ShouldNotBeNull();
    [Fact] void should_have_correct_host() => _result.ConnectionString.ServerAddress.Host.ShouldEqual("options-server");
    [Fact] void should_have_correct_port() => _result.ConnectionString.ServerAddress.Port.ShouldEqual(7070);
}
