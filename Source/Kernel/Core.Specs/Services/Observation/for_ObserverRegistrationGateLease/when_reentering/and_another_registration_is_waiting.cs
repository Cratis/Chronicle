// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.for_ObserverRegistrationGateLease.when_reentering;

public class and_another_registration_is_waiting : Specification
{
    ObserverRegistrationGate _gate;
    ObserverRegistrationGateLease _lease;
    Task<IDisposable> _waiting;
    Task _reentering;
    bool _waitingGotThrough;
    bool _reenteringWasPending;
    bool _reenteringGotThroughOnceReleased;

    async Task Establish()
    {
        _gate = new(Options.Create(new ChronicleOptions { Observers = new() { MaxConcurrentRegistrations = 1 } }));
        _lease = await ObserverRegistrationGateLease.Enter(_gate, CancellationToken.None);
        _waiting = _gate.Enter(CancellationToken.None).AsTask();
    }

    async Task Because()
    {
        _reentering = _lease.Reenter(CancellationToken.None);
        var other = await _waiting.WaitAsync(TimeSpan.FromSeconds(5));
        _waitingGotThrough = true;
        _reenteringWasPending = !_reentering.IsCompleted;
        other.Dispose();
        await _reentering.WaitAsync(TimeSpan.FromSeconds(5));
        _reenteringGotThroughOnceReleased = true;
    }

    void Destroy()
    {
        _lease.Dispose();
        _gate.Dispose();
    }

    [Fact] void should_let_the_waiting_registration_through() => _waitingGotThrough.ShouldBeTrue();
    [Fact] void should_queue_behind_it() => _reenteringWasPending.ShouldBeTrue();
    [Fact] void should_get_a_permit_once_it_is_released() => _reenteringGotThroughOnceReleased.ShouldBeTrue();
}
