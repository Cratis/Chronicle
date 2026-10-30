// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_TelemetryConfiguration;

[Collection(nameof(telemetry_environment))]
public class when_resource_attributes_define_identity : given.a_telemetry_configuration
{
    void Establish() => Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "service.name=resource-kernel,service.instance.id=kernel-42");

    void Because()
    {
        _services.AddOpenTelemetry().WithCratis(_configuration, TelemetryConfiguration.Configure);
        ResolveProviders();
    }

    [Fact] void should_preserve_the_service_name() => _traces.GetResource().Attributes.Single(_ => _.Key == "service.name").Value.ShouldEqual("resource-kernel");
    [Fact] void should_preserve_the_instance_id() => _traces.GetResource().Attributes.Single(_ => _.Key == "service.instance.id").Value.ShouldEqual("kernel-42");
}
