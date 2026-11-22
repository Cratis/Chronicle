// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.given;
using Cratis.Chronicle.Services.Observation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Supervision.for_SiloSupervisor.given;

public class a_silo_supervisor : Specification
{
    protected static readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

    protected DeliveryClock _clock;
    protected SiloSupervisor _supervisor;
    protected ISiloDepartures _departures;
    protected ChronicleOptions _options;

    void Establish()
    {
        _clock = new();
        _options = new();
        var monitor = Substitute.For<IOptionsMonitor<ChronicleOptions>>();
        monitor.CurrentValue.Returns(_ => _options);
        _departures = Substitute.For<ISiloDepartures>();
        _departures.Next.Returns(new TaskCompletionSource().Task);
        _supervisor = new(monitor, Substitute.For<ILogger<SiloSupervisor>>(), _departures, _clock);
    }

    protected static Func<Task<TimeSpan?>> Counting(Action onCheck, TimeSpan? next = null) => () =>
    {
        onCheck();
        return Task.FromResult(next);
    };

    /// <summary>
    /// Starts the supervisor the way the silo does, through its lifecycle stage.
    /// </summary>
    /// <returns>The observer that stops it.</returns>
    protected async Task<ILifecycleObserver> Start()
    {
        ILifecycleObserver? observer = null;
        var lifecycle = Substitute.For<ISiloLifecycle>();
        lifecycle.Subscribe(Arg.Any<string>(), Arg.Any<int>(), Arg.Do<ILifecycleObserver>(captured => observer = captured)).Returns(Substitute.For<IDisposable>());
        _supervisor.Participate(lifecycle);
        await observer!.OnStart(CancellationToken.None);
        return observer;
    }

    protected async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (!condition() && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(10);
        }
    }
}
