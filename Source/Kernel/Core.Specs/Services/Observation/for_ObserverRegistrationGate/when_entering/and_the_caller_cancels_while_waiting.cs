// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.for_ObserverRegistrationGate.when_entering;

public class and_the_caller_cancels_while_waiting : Specification
{
    ObserverRegistrationGate _gate;
    readonly CancellationTokenSource _cancellation = new();
    IDisposable _held;
    IDisposable _replacement;
    Task<IDisposable> _waiting;
    Task<IDisposable> _next;
    Exception _error;
    bool _countWasUnchanged;

    async Task Establish()
    {
        _gate = new(Options.Create(new ChronicleOptions { Observers = new() { MaxConcurrentRegistrations = 1 } }));
        _held = await _gate.Enter(CancellationToken.None);
        _waiting = _gate.Enter(_cancellation.Token).AsTask();
    }

    async Task Because()
    {
        _cancellation.Cancel();
        _error = await Catch.Exception(async () => await _waiting.WaitAsync(TimeSpan.FromSeconds(5)));
        var replacement = _gate.Enter(CancellationToken.None).AsTask();
        _countWasUnchanged = !replacement.IsCompleted;
        _held.Dispose();
        _replacement = await replacement.WaitAsync(TimeSpan.FromSeconds(5));
        _next = _gate.Enter(CancellationToken.None).AsTask();
    }

    async Task Destroy()
    {
        _replacement.Dispose();
        (await _next.WaitAsync(TimeSpan.FromSeconds(5))).Dispose();
        _cancellation.Dispose();
        _gate.Dispose();
    }

    [Fact] void should_cancel_the_waiter() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_release_the_held_permit() => _countWasUnchanged.ShouldBeTrue();
    [Fact] void should_not_leak_the_permit() => _replacement.ShouldNotBeNull();
    [Fact] void should_not_add_an_extra_permit() => _next.IsCompleted.ShouldBeFalse();
}
