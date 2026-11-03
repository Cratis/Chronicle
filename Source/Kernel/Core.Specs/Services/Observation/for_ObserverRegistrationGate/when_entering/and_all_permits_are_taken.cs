// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.for_ObserverRegistrationGate.when_entering;

public class and_all_permits_are_taken : Specification
{
    ObserverRegistrationGate _gate;
    IDisposable _held;
    Task<IDisposable> _waiting;
    bool _wasPending;

    async Task Establish()
    {
        _gate = new(Options.Create(new ChronicleOptions { Observers = new() { MaxConcurrentRegistrations = 2 } }));
        _held = await _gate.Enter(CancellationToken.None);
    }

    async Task Because()
    {
        using var second = await _gate.Enter(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        _waiting = _gate.Enter(CancellationToken.None).AsTask();
        _wasPending = !_waiting.IsCompleted;
    }

    async Task Destroy()
    {
        _held.Dispose();
        (await _waiting.WaitAsync(TimeSpan.FromSeconds(5))).Dispose();
        _gate.Dispose();
    }

    [Fact] void should_wait_for_a_permit() => _wasPending.ShouldBeTrue();
}
