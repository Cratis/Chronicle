// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.ReadModels.for_LatestWinsObservableExtensions.when_selecting_latest_sequentially;

public class and_the_source_errors_while_an_item_is_processing : given.a_latest_wins_pipeline
{
    void Establish() => SubscribeWithGatedFirstItem();

    async Task Because()
    {
        _source.OnNext(1);
        _source.OnNext(2);
        _source.OnError(new InvalidOperationException("source failed"));
        _gate.SetResult(10);
        await _terminated.Task.WaitAsync(_deadline);
    }

    [Fact] void should_deliver_the_result_being_processed_before_the_error() => _events.ShouldContainOnly(["next:10", "error:source failed"]);
    [Fact] void should_deliver_the_error_last() => _events[^1].ShouldEqual("error:source failed");
    [Fact] void should_drop_the_waiting_item() => _processed.ShouldContainOnly([1]);
}
