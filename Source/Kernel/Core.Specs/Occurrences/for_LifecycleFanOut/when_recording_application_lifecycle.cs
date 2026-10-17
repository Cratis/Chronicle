// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_LifecycleFanOut;

public class when_recording_application_lifecycle : given.a_fan_out
{
    LifecycleOccurrenceResult _first;
    LifecycleOccurrenceResult _duplicate;
    Occurrence[] _stored;

    async Task Because()
    {
        _first = await _fanOut.Record(OccurrenceKind.Startup, "rollout", false);
        _duplicate = await Recreate().Record(OccurrenceKind.Startup, "rollout", false);
        _stored = (await _application.GetForTrigger(new("startup"))).ToArray();
    }

    [Fact] void should_record_one_application_occurrence() => _stored.Length.ShouldEqual(1);
    [Fact] void should_deduplicate_other_instances() => _duplicate.Status.ShouldEqual(OccurrenceIngressStatus.AlreadyRecorded);
    [Fact] void should_use_the_logical_activation_identity() => _first.Id.ShouldEqual(OccurrenceIdentities.ForLifecycle(OccurrenceKind.Startup, "rollout").Value);
    [Fact] void should_not_select_a_default_namespace() => _first.Namespaces.ShouldBeEmpty();
}
