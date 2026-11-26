// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.for_ObserverRegistrationGate.when_a_burst_of_registrations_meets_a_busy_observer;

/// <summary>
/// Reproduces the registration burst of a large client: many subscribes arrive at once and a few of the observers are
/// busy, so every attempt at them times out. Healthy observers must still get through, the load on the kernel must
/// stay bounded and the busy observers must give their permit back instead of blocking everyone behind them.
/// </summary>
public class and_the_gate_is_bounded : Specification
{
    const int Limit = 4;
    const int Healthy = 400;
    const int Busy = 6;

    ObserverRegistrationGate _gate;
    int _inFlight;
    int _maxInFlight;
    int _healthyEstablished;
    int _busyTimedOut;
    int _busyAttempts;

    void Establish() => _gate = new(Options.Create(new ChronicleOptions { Observers = new() { MaxConcurrentRegistrations = Limit } }));

    async Task Because()
    {
        var all = Enumerable.Range(0, Healthy).Select(_ => Register(busy: false))
            .Concat(Enumerable.Range(0, Busy).Select(_ => Register(busy: true)))
            .ToArray();
        await Task.WhenAll(all).WaitAsync(TimeSpan.FromSeconds(60));
    }

    void Destroy() => _gate.Dispose();

    [Fact] void should_never_run_more_registrations_than_the_limit() => (_maxInFlight <= Limit).ShouldBeTrue();
    [Fact] void should_establish_every_healthy_observer() => _healthyEstablished.ShouldEqual(Healthy);
    [Fact] void should_ask_the_busy_observers_the_maximum_number_of_times() => _busyAttempts.ShouldEqual(Busy * ObserverRegistrationEstablishment.MaxAttempts);
    [Fact] void should_surface_a_timeout_for_each_busy_observer() => _busyTimedOut.ShouldEqual(Busy);

    static void InterlockedMax(ref int target, int value)
    {
        int current;
        while (value > (current = Volatile.Read(ref target)))
        {
            if (Interlocked.CompareExchange(ref target, value, current) == current)
            {
                return;
            }
        }
    }

    async Task Register(bool busy)
    {
        await Task.Yield();
        using var lease = await ObserverRegistrationGateLease.Enter(_gate, CancellationToken.None);
        try
        {
            await ObserverRegistrationEstablishment.Establish(
                async () =>
                {
                    var now = Interlocked.Increment(ref _inFlight);
                    InterlockedMax(ref _maxInFlight, now);
                    try
                    {
                        await Task.Delay(busy ? 20 : 1);
                        if (busy)
                        {
                            Interlocked.Increment(ref _busyAttempts);
                            throw new TimeoutException();
                        }
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _inFlight);
                    }
                },
                (_, _) => { },
                () => lease.Reenter(CancellationToken.None),
                CancellationToken.None);
            Interlocked.Increment(ref _healthyEstablished);
        }
        catch (TimeoutException)
        {
            Interlocked.Increment(ref _busyTimedOut);
        }
    }
}
