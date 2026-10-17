// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_LifecycleOccurrences;

public class when_recording_with_an_unrelated_pending_intent : given.a_lifecycle_grain
{
    LifecycleOccurrenceResult? _result;
    Exception? _error;

    void Establish() => _queue.Prepare().Returns<Task>(_ => throw new IOException("unavailable namespace"));

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        await _grain.Record(OccurrenceKind.Startup, "unrelated", true);
        _result = JsonSerializer.Deserialize<LifecycleOccurrenceResult>(await _grain.Record(OccurrenceKind.Startup, "new-activation", true));
    });

    [Fact] void should_reply_after_the_durable_insert() => _error.ShouldBeNull();
    [Fact] void should_report_the_recorded_outcome() => _result!.Status.ShouldEqual(OccurrenceIngressStatus.Recorded);
    [Fact] void should_arm_recovery() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
