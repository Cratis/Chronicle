// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_LifecycleFanOut;

public class when_reconciling_with_a_poisoned_intent : given.a_fan_out
{
    Exception? _error;
    LifecycleFanOutIntent? _poisoned;
    LifecycleFanOutIntent? _healthy;

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
    }

    [Fact] void should_isolate_the_failure() => _error.ShouldBeNull();
    [Fact] void should_preserve_the_failed_prefix_for_retry() => _poisoned!.Progress.ShouldEqual(0);
    [Fact] void should_complete_the_unrelated_intent() => _healthy!.Progress.ShouldEqual(2);
}
