// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_TelemetryConfiguration.when_environment_defines_identity;

[Collection(nameof(telemetry_environment))]
public class and_instance_id_is_present : given.a_telemetry_configuration
{
    void Establish()
    {
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "deployed-kernel");
        Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "service.name=resource-name,service.instance.id=kernel-42");
    }

    void Because()
    {
        _services.AddOpenTelemetry().WithCratis(_configuration, TelemetryConfiguration.Configure);
        ResolveProviders();
    }

    [Fact] void should_preserve_the_trace_service_name() => _traces.GetResource().Attributes.Single(_ => _.Key == "service.name").Value.ShouldEqual("deployed-kernel");
    [Fact] void should_preserve_the_metric_service_name() => _metrics.GetResource().Attributes.Single(_ => _.Key == "service.name").Value.ShouldEqual("deployed-kernel");
    [Fact] void should_preserve_the_log_service_name() => _logs.GetResource().Attributes.Single(_ => _.Key == "service.name").Value.ShouldEqual("deployed-kernel");
    [Fact] void should_preserve_the_trace_instance_id() => _traces.GetResource().Attributes.Single(_ => _.Key == "service.instance.id").Value.ShouldEqual("kernel-42");
    [Fact] void should_preserve_the_metric_instance_id() => _metrics.GetResource().Attributes.Single(_ => _.Key == "service.instance.id").Value.ShouldEqual("kernel-42");
    [Fact] void should_preserve_the_log_instance_id() => _logs.GetResource().Attributes.Single(_ => _.Key == "service.instance.id").Value.ShouldEqual("kernel-42");
}
