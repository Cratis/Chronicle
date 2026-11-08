// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering_a_read_model_again;

public class and_its_sink_is_unchanged : given_named_sinks
{
    async Task Establish() => await _manager.Register([Orders("reporting")]);

    Task Because() => _manager.Register([Orders("Reporting") with { DisplayName = "Order overview" }]);

    [Fact] void should_not_replay() => _sinkChangeReplays.DidNotReceiveWithAnyArgs().ReplayFor(default!, default!);
}
