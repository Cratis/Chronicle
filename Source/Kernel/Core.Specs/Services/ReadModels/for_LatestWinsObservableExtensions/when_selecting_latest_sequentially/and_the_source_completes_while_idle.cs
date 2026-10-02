// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.ReadModels.for_LatestWinsObservableExtensions.when_selecting_latest_sequentially;

public class and_the_source_completes_while_idle : given.a_latest_wins_pipeline
{
    void Establish() => Subscribe(item => Task.FromResult(item * 10));

    async Task Because()
    {
        // The selector completes synchronously, so the item has been delivered and nothing is being processed when the source completes.
        _source.OnNext(1);
        _source.OnCompleted();
        await _terminated.Task.WaitAsync(_deadline);
    }

    [Fact] void should_deliver_the_result_and_then_complete() => _events.ShouldContainOnly(["next:10", "completed"]);
    [Fact] void should_complete_last() => _events[^1].ShouldEqual("completed");
}
