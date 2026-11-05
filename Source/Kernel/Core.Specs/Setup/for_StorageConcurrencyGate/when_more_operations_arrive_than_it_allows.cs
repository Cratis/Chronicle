// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.for_StorageConcurrencyGate;

public class when_more_operations_arrive_than_it_allows : Specification
{
    StorageConcurrencyGate _gate;
    int _running;
    int _mostRunning;
    int _completed;

    void Establish() => _gate = new(2);

    async Task Because()
    {
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => _gate.Run(async () =>
        {
            var now = Interlocked.Increment(ref _running);
            lock (this)
            {
                _mostRunning = Math.Max(_mostRunning, now);
            }

            await Task.Delay(5);
            Interlocked.Decrement(ref _running);
            Interlocked.Increment(ref _completed);
        })));
    }

    [Fact] void should_never_run_more_than_allowed() => _mostRunning.ShouldBeLessThanOrEqual(2);
    [Fact] void should_run_them_all() => _completed.ShouldEqual(20);
}
