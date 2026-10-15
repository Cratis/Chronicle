// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceDelivery.when_recording;

public class and_the_reply_is_lost : given.a_queue
{
    OccurrenceIngressResult _result;

    async Task Establish() => await _queue.Record("retry", "{\"value\":1,\"nested\":{\"b\":2,\"a\":1}}", OccurrenceKind.Application);
    async Task Because() => _result = await _queue.Record("retry", "{\"nested\":{\"a\":1,\"b\":2},\"value\":1}", OccurrenceKind.Application);

    [Fact] void should_settle_from_durable_identity() => _result.Status.ShouldEqual(OccurrenceIngressStatus.AlreadyRecorded);
    [Fact] async Task should_keep_one_occurrence() => (await _storage.GetForTrigger(_trigger)).Count().ShouldEqual(1);
    [Fact] async Task should_pin_the_schema_revision() => (await _storage.Get(new(_result.Id)))!.SchemaRevision.ShouldEqual(4);
    [Fact] async Task should_use_kernel_recording_time() => (await _storage.Get(new(_result.Id)))!.RecordedAt.ShouldEqual(_now);
}
