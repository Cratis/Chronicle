// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleOptions;

public class when_cloning_the_default_connection_for_configuration : Specification
{
    ServiceProvider _provider;
    ChronicleConnectionString _configured;
    string _defaultRaw;
    string _configuredRaw;

    void Establish() => _defaultRaw = ChronicleConnectionString.Development.ToString();

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddOptions<ChronicleOptions>()
            .WithChronicleConfiguration("Chronicle", "Chronicle")
            .Configure(options => _configured = options.ConnectionString);
        _provider = services.BuildServiceProvider();
        _ = _provider.GetRequiredService<IOptions<ChronicleOptions>>().Value;
        _configuredRaw = _configured.ToString();
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_copy_raw_default_text_before_post_configuration() => _configuredRaw.ShouldEqual(_defaultRaw);
    [Fact] void should_create_an_independent_connection_before_post_configuration() => ReferenceEquals(_configured, ChronicleConnectionString.Development).ShouldBeFalse();
    [Fact] void should_leave_the_shared_default_unchanged() => ChronicleConnectionString.Development.ToString().ShouldEqual(_defaultRaw);
}
