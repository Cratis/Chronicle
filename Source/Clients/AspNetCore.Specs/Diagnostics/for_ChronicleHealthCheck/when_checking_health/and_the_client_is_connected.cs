// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.AspNetCore.Diagnostics.for_ChronicleHealthCheck.when_checking_health;

public class and_the_client_is_connected : given.a_health_check
{
    HealthCheckResult _result;

    void Establish() => _lifecycle.IsConnected.Returns(true);

    async Task Because() => _result = await _healthCheck.CheckHealthAsync(_context);

    [Fact] void should_report_healthy() => _result.Status.ShouldEqual(HealthStatus.Healthy);
}
