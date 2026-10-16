// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_LifecycleFanOut;

public class when_reconciling_with_a_poisoned_intent : given.a_fan_out
{
    Exception? _error;
    LifecycleFanOutIntent? _poisoned;
    LifecycleFanOutIntent? _healthy;
    IEnumerable<LifecycleFanOutIntent> _pending;
    bool _deliveredToTheUnconflictedNamespace;

    async Task Establish()
    {
        await _fanOut.Record(OccurrenceKind.Startup, "poisoned", true);
        await _fanOut.Record(OccurrenceKind.Shutdown, "healthy", true);
        var template = (await _intents.Get(OccurrenceIdentities.ForLifecycle(OccurrenceKind.Startup, "poisoned")))!.Occurrence;
        await _namespaces["one"].Record(template with
        {
            Id = OccurrenceIdentities.ForLifecycle(OccurrenceKind.Startup, "poisoned", "one"),
            Fingerprint = new("different-content")
        });
    }

    async Task Because()
    {
        _error = await Catch.Exception(_fanOut.Reconcile);
        _poisoned = await _intents.Get(OccurrenceIdentities.ForLifecycle(OccurrenceKind.Startup, "poisoned"));
        _healthy = await _intents.Get(OccurrenceIdentities.ForLifecycle(OccurrenceKind.Shutdown, "healthy"));
        _pending = await _intents.GetPending(16);
        _deliveredToTheUnconflictedNamespace = await _namespaces["two"].Get(OccurrenceIdentities.ForLifecycle(OccurrenceKind.Startup, "poisoned", "two")) is not null;
    }

    [Fact] void should_isolate_the_failure() => _error.ShouldBeNull();
    [Fact] void should_finish_the_intent_instead_of_retrying_a_permanent_conflict() => _poisoned!.Progress.ShouldEqual(2);
    [Fact] void should_leave_nothing_pending() => _pending.ShouldBeEmpty();
    [Fact] void should_still_deliver_to_the_namespace_without_a_conflict() => _deliveredToTheUnconflictedNamespace.ShouldBeTrue();
    [Fact] void should_complete_the_unrelated_intent() => _healthy!.Progress.ShouldEqual(2);
}
