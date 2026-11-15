// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Server.for_StorageAuthorizationHealthCheck.when_checking;

public class and_a_failure_is_recorded : Specification
{
    IStorageAuthorizationDiagnostics _diagnostics;
    HealthCheckResult _result;

    void Establish()
    {
        _diagnostics = Substitute.For<IStorageAuthorizationDiagnostics>();
        _diagnostics.Failures.Returns([new StorageAuthorizationFailure("System", "EventTypeStatistics", "listIndexes")]);
    }

    async Task Because() => _result = await new StorageAuthorizationHealthCheck(_diagnostics).CheckHealthAsync(new());

    [Fact] void should_report_degraded() => _result.Status.ShouldEqual(HealthStatus.Degraded);
    [Fact] void should_describe_the_database_collection_and_operation() => _result.Description.ShouldEqual("Storage refused: listIndexes on System.EventTypeStatistics. See https://www.cratis.io/chronicle/hosting/configuration/mongodb-permissions/#troubleshooting-authorization-errors");
    [Fact] void should_include_the_operation_in_the_data() => _result.Data["System.EventTypeStatistics"].ShouldEqual("listIndexes");
    [Fact] void should_not_report_unhealthy() => _result.Status.ShouldNotEqual(HealthStatus.Unhealthy);
    [Fact] void should_not_include_an_exception() => _result.Exception.ShouldBeNull();
}
