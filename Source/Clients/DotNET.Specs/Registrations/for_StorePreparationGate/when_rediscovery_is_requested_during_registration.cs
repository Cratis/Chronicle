// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_StorePreparationGate;

public class when_rediscovery_is_requested_during_registration : Specification
{
    StorePreparationGate _gate;
    TaskCompletionSource _releaseRegistration;
    Task _registering;
    bool _preparedWhileRegistering;
    int _rediscoveries;

    void Establish()
    {
        _gate = new();
        _releaseRegistration = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _registering = _gate.Register(() => Task.CompletedTask, () => _releaseRegistration.Task);
    }

    async Task Because()
    {
        var rediscovery = _gate.Discover(() =>
        {
            _rediscoveries++;
            return Task.CompletedTask;
        });
        _preparedWhileRegistering = _rediscoveries != 0;
        _releaseRegistration.SetResult();
        await Task.WhenAll(_registering, rediscovery).WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_not_mutate_the_revision_under_an_existing_registration_lease() => _preparedWhileRegistering.ShouldBeFalse();
    [Fact] void should_prepare_the_new_revision_after_registration_finishes() => _rediscoveries.ShouldEqual(1);
}
