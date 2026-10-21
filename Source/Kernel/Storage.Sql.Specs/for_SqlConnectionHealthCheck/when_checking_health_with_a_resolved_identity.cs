// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Storage.Sql.for_SqlConnectionHealthCheck;

public class when_checking_health_with_a_resolved_identity : Specification
{
    SqlConnectionHealthCheck _check;
    HealthCheckContext _context;
    DbConnection _prepared;
    HealthCheckResult _result;

    void Establish()
    {
        _check = new(
            "Server=sql.invalid;Database=chronicle;Encrypt=true",
            connection =>
            {
                _prepared = connection;
                throw new InvalidOperationException("stop before connecting");
            });
        _context = new() { Registration = new("mssql", Substitute.For<IHealthCheck>(), HealthStatus.Unhealthy, null) };
    }

    async Task Because() => _result = await _check.CheckHealthAsync(_context);

    [Fact] void should_prepare_the_connection_before_opening_it() => _prepared.ShouldBeOfExactType<SqlConnection>();
    [Fact] void should_report_the_registered_failure_status() => _result.Status.ShouldEqual(HealthStatus.Unhealthy);
    [Fact] void should_carry_the_failure() => _result.Exception.ShouldBeOfExactType<InvalidOperationException>();
}
