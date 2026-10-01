// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.ReadModels.for_LatestWinsObservableExtensions.when_selecting_latest_sequentially;

public class and_the_selector_throws : given.a_latest_wins_pipeline
{
    void Establish() => Subscribe(_ => Task.FromException<int>(new InvalidOperationException("selector failed")));

    async Task Because()
    {
        _source.OnNext(1);
        await _terminated.Task.WaitAsync(_deadline);
    }

    [Fact] void should_pass_the_failure_on_as_an_error() => _events.ShouldContainOnly(["error:selector failed"]);
    [Fact] void should_not_log_a_failure() => _logger.Exceptions.ShouldBeEmpty();
}
