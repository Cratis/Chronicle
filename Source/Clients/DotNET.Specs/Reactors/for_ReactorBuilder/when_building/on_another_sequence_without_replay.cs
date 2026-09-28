// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorBuilder.when_building;

public class on_another_sequence_without_replay : given.a_reactor_builder
{
    IReactorDefinition _definition;

    void Establish() => _builder
        .On<OrderPlaced>(_ => { })
        .OnEventSequence("outbox")
        .NotReplayable();

    void Because() => _definition = _builder.Build("orders");

    [Fact] void should_observe_the_sequence() => _definition.EventSequenceId.ShouldEqual((EventSequenceId)"outbox");
    [Fact] void should_not_be_replayable() => _definition.IsReplayable.ShouldBeFalse();
}
