// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_ImportStager.when_staging;

public class and_other_events_are_sent_under_the_same_index : given.a_stager
{
    async Task Establish() => await Stage(0, Event(), Event());

    async Task Because() => _result = await Stage(0, Event(), Event(), Event());

    [Fact] void should_refuse_the_chunk_as_a_conflict() => _result.Error.ShouldContain("must carry the same events");
    [Fact] void should_not_report_it_as_staged() => _result.AlreadyStaged.ShouldBeFalse();
    [Fact] async Task should_keep_the_chunk_staged_first() => (await _imports.GetChunk(_key.ImportId, 0))!.EventsReceived.ShouldEqual(2);
}
