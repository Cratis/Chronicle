// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering_a_read_model_again;

public class and_it_was_stored_without_a_sink_name : given_named_sinks
{
    /// <summary>A definition stored before sinks had names carries none; registering it again with the default sink is no change.</summary>
    async Task Establish() => await _manager.Register([Orders(null)]);

    Task Because() => _manager.Register([Orders(SinkName.Default) with { DisplayName = "Order overview" }]);

    [Fact] void should_not_replay() => _sinkChangeReplays.DidNotReceiveWithAnyArgs().ReplayFor(default!, default!);
}
