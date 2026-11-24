// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Diagnostics.Load.for_KernelOverloadHealthCheck;

public class when_checking : Specification
{
    ManualTimeProvider _time;
    KernelLoad _load;
    KernelOverloadHealthCheck _check;
    HealthCheckResult _accepting;
    HealthCheckResult _degraded;
    HealthCheckResult _recovering;
    HealthCheckResult _drained;
    PendingCall _call;

    void Establish()
    {
        _time = new();
        _load = new KernelLoad(Options.Create(new ChronicleOptions()), NullLogger<KernelLoad>.Instance, null, _time);
        _check = new KernelOverloadHealthCheck(_load);
    }

    async Task Because()
    {
        _accepting = await _check.CheckHealthAsync(new HealthCheckContext());

        _call = _load.BeginPending("IProjectionsManager", "the-store");
        _time.Advance(TimeSpan.FromSeconds(20));
        _degraded = await _check.CheckHealthAsync(new HealthCheckContext());

        _load.EndPending(_call);
        _time.Advance(TimeSpan.FromSeconds(2));
        _recovering = await _check.CheckHealthAsync(new HealthCheckContext());

        _time.Advance(TimeSpan.FromSeconds(31));
        _drained = await _check.CheckHealthAsync(new HealthCheckContext());
    }

    [Fact] void should_be_healthy_when_accepting() => _accepting.Status.ShouldEqual(HealthStatus.Healthy);
    [Fact] void should_be_degraded_when_saturated() => _degraded.Status.ShouldEqual(HealthStatus.Degraded);
    [Fact] void should_say_why_when_saturated() => _degraded.Description.ShouldContain("IProjectionsManager");
    [Fact] void should_name_the_event_store_when_saturated() => _degraded.Description.ShouldContain("the-store");
    [Fact] void should_stay_degraded_while_recovering() => _recovering.Status.ShouldEqual(HealthStatus.Degraded);
    [Fact] void should_say_that_it_is_recovering() => _recovering.Description.ShouldContain("recovering");
    [Fact] void should_be_healthy_again_once_drained() => _drained.Status.ShouldEqual(HealthStatus.Healthy);
}
