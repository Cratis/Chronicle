// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.ReadModels.for_SinkChangeReplays.when_replaying;

public class and_starting_with_a_manual_policy : given.a_read_model_filled_by_a_projection_and_a_reducer
{
    protected override DefinitionEvolutionPolicy Policy => DefinitionEvolutionPolicy.Manual;

    async Task Because() => await _replays.Start(await _replays.Request(_eventStore, _readModel));

    [Fact] void should_leave_the_replays_recommended() => _recommendations.DidNotReceiveWithAnyArgs().Perform(default!);
}
