// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_OpenTelemetryConfigurationExtensions.when_adding_chronicle_telemetry;

public class and_a_custom_health_check_endpoint_is_configured : Specification
{
    ServiceProvider _provider;
    AspNetCoreTraceInstrumentationOptions _options;
    bool _health;
    bool _ordinary;

    void Establish()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cratis:Chronicle:HealthCheckEndpoint"] = "/probe"
            })
            .Build();
        _provider = new ServiceCollection().AddChronicleTelemetry(configuration).BuildServiceProvider();
        _options = _provider.GetRequiredService<IOptionsMonitor<AspNetCoreTraceInstrumentationOptions>>().Get(Options.DefaultName);
    }

    void Because()
    {
        var health = new DefaultHttpContext();
        health.Request.Path = "/probe/ready";
        var ordinary = new DefaultHttpContext();
        ordinary.Request.Path = "/api/x";
        _health = _options.Filter(health);
        _ordinary = _options.Filter(ordinary);
    }

    [Fact] void should_not_trace_the_health_check() => _health.ShouldBeFalse();
    [Fact] void should_trace_an_ordinary_request() => _ordinary.ShouldBeTrue();

    void Destroy() => _provider.Dispose();
}
