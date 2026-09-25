// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_a_delegate;

public class and_with_a_configured_sequence : given.all_dependencies
{
    IReactorHandler _handler;

    async Task Because()
    {
        _handler = await _reactors.Register(
            "runtime-reactor",
            builder => builder.WithEventType(new EventType("configured-event", 2)).OnEventSequence("configured-sequence").NotReplayable(),
            (_, _) => Task.CompletedTask);
        await _reactors.Register();
    }

    [Fact] void should_find_the_handler_by_id() => _reactors.GetHandlerById("runtime-reactor").ShouldEqual(_handler);
    [Fact] void should_register_both_the_id_and_generation() => _handler.EventTypes.ShouldContain(new EventType("configured-event", 2));
    [Fact] void should_observe_the_configured_sequence() => _handler.EventSequenceId.ShouldEqual((EventSequenceId)"configured-sequence");
    [Fact] void should_only_open_one_observation_stream() => _services.Reactors.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(Contracts.Observation.Reactors.IReactors.Observe)).ShouldEqual(1);
}
