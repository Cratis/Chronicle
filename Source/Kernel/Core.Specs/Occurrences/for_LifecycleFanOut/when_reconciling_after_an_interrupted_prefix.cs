// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_LifecycleFanOut;

public class when_reconciling_after_an_interrupted_prefix : given.a_fan_out
{
    Exception? _failure;
    Occurrence[] _one;
    Occurrence[] _two;
    LifecycleFanOutIntent? _intent;

    async Task Establish()
    {
        await _fanOut.Record(OccurrenceKind.Startup, "rollout", true);
        _interrupt = true;
    }

    async Task Because()
    {
        _failure = await Catch.Exception(_fanOut.Reconcile);
        _interrupt = false;
        _fanOut = Recreate();
        await _fanOut.Reconcile();
        await _fanOut.Reconcile();
        _one = (await _namespaces["one"].GetForTrigger(new("startup"))).ToArray();
        _two = (await _namespaces["two"].GetForTrigger(new("startup"))).ToArray();
        _intent = await _intents.Get(OccurrenceIdentities.ForLifecycle(OccurrenceKind.Startup, "rollout"));
    }

    [Fact] void should_isolate_the_interrupted_write_for_retry() => _failure.ShouldBeNull();
    [Fact] void should_not_duplicate_the_acknowledged_namespace() => _one.Length.ShouldEqual(1);
    [Fact] void should_complete_the_unwritten_namespace() => _two.Length.ShouldEqual(1);
    [Fact] void should_acknowledge_the_complete_snapshot() => _intent!.Progress.ShouldEqual(2);
    [Fact] void should_use_the_namespace_lifecycle_identity() => _one[0].Id.ShouldEqual(OccurrenceIdentities.ForLifecycle(OccurrenceKind.Startup, "rollout", "one"));
}
