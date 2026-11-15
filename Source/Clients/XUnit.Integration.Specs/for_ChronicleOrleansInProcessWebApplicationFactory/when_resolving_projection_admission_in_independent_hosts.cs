// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ISubscriptionAdmission = KernelCore::Cratis.Chronicle.Observation.ISubscriptionAdmission;
using SubscriptionAdmission = KernelCore::Cratis.Chronicle.Observation.SubscriptionAdmission;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleOrleansInProcessWebApplicationFactory;

/// <summary>
/// The build-only fallback host resolves real host-local admission without starting a silo.
/// </summary>
public class when_resolving_projection_admission_in_independent_hosts : Specification
{
    given.a_build_only_factory _firstFactory;
    given.a_build_only_factory _secondFactory;
    IHost _first;
    IHost _second;
    ISubscriptionAdmission _firstThrottle;
    ISubscriptionAdmission _secondThrottle;
    bool _sameScopeInstance;
    bool _independentCapacity = true;

    void Establish()
    {
        _firstFactory = new();
        _secondFactory = new();
        _first = _firstFactory.BuildHost();
        _second = _secondFactory.BuildHost();
    }

    async Task Because()
    {
        _firstThrottle = _first.Services.GetRequiredService<ISubscriptionAdmission>();
        _secondThrottle = _second.Services.GetRequiredService<ISubscriptionAdmission>();
        await using var firstScope = _first.Services.CreateAsyncScope();
        await using var secondScope = _first.Services.CreateAsyncScope();
        _sameScopeInstance = ReferenceEquals(_firstThrottle, _first.Services.GetRequiredService<ISubscriptionAdmission>()) &&
            ReferenceEquals(_firstThrottle, firstScope.ServiceProvider.GetRequiredService<ISubscriptionAdmission>()) &&
            ReferenceEquals(_firstThrottle, secondScope.ServiceProvider.GetRequiredService<ISubscriptionAdmission>());
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

    void Destroy()
    {
        _first?.Dispose();
        _second?.Dispose();
        _firstFactory?.Dispose();
        _secondFactory?.Dispose();
    }

    [Fact] void should_preserve_the_first_fallback_provider() => _first.Services.ShouldBeOfExactType<FallbackServiceProvider>();
    [Fact] void should_preserve_the_second_fallback_provider() => _second.Services.ShouldBeOfExactType<FallbackServiceProvider>();
    [Fact] void should_resolve_real_admission() => _firstThrottle.ShouldBeOfExactType<SubscriptionAdmission>();
    [Fact] void should_share_admission_across_scopes() => _sameScopeInstance.ShouldBeTrue();
    [Fact] void should_not_share_between_hosts() => ReferenceEquals(_firstThrottle, _secondThrottle).ShouldBeFalse();
    [Fact] void should_give_each_host_its_own_permits() => _independentCapacity.ShouldBeTrue();
}
