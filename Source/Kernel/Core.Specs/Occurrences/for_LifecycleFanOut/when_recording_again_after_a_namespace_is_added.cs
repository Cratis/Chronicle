// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_LifecycleFanOut;

public class when_recording_again_after_a_namespace_is_added : given.a_fan_out
{
    LifecycleOccurrenceResult _result;
    IEnumerable<Occurrence> _later;

    async Task Establish()
    {
        await _fanOut.Record(OccurrenceKind.Startup, "rollout", true);
        AddNamespace("later");
    }

    async Task Because()
    {
        _result = await Recreate().Record(OccurrenceKind.Startup, "rollout", true);
        await _fanOut.Reconcile();
        _later = await _namespaces["later"].GetForTrigger(new("startup"));
    }

    [Fact] void should_return_the_original_snapshot() => _result.Namespaces.ShouldContainOnly("one", "two");
    [Fact] void should_report_the_existing_intent() => _result.Status.ShouldEqual(OccurrenceIngressStatus.AlreadyRecorded);
    [Fact] void should_not_deliver_to_the_later_namespace() => _later.ShouldBeEmpty();
}
