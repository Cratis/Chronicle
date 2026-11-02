// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_binding_configuration;

public class and_grpc_client_tracing_is_enabled : Specification
{
    ChronicleOptions _options;

    void Establish()
    {
        _options = new ChronicleOptions();
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cratis:Chronicle:Telemetry:GrpcClientTracing"] = "true"
            })
            .Build()
            .GetSection(ChronicleOptions.SectionPath)
            .Bind(_options);
    }

    [Fact] void should_enable_grpc_client_tracing() => _options.Telemetry.GrpcClientTracing.ShouldBeTrue();
}
