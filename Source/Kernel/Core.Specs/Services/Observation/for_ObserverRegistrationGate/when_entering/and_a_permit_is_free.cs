// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.for_ObserverRegistrationGate.when_entering;

public class and_a_permit_is_free : Specification
{
    ObserverRegistrationGate _gate;
    ValueTask<IDisposable> _enter;

    void Establish() => _gate = new(Options.Create(new ChronicleOptions { Observers = new() { MaxConcurrentRegistrations = 1 } }));

    void Because() => _enter = _gate.Enter(CancellationToken.None);

    async Task Destroy()
    {
        (await _enter.AsTask().WaitAsync(TimeSpan.FromSeconds(5))).Dispose();
        _gate.Dispose();
    }

    [Fact] void should_acquire_the_permit_immediately() => _enter.IsCompletedSuccessfully.ShouldBeTrue();
}
