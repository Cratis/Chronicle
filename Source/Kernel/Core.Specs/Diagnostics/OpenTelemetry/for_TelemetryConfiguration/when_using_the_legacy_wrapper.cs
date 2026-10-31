// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_TelemetryConfiguration;

[Collection(nameof(telemetry_environment))]
public class when_using_the_legacy_wrapper : given.a_telemetry_configuration
{
    IServiceCollection _result;

    void Establish()
    {
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "deployed-kernel");
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "service.instance.id=kernel-42");
    }

    void Because()
    {
        _result = _services.AddChronicleTelemetry(_configuration, "legacy-fallback");
        ResolveProviders();
    }

    [Fact] void should_return_the_service_collection() => _result.ShouldBeSame(_services);
    [Fact] void should_not_have_a_compiler_obsolete_attribute() => typeof(OpenTelemetryConfigurationExtensions).GetMethod(nameof(OpenTelemetryConfigurationExtensions.AddChronicleTelemetry)).GetCustomAttribute<ObsoleteAttribute>().ShouldBeNull();
    [Fact] void should_preserve_the_service_name() => _traces.GetResource().Attributes.Single(_ => _.Key == "service.name").Value.ShouldEqual("deployed-kernel");
    [Fact] void should_preserve_the_instance_id() => _traces.GetResource().Attributes.Single(_ => _.Key == "service.instance.id").Value.ShouldEqual("kernel-42");
    [Fact] void should_not_create_otlp_exporters_without_an_endpoint() => _exporterOptions.ShouldBeEmpty();
    [Fact] void should_include_formatted_messages() => _provider.GetRequiredService<IOptions<OpenTelemetryLoggerOptions>>().Value.IncludeFormattedMessage.ShouldBeTrue();
    [Fact] void should_include_scopes() => _provider.GetRequiredService<IOptions<OpenTelemetryLoggerOptions>>().Value.IncludeScopes.ShouldBeTrue();
}
