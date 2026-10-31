// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_LifecycleOccurrences;

public class when_lifecycle_ingress_loses_its_reply : given.a_lifecycle_grain
{
    Exception? _error;
    int _timersAfterUnknownWrite;

    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        var storage = Substitute.For<ILifecycleFanOutStorage>();
        storage.TryInsert(Arg.Any<LifecycleFanOutIntent>()).Returns<Task<bool>>(async call =>
        {
            await _intents.TryInsert(call.Arg<LifecycleFanOutIntent>());
            throw new IOException("commit succeeded before the reply was lost");
        });
        storage.GetPending(Arg.Any<int>()).Returns(call => _intents.GetPending(call.Arg<int>()));
        storage.Advance(Arg.Any<OccurrenceId>(), Arg.Any<int>()).Returns(call => _intents.Advance(call.Arg<OccurrenceId>(), call.Arg<int>()));
        _store.LifecycleFanOut.Returns(storage);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _grain.Record(OccurrenceKind.Startup, "rollout", true));
        _timersAfterUnknownWrite = _silo.TimerRegistry.NumberOfActiveTimers;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_preserve_the_unknown_ingress_outcome() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_leave_recovery_armed() => _timersAfterUnknownWrite.ShouldEqual(1);
    [Fact] async Task should_recover_the_committed_intent() => (await _intents.Get(OccurrenceIdentities.ForLifecycle(OccurrenceKind.Startup, "rollout")))!.Progress.ShouldEqual(2);
    [Fact] void should_disarm_after_recovery() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
