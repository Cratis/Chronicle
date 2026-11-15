// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_SinkChangeReplays.when_replaying;

public class and_a_replay_is_refused : given.a_read_model_filled_by_a_projection_and_a_reducer
{
    Exception _error;

    void Establish() => _recommendations.Perform(default!).ReturnsForAnyArgs(Task.FromException(new InvalidOperationException("refused")));

    async Task Because() => _error = await Catch.Exception(async () => await _replays.Start(await _replays.Request(_eventStore, _readModel)));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_still_try_every_replay() => _recommendations.ReceivedWithAnyArgs(2).Perform(default!);
}
