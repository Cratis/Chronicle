// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using AlertsOptions = Cratis.Chronicle.Configuration.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertConditions.given;

/// <summary>
/// Builds the alert conditions over the alert configuration a spec chooses.
/// </summary>
public class alert_conditions : Specification
{
    protected AlertConditions _conditions;

    void Establish() => Configure(new AlertsOptions());

    protected void Configure(AlertsOptions alerts)
    {
        var options = Substitute.For<IOptionsMonitor<ChronicleOptions>>();
        options.CurrentValue.Returns(new ChronicleOptions { Alerts = alerts });
        _conditions = new(options, NullLogger<AlertConditions>.Instance);
    }
}
