// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers.when_the_previous_contract_is_called_directly;

/// <summary>
/// The previous dispatcher contract answered <see langword="false"/> when no registered handler accepted a value.
/// Keeping that answer matters independently of the new event-store-aware path.
/// </summary>
public class and_no_handler_can_handle_the_value : Specification
{
    ReactorSideEffectHandlers _handlers;
    bool _result;

    void Establish() => _handlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([]));

    void Because() => _result = _handlers.CanHandle(
        new ReactorContext(Events.EventContext.Empty, new object(), ReactorContextValues.Empty),
        new object());

    [Fact] void should_answer_false() => _result.ShouldBeFalse();
}
