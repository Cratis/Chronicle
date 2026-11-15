// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering_a_read_model_again;

public class and_its_sink_name_changed : given_named_sinks
{
    readonly List<string> _order = [];

    async Task Establish()
    {
        await _manager.Register([Orders()]);
        _sinkChangeReplays.When(_ => _.Request(Arg.Any<Concepts.EventStoreName>(), Arg.Any<ReadModelIdentifier>())).Do(_ => _order.Add("request"));
        _readModelGrain.When(_ => _.SetDefinition(Arg.Any<ReadModelDefinition>())).Do(_ => _order.Add("set-definition"));
        _sinkChangeReplays.When(_ => _.Start(Arg.Any<SinkChangeReplay>())).Do(_ => _order.Add("start"));
    }

    Task Because() => _manager.Register([Orders("reporting")]);

    [Fact] void should_request_the_replays() => _sinkChangeReplays.Received(1).Request(_eventStore, (ReadModelIdentifier)"orders");
    [Fact] void should_start_the_requested_replays() => _sinkChangeReplays.Received(1).Start(Arg.Is<SinkChangeReplay>(_ => _.ReadModel == "orders"));
    [Fact] void should_request_before_the_new_sink_is_stored_and_start_after() => _order.ShouldEqual(["request", "set-definition", "start"]);
    [Fact] async Task should_store_the_resolved_sink() => (await _manager.GetDefinitions()).Single().Sink.Type.ShouldEqual(WellKnownSinkTypes.SQL);
}
