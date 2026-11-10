// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Observation.for_SubscriptionAdmission;

/// <summary>
/// Convention binding shares admission across repeated root and scoped resolutions.
/// </summary>
public class when_resolving_twice_in_one_host : given.two_convention_bound_hosts
{
    ISubscriptionAdmission _throttle;
    bool _sameInstance;

    void Because()
    {
        _throttle = _first.Services.GetRequiredService<ISubscriptionAdmission>();
        using var firstScope = _first.Services.CreateScope();
        using var secondScope = _first.Services.CreateScope();
        _sameInstance = ReferenceEquals(_throttle, _first.Services.GetRequiredService<ISubscriptionAdmission>()) &&
            ReferenceEquals(_throttle, firstScope.ServiceProvider.GetRequiredService<ISubscriptionAdmission>()) &&
            ReferenceEquals(_throttle, secondScope.ServiceProvider.GetRequiredService<ISubscriptionAdmission>());
    }

    [Fact] void should_resolve_the_real_throttle() => _throttle.ShouldBeOfExactType<SubscriptionAdmission>();
    [Fact] void should_share_one_instance() => _sameInstance.ShouldBeTrue();
}
