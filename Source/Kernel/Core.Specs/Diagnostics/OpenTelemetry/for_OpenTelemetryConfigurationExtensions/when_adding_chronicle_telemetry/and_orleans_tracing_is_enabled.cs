// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_OpenTelemetryConfigurationExtensions.when_adding_chronicle_telemetry;

[Collection(nameof(given.orleans_activity_source_collection))]
public class and_orleans_tracing_is_enabled : Specification
{
    IServiceCollection _services;
    ChronicleOptions _options;

    void Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Chronicle:Telemetry:OrleansTracing"] = "true"
        }).Build();
        _options = configuration.GetSection(ChronicleOptions.SectionPath).Get<ChronicleOptions>();
        _services = new ServiceCollection().AddChronicleTelemetry(configuration);
    }

    [Fact] void should_bind_the_orleans_tracing_option() => _options.Telemetry.OrleansTracing.ShouldBeTrue();
    [Fact] void should_not_register_the_propagation_only_listener() => _services.Any(descriptor => descriptor.ServiceType == typeof(IHostedService) && descriptor.ImplementationType == typeof(OrleansTraceContextPropagation)).ShouldBeFalse();
}
