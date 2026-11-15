// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLeaseHealthCheck.given;

public class a_lease_health_check : Specification
{
    protected ISingleWriterLease _lease;
    protected ChronicleOptions _options;
    protected SingleWriterLeaseHealthCheck _check;
    protected HealthCheckResult _result;

    void Establish()
    {
        _lease = Substitute.For<ISingleWriterLease>();
        _options = new();
        _check = new(_lease, Microsoft.Extensions.Options.Options.Create(_options));
    }
}
