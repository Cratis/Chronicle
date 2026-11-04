// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Server.for_StorageAuthorizationHealthCheck.when_checking;

public class and_no_failures_are_recorded : Specification
{
    IStorageAuthorizationDiagnostics _diagnostics;
    HealthCheckResult _result;

    void Establish()
    {
        _diagnostics = Substitute.For<IStorageAuthorizationDiagnostics>();
        _diagnostics.Failures.Returns([]);
    }

    async Task Because() => _result = await new StorageAuthorizationHealthCheck(_diagnostics).CheckHealthAsync(new());

    [Fact] void should_report_healthy() => _result.Status.ShouldEqual(HealthStatus.Healthy);
}
