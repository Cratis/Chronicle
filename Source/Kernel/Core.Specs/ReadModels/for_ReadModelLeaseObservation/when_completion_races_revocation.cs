// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using Cratis.Chronicle.Storage.InMemory.Sinks;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelLeaseObservation;

public class when_completion_races_revocation : Specification
{
    bool _finished;
    bool _marked;
    int _completed;
    int _failed;

    async Task Because()
    {
        var raw = new InMemorySinkProvenanceStorage();
        await raw.CompleteInventory(false);
        var cache = new SinkProvenanceCache();
        var cached = cache.Bind("route", () => raw);
        var lease = await cached.GetLease();
        await lease.Ensure();
        var observer = System.Reactive.Observer.Create<int>(_ => { }, _ => _failed++, () => _completed++);
        var observation = new ReadModelLeaseObservation<int>(lease, observer);
        using var entered = new ManualResetEventSlim();
        Thread? revoker = null;
        var source = Observable.Create<int>(_ => Disposable.Create(() =>
        {
            revoker = new Thread(() =>
            {
                entered.Set();
                cached.MarkQuarantined().GetAwaiter().GetResult();
            })
            { IsBackground = true };
            revoker.Start();
            entered.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Wait for the cancellation callback to block on delivery, or for nonblocking notification to finish.
            while ((revoker.ThreadState & (ThreadState.WaitSleepJoin | ThreadState.Stopped)) == ThreadState.Running)
            {
                deadline.Token.ThrowIfCancellationRequested();
            }
        }));
        observation.Start(source);
        var completion = Task.Run(observation.OnCompleted);
        _finished = await Catch.Exception(() => completion.WaitAsync(TimeSpan.FromSeconds(5))) is null;
        if (_finished)
        {
            await completion;
            revoker!.Join(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            _marked = await raw.IsQuarantined();
            await cache.DisposeAsync();
        }
    }

    [Fact] void should_finish_without_waiting_for_a_callback_holding_the_delivery_lock() => _finished.ShouldBeTrue();
    [Fact] void should_persist_the_restriction() => _marked.ShouldBeTrue();
    [Fact] void should_publish_only_one_terminal_notification() => (_completed + _failed).ShouldEqual(1);
}
