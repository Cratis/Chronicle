// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.for_ChronicleOptions.when_binding_from_configuration;

public class with_url_as_string : Specification
{
    ChronicleOptions _result;

    void Because()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Chronicle:Url"] = "chronicle://my-server:8080"
        };

        var configurationRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(configuration)
            .Build();

        _result = new ChronicleOptions();
        configurationRoot.GetSection("Chronicle").Bind(_result);
    }

    [Fact] void should_bind_url_property() => _result.ConnectionString.ShouldNotBeNull();
    [Fact] void should_have_correct_host() => _result.ConnectionString.ServerAddress.Host.ShouldEqual("my-server");
    [Fact] void should_have_correct_port() => _result.ConnectionString.ServerAddress.Port.ShouldEqual(8080);
}
