// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_SinkProvenanceCache;

public class when_the_second_route_becomes_restricted : given.two_routes
{
    readonly TaskCompletionSource _revoked = new(TaskCreationOptions.RunContinuationsAsynchronously);
    CancellationTokenRegistration _registration;

    void Establish()
    {
        _registration = _lease.Revoked.Register(() => _revoked.TrySetResult());
        _secondState = _secondState with { Restricted = true };
    }

    void Because() => _clock.Advance(TimeSpan.FromSeconds(1));

    [Fact] void should_deny_the_previously_admitted_route() => Catch.Exception(_lease.Check).ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_deny_the_second_route() => Catch.Exception(_secondLease.Check).ShouldBeOfExactType<InvalidOperationException>();
    [Fact] async Task should_revoke_the_shared_lease() => await _revoked.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

    void Destroy() => _registration.Dispose();
}
