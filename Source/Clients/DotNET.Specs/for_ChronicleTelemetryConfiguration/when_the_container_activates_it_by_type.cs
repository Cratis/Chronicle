// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.for_ChronicleTelemetryConfiguration;

public class when_the_container_activates_it_by_type : Specification
{
    ServiceProvider _provider;
    ChronicleTelemetryConfiguration _configuration;
    ChronicleTelemetryOptions _options;

    void Establish() => _provider = new ServiceCollection()
        .AddTransient<ChronicleTelemetryConfiguration>()
        .BuildServiceProvider();

    void Because()
    {
        _configuration = _provider.GetRequiredService<ChronicleTelemetryConfiguration>();
        _options = new ChronicleTelemetryOptions();
        _configuration.Configure(_options);
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_resolve_the_configuration() => _configuration.ShouldNotBeNull();
    [Fact] void should_apply_no_changes() => _options.ShouldNotBeNull();
}
