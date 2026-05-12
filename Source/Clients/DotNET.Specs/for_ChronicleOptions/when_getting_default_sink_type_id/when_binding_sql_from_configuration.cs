// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sinks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleOptions.when_getting_default_sink_type_id;

public class when_binding_sql_from_configuration : Specification
{
    ChronicleOptions _result;

    void Because()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Chronicle:DefaultSinkTypeId"] = WellKnownSinkTypes.SQL.Value.ToString()
        };

        var configurationRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(configuration)
            .Build();

        ConceptTypeConvertersRegistrar.EnsureFor(typeof(WellKnownSinkTypes).Assembly);

        var services = new ServiceCollection();
        services.Configure<ChronicleOptions>(configurationRoot.GetSection("Chronicle"));
        var serviceProvider = services.BuildServiceProvider();
        _result = serviceProvider.GetRequiredService<IOptions<ChronicleOptions>>().Value;
    }

    [Fact] void should_bind_default_sink_type_id() => _result.DefaultSinkTypeId.ShouldEqual(WellKnownSinkTypes.SQL);
}
