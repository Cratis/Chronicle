// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Telemetry.when_replaying_with_default_orleans_tracing;

[DependencyInjection.IgnoreConvention]
public class Reactor : IReactor
{
    readonly SemaphoreSlim _handledEvents = new(0);
    int _handled;

    public int Handled => Volatile.Read(ref _handled);

    public void OnEvent(EventApplied evt)
    {
        Interlocked.Increment(ref _handled);
        _handledEvents.Release();
    }

    public void Reset() => Interlocked.Exchange(ref _handled, 0);

    public async Task WaitFor(int count)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        for (var index = 0; index < count; index++)
            await _handledEvents.WaitAsync(timeout.Token);
    }
}
