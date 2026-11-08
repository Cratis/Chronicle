// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering_a_read_model_again;

public class and_its_sink_name_changed : given_named_sinks
{
    async Task Establish() => await _manager.Register([Orders()]);

    Task Because() => _manager.Register([Orders("reporting")]);

    [Fact] void should_replay_into_the_new_sink() => _sinkChangeReplays.Received(1).ReplayFor(_eventStore, (ReadModelIdentifier)"orders");
    [Fact] async Task should_store_the_resolved_sink() => (await _manager.GetDefinitions()).Single().Sink.Type.ShouldEqual(WellKnownSinkTypes.SQL);
}
