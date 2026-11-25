// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Supervision.for_SiloSupervisor.when_a_silo_leaves;

public class and_there_are_many_grains : given.a_silo_supervisor
{
    const int Count = 100;

    readonly int[] _asked = new int[Count];
    int _inFlight;
    int _mostInFlight;

    void Establish()
    {
        for (var index = 0; index < Count; index++)
        {
            var captured = index;
            _supervisor.Register($"grain-{index}", null, null, async () =>
            {
                var now = Interlocked.Increment(ref _inFlight);
                InterlockedMax(ref _mostInFlight, now);
                await Task.Yield();
                _asked[captured]++;
                Interlocked.Decrement(ref _inFlight);
            });
        }
    }

    async Task Because() => await _supervisor.RunDeparturePass(TimeSpan.Zero);

    static void InterlockedMax(ref int target, int value)
    {
        int current;
        while ((current = Volatile.Read(ref target)) < value && Interlocked.CompareExchange(ref target, value, current) != current)
        {
        }
    }

    [Fact] void should_ask_every_grain_once() => _asked.All(times => times == 1).ShouldBeTrue();
    [Fact] void should_ask_them_in_batches_rather_than_all_at_once() => _mostInFlight.ShouldBeLessThanOrEqual(SiloSupervisor.DepartureBatchSize);
}
