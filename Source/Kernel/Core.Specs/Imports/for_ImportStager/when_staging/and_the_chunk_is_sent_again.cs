// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_ImportStager.when_staging;

public class and_the_chunk_is_sent_again : given.a_stager
{
    async Task Establish() => await Stage(0, Event(), Event());

    async Task Because() => _result = await Stage(0, Event(), Event());

    [Fact] void should_report_it_was_staged_before() => _result.AlreadyStaged.ShouldBeTrue();
    [Fact] void should_return_the_first_result() => _result.EventsReceived.ShouldEqual(2);
    [Fact] async Task should_keep_one_chunk() => (await _imports.GetChunkSummaries(_key.ImportId)).Count().ShouldEqual(1);
}
