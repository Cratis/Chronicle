// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.for_ObserverRegistrationGate.when_entering;

public class and_a_lease_is_disposed_twice : Specification
{
    ObserverRegistrationGate _gate;
    IDisposable _held;
    IDisposable _replacement;
    Task<IDisposable> _waiting;

    async Task Establish()
    {
        _gate = new(Options.Create(new ChronicleOptions { Observers = new() { MaxConcurrentRegistrations = 1 } }));
        _held = await _gate.Enter(CancellationToken.None);
    }

    async Task Because()
    {
        _held.Dispose();
        _replacement = await _gate.Enter(CancellationToken.None);
        _held.Dispose();
        _waiting = _gate.Enter(CancellationToken.None).AsTask();
    }

    async Task Destroy()
    {
        _replacement.Dispose();
        (await _waiting.WaitAsync(TimeSpan.FromSeconds(5))).Dispose();
        _gate.Dispose();
    }

    [Fact] void should_release_only_once() => _waiting.IsCompleted.ShouldBeFalse();
}
