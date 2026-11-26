// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.for_ObserverRegistrationGate.when_entering;

public class and_the_default_limit_is_taken : Specification
{
    ObserverRegistrationGate _gate;
    List<IDisposable> _held;
    Task<IDisposable> _waiting;
    bool _wasPending;

    async Task Establish()
    {
        _gate = new(Options.Create(new ChronicleOptions()));
        _held = [];
        for (var i = 0; i < new ChronicleOptions().Observers.MaxConcurrentRegistrations; i++)
        {
            _held.Add(await _gate.Enter(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
        }
    }

    void Because()
    {
        _waiting = _gate.Enter(CancellationToken.None).AsTask();
        _wasPending = !_waiting.IsCompleted;
    }

    async Task Destroy()
    {
        _held.ForEach(_ => _.Dispose());
        (await _waiting.WaitAsync(TimeSpan.FromSeconds(5))).Dispose();
        _gate.Dispose();
    }

    [Fact] void should_make_the_next_registration_wait_without_any_configuration() => _wasPending.ShouldBeTrue();
}
