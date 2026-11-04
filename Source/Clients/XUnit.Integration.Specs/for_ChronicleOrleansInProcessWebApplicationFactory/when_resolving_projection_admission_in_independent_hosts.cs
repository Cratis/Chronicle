// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IProjectionSubscriptionThrottle = KernelCore::Cratis.Chronicle.Projections.IProjectionSubscriptionThrottle;
using ProjectionSubscriptionThrottle = KernelCore::Cratis.Chronicle.Projections.ProjectionSubscriptionThrottle;

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
    IProjectionSubscriptionThrottle _firstThrottle;
    IProjectionSubscriptionThrottle _secondThrottle;
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
        _firstThrottle = _first.Services.GetRequiredService<IProjectionSubscriptionThrottle>();
        _secondThrottle = _second.Services.GetRequiredService<IProjectionSubscriptionThrottle>();
        await using var firstScope = _first.Services.CreateAsyncScope();
        await using var secondScope = _first.Services.CreateAsyncScope();
        _sameScopeInstance = ReferenceEquals(_firstThrottle, _first.Services.GetRequiredService<IProjectionSubscriptionThrottle>()) &&
            ReferenceEquals(_firstThrottle, firstScope.ServiceProvider.GetRequiredService<IProjectionSubscriptionThrottle>()) &&
            ReferenceEquals(_firstThrottle, secondScope.ServiceProvider.GetRequiredService<IProjectionSubscriptionThrottle>());
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

    void Destroy()
    {
        _first?.Dispose();
        _second?.Dispose();
        _firstFactory?.Dispose();
        _secondFactory?.Dispose();
    }

    [Fact] void should_preserve_the_first_fallback_provider() => _first.Services.ShouldBeOfExactType<FallbackServiceProvider>();
    [Fact] void should_preserve_the_second_fallback_provider() => _second.Services.ShouldBeOfExactType<FallbackServiceProvider>();
    [Fact] void should_resolve_real_admission() => _firstThrottle.ShouldBeOfExactType<ProjectionSubscriptionThrottle>();
    [Fact] void should_share_admission_across_scopes() => _sameScopeInstance.ShouldBeTrue();
    [Fact] void should_not_share_between_hosts() => ReferenceEquals(_firstThrottle, _secondThrottle).ShouldBeFalse();
    [Fact] void should_give_each_host_eight_permits() => _independentCapacity.ShouldBeTrue();
}
