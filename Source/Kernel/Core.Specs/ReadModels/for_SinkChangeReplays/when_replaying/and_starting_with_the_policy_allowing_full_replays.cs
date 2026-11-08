// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_SinkChangeReplays.when_replaying;

public class and_starting_with_the_policy_allowing_full_replays : given.a_read_model_filled_by_a_projection_and_a_reducer
{
    async Task Because() => await _replays.Start(await _replays.Request(_eventStore, _readModel));

    [Fact] void should_perform_every_recorded_replay() => _recommendations.ReceivedWithAnyArgs(2).Perform(default!);
}
