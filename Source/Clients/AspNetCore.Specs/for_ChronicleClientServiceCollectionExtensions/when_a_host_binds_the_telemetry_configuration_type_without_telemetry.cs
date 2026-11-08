// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

public class when_a_host_binds_the_telemetry_configuration_type_without_telemetry : Specification
{
    ServiceProvider _provider;
    ChronicleAspNetCoreOptions _options;

    void Establish()
    {
        var services = new ServiceCollection();

        // Some host discovery binds types without honouring [IgnoreConvention], and nothing registers an Action<ChronicleTelemetryOptions>.
        services.AddSingleton<ChronicleTelemetryConfiguration>();
        var builder = new ChronicleBuilder(services, new ConfigurationBuilder().Build(), DefaultClientArtifactsProvider.Default);
        services.AddCratisChronicleClient(builder);
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    void Because() => _options = _provider.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>().Value;

    void Destroy() => _provider.Dispose();

    [Fact] void should_keep_default_identifier_tracing() => _options.Telemetry.EventSourceIdTracing.ShouldEqual(EventSourceIdTracing.Raw);
}
