// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_TelemetryConfiguration;

[Collection(nameof(telemetry_environment))]
public class when_no_endpoint_is_configured : given.a_telemetry_configuration
{
    void Because()
    {
        _services.AddOpenTelemetry().WithCratis(_configuration, TelemetryConfiguration.Configure);
        ResolveProviders();
    }

    [Fact] void should_not_create_otlp_exporters() => _exporterOptions.ShouldBeEmpty();
    [Fact] void should_default_to_chronicle() => _traces.GetResource().Attributes.Single(_ => _.Key == "service.name").Value.ShouldEqual("Chronicle");
    [Fact]
    void should_collect_chronicle_traces()
    {
        using var source = new ActivitySource(ChronicleActivity.SourceName);
        source.HasListeners().ShouldBeTrue();
    }
    [Fact] void should_include_formatted_log_messages() => _provider.GetRequiredService<IOptions<OpenTelemetryLoggerOptions>>().Value.IncludeFormattedMessage.ShouldBeTrue();
    [Fact] void should_include_log_scopes() => _provider.GetRequiredService<IOptions<OpenTelemetryLoggerOptions>>().Value.IncludeScopes.ShouldBeTrue();
}
