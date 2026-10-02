// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.ReadModels.for_LatestWinsObservableExtensions.when_selecting_latest_sequentially;

public class and_it_is_disposed_while_a_selector_is_in_flight : given.a_latest_wins_pipeline
{
    IDisposable _subscription;

    void Establish() => _subscription = SubscribeWithGatedFirstItem();

    void Because()
    {
        _source.OnNext(1);
        _source.OnNext(2);
        _subscription.Dispose();
        _gate.SetResult(10);
    }

    [Fact] void should_not_deliver_anything() => _events.ShouldBeEmpty();
    [Fact] void should_not_process_the_waiting_item() => _processed.ShouldContainOnly([1]);
}
