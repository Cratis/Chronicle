// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Projections.for_ProjectionSubscriptionThrottle;

/// <summary>
/// Saturating and disposing one host does not consume or dispose another host's admission.
/// </summary>
public class when_resolving_in_two_independent_hosts : given.two_convention_bound_hosts
{
    IProjectionSubscriptionThrottle _firstThrottle;
    IProjectionSubscriptionThrottle _secondThrottle;
    bool _independentCapacity = true;

    async Task Because()
    {
        _firstThrottle = _first.Services.GetRequiredService<IProjectionSubscriptionThrottle>();
        _secondThrottle = _second.Services.GetRequiredService<IProjectionSubscriptionThrottle>();
        for (var index = 0; index < 8; index++)
        {
            _independentCapacity &= await _firstThrottle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
            _independentCapacity &= await _secondThrottle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        }

        _independentCapacity &= !await _firstThrottle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        _independentCapacity &= !await _secondThrottle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        _first.Dispose();
        for (var index = 0; index < 8; index++)
        {
            _firstThrottle.Release();
            _secondThrottle.Release();
        }

        for (var index = 0; index < 8; index++) _independentCapacity &= await _secondThrottle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        _independentCapacity &= !await _secondThrottle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        for (var index = 0; index < 8; index++) _secondThrottle.Release();
    }

    [Fact] void should_use_distinct_host_instances() => ReferenceEquals(_firstThrottle, _secondThrottle).ShouldBeFalse();
    [Fact] void should_give_each_host_its_own_eight_permits() => _independentCapacity.ShouldBeTrue();
}
