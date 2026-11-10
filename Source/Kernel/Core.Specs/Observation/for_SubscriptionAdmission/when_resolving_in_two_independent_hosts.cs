// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Observation.for_SubscriptionAdmission;

/// <summary>
/// Saturating and disposing one host does not consume or dispose another host's admission.
/// </summary>
public class when_resolving_in_two_independent_hosts : given.two_convention_bound_hosts
{
    ISubscriptionAdmission _firstThrottle;
    ISubscriptionAdmission _secondThrottle;
    bool _independentCapacity = true;

    async Task Because()
    {
        _firstThrottle = _first.Services.GetRequiredService<ISubscriptionAdmission>();
        _secondThrottle = _second.Services.GetRequiredService<ISubscriptionAdmission>();
        for (var index = 0; index < _firstThrottle.Limit; index++)
        {
            _independentCapacity &= await _firstThrottle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
            _independentCapacity &= await _secondThrottle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        }

        _independentCapacity &= !await _firstThrottle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        _independentCapacity &= !await _secondThrottle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        _first.Dispose();
        for (var index = 0; index < _firstThrottle.Limit; index++)
        {
            _firstThrottle.Release();
            _secondThrottle.Release();
        }

        for (var index = 0; index < _firstThrottle.Limit; index++) _independentCapacity &= await _secondThrottle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        _independentCapacity &= !await _secondThrottle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        for (var index = 0; index < _firstThrottle.Limit; index++) _secondThrottle.Release();
    }

    [Fact] void should_use_distinct_host_instances() => ReferenceEquals(_firstThrottle, _secondThrottle).ShouldBeFalse();
    [Fact] void should_give_each_host_its_own_permits() => _independentCapacity.ShouldBeTrue();
}
