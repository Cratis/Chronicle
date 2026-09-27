// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorBuilder.when_handling_an_event;

public class without_a_typed_handler_for_it : given.a_reactor_builder
{
    readonly List<object> _typed = [];
    readonly List<object> _catchAll = [];
    readonly CustomerRegistered _event = new("Jane");
    IReactorDefinition _definition;

    void Establish()
    {
        _builder
            .On<OrderPlaced>(_typed.Add)
            .Subscribe((@event, _) =>
            {
                _catchAll.Add(@event);
                return Task.CompletedTask;
            });
        _definition = _builder.Build("orders");
    }

    Task Because() => _definition.Handle(_event, EventContext.Empty);

    [Fact] void should_not_call_the_typed_handler() => _typed.ShouldBeEmpty();
    [Fact] void should_call_the_async_catch_all() => _catchAll.ShouldContainOnly([_event]);
}
