// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.for_ObserverRegistrationGate.when_entering;

public class and_no_limit_is_configured : Specification
{
    ObserverRegistrationGate _gate;
    ValueTask<IDisposable>[] _entries;

    void Establish() => _gate = new(Options.Create(new ChronicleOptions()));

    void Because() => _entries = Enumerable.Range(0, 100).Select(_ => _gate.Enter(CancellationToken.None)).ToArray();

    void Destroy()
    {
        foreach (var entry in _entries.Where(entry => entry.IsCompletedSuccessfully)) entry.Result.Dispose();
        _gate.Dispose();
    }

    [Fact] void should_never_wait_for_a_registration() => _entries.All(entry => entry.IsCompletedSuccessfully).ShouldBeTrue();
    [Fact] void should_not_create_a_semaphore() => typeof(ObserverRegistrationGate).GetField("_semaphore", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_gate).ShouldBeNull();
    [Fact] void should_reuse_the_empty_lease() => _entries.All(entry => ReferenceEquals(entry.Result, System.Reactive.Disposables.Disposable.Empty)).ShouldBeTrue();
}
