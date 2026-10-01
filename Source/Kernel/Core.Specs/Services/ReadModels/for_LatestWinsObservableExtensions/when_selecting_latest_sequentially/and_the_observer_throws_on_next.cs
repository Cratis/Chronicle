// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.ReadModels.for_LatestWinsObservableExtensions.when_selecting_latest_sequentially;

public class and_the_observer_throws_on_next : given.a_latest_wins_pipeline
{
    InvalidOperationException _failure;

    void Establish()
    {
        _failure = new("observer failed");
        Subscribe(item => Task.FromResult(item * 10), _ => throw _failure);
    }

    /// <summary>
    /// The selector completes synchronously, so the failure has been dealt with by the time OnNext returns.
    /// </summary>
    void Because() => _source.OnNext(1);

    [Fact] void should_not_pass_the_failure_back_to_the_observer_as_an_error() => _events.ShouldContainOnly(["next:10"]);
    [Fact] void should_log_the_failure() => _logger.Exceptions.ShouldContainOnly([_failure]);
    [Fact] void should_stop_listening_to_the_source() => _source.HasObservers.ShouldBeFalse();
}
