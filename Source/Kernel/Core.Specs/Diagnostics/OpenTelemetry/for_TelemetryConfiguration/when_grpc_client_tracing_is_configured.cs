// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_TelemetryConfiguration;

[Collection(nameof(telemetry_environment))]
public class when_grpc_client_tracing_is_configured : given.a_telemetry_configuration
{
    void Establish() => _configuration["Cratis:Chronicle:Telemetry:GrpcClientTracing"] = "true";

    void Because()
    {
        _services.AddOpenTelemetry().WithCratis(_configuration, options => TelemetryConfiguration.Configure(options, _configuration));
        ResolveProviders();
    }

    [Fact]
    void should_listen_to_grpc_client_activities()
    {
        using var source = new DiagnosticListener("Grpc.Net.Client");
        source.IsEnabled().ShouldBeTrue();
    }
}
