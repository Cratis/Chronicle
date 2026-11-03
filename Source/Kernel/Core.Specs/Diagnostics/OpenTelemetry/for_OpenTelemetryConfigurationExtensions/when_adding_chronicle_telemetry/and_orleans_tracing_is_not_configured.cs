// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_OpenTelemetryConfigurationExtensions.when_adding_chronicle_telemetry;

[Collection(nameof(given.orleans_activity_source_collection))]
public class and_orleans_tracing_is_not_configured : Specification
{
    IServiceCollection _services;
    ChronicleOptions _options;

    void Because()
    {
        var configuration = new ConfigurationBuilder().Build();
        _options = configuration.GetSection(ChronicleOptions.SectionPath).Get<ChronicleOptions>() ?? new();
        _services = new ServiceCollection().AddChronicleTelemetry(configuration);
    }

    [Fact] void should_disable_orleans_tracing_by_default() => _options.Telemetry.OrleansTracing.ShouldBeFalse();
    [Fact] void should_register_context_propagation() => _services.Any(descriptor => descriptor.ServiceType == typeof(IHostedService) && descriptor.ImplementationType == typeof(OrleansTraceContextPropagation)).ShouldBeTrue();
}
