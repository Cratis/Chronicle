// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.for_ObserverRegistrationGate.when_entering;

public class and_the_limit_is_configured_below_one : Specification
{
    ObserverRegistrationGate _gate;
    IDisposable _held;
    Task<IDisposable> _waiting;

    void Establish() => _gate = new(Options.Create(new ChronicleOptions { Observers = new() { MaxConcurrentRegistrations = -1 } }));

    async Task Because()
    {
        _held = await _gate.Enter(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        _waiting = _gate.Enter(CancellationToken.None).AsTask();
    }

    async Task Destroy()
    {
        _held.Dispose();
        (await _waiting.WaitAsync(TimeSpan.FromSeconds(5))).Dispose();
        _gate.Dispose();
    }

    [Fact] void should_allow_the_first_registration() => _held.ShouldNotBeNull();
    [Fact] void should_not_queue_a_second_registration() => _waiting.IsCompletedSuccessfully.ShouldBeTrue();
}
