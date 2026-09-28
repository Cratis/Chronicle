// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_observing_instances_during_a_replay;

/// <summary>
/// An observed page and the total count sent along with it are read separately; both describe the state the read
/// model had before the replay, so they agree while the replay runs.
/// </summary>
public class and_counting_the_instances : given.an_observed_read_model_being_replayed
{
    int[]? _page;
    long _totalCount;

    async Task Because()
    {
        Observe();
        _page = await PageWhere(_ => true);
        _totalCount = (await _sink.GetInstances()).TotalCount;
    }

    [Fact] void should_stream_the_state_before_the_replay() => _page.ShouldContainOnly(1);
    [Fact] void should_count_the_instances_it_streams() => _totalCount.ShouldEqual(_page!.Length);
}
