// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceDelivery.when_recording;

public class with_invalid_payload : given.a_queue
{
    OccurrenceIngressResult _result;
    async Task Because() => _result = await _queue.Record("invalid", "{\"value\":\"not-an-integer\"}", OccurrenceKind.Application);
    [Fact] void should_report_schema_rejection() => _result.Status.ShouldEqual(OccurrenceIngressStatus.OccurrencePayloadInvalid);
    [Fact] async Task should_write_nothing() => (await _storage.GetForTrigger(_trigger)).ShouldBeEmpty();
}
