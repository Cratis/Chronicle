// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleBuilder;

public class when_configuring_telemetry : Specification
{
    ServiceProvider _provider;
    IChronicleBuilder _builder;
    IChronicleBuilder _result;

    void Establish() => _builder = new ChronicleBuilder(new ServiceCollection(), new ConfigurationBuilder().Build(), Substitute.For<IClientArtifactsProvider>());

    void Because()
    {
        _result = _builder.WithTelemetry(options => options.EventSourceIdTracing = EventSourceIdTracing.Omitted);
        _provider = _builder.Services.BuildServiceProvider();
    }

    [Fact] void should_continue_with_the_same_builder() => _result.ShouldEqual(_builder);
    [Fact] void should_configure_worker_options() => _provider.GetRequiredService<IOptions<ChronicleClientOptions>>().Value.Telemetry.EventSourceIdTracing.ShouldEqual(EventSourceIdTracing.Omitted);
    [Fact] void should_configure_base_options_used_by_aspnetcore() => _provider.GetRequiredService<IOptions<ChronicleOptions>>().Value.Telemetry.EventSourceIdTracing.ShouldEqual(EventSourceIdTracing.Omitted);

    void Destroy() => _provider.Dispose();
}
