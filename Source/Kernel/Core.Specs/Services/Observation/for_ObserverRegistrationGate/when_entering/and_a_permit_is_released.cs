// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.for_ObserverRegistrationGate.when_entering;

public class and_a_permit_is_released : Specification
{
    ObserverRegistrationGate _gate;
    IDisposable _held;
    IDisposable _acquired;
    Task<IDisposable> _waiting;

    async Task Establish()
    {
        _gate = new(Options.Create(new ChronicleOptions { Observers = new() { MaxConcurrentRegistrations = 1 } }));
        _held = await _gate.Enter(CancellationToken.None);
        _waiting = _gate.Enter(CancellationToken.None).AsTask();
    }

    async Task Because()
    {
        _held.Dispose();
        _acquired = await _waiting.WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy()
    {
        _acquired.Dispose();
        _gate.Dispose();
    }

    [Fact] void should_let_the_waiter_proceed() => _acquired.ShouldNotBeNull();
}
