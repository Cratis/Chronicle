// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Projections.for_ProjectionSubscriptionThrottle;

/// <summary>
/// Convention binding shares admission across repeated root and scoped resolutions.
/// </summary>
public class when_resolving_twice_in_one_host : given.two_convention_bound_hosts
{
    IProjectionSubscriptionThrottle _throttle;
    bool _sameInstance;

    void Because()
    {
        _throttle = _first.Services.GetRequiredService<IProjectionSubscriptionThrottle>();
        using var firstScope = _first.Services.CreateScope();
        using var secondScope = _first.Services.CreateScope();
        _sameInstance = ReferenceEquals(_throttle, _first.Services.GetRequiredService<IProjectionSubscriptionThrottle>()) &&
            ReferenceEquals(_throttle, firstScope.ServiceProvider.GetRequiredService<IProjectionSubscriptionThrottle>()) &&
            ReferenceEquals(_throttle, secondScope.ServiceProvider.GetRequiredService<IProjectionSubscriptionThrottle>());
    }

    [Fact] void should_resolve_the_real_throttle() => _throttle.ShouldBeOfExactType<ProjectionSubscriptionThrottle>();
    [Fact] void should_share_one_instance() => _sameInstance.ShouldBeTrue();
}
