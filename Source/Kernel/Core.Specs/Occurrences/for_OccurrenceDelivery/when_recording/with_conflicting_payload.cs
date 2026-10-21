// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceDelivery.when_recording;

public class with_conflicting_payload : given.a_queue
{
    OccurrenceIngressResult _result;
    async Task Establish() => await _queue.Record("same", "{\"value\":1}", OccurrenceKind.Startup);
    async Task Because() => _result = await _queue.Record("same", "{\"value\":2}", OccurrenceKind.Startup);
    [Fact] void should_refuse_the_collision() => _result.Status.ShouldEqual(OccurrenceIngressStatus.OccurrenceIdConflict);
    [Fact] async Task should_preserve_the_first_payload() => (await _storage.Get(new(_result.Id)))!.Payload.ShouldEqual("{\"value\":1}");
}
