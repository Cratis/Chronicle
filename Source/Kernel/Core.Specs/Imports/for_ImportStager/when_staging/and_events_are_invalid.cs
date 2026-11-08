// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_ImportStager.when_staging;

public class and_events_are_invalid : given.a_stager
{
    async Task Because() => _result = await Stage(0, Event(), Event(content: "{}"), Event(eventType: "unknown"), Event(content: "[1]"), Event(idempotencyKey: "   "));

    [Fact] void should_stage_the_valid_event() => _result.EventsStaged.ShouldEqual(1);
    [Fact] void should_count_every_event_received() => _result.EventsReceived.ShouldEqual(5);
    [Fact] void should_reject_the_others_by_their_index_in_the_import() => _result.Rejections.Select(_ => _.Index).ShouldEqual([1L, 2L, 3L, 4L]);
    [Fact] void should_say_the_schema_was_broken() => _result.Rejections.First().Reasons.First().ShouldContain("SchemaValidation");
    [Fact] void should_say_the_event_type_is_unknown() => _result.Rejections.ElementAt(1).Reasons.First().ShouldContain("not registered");
}
