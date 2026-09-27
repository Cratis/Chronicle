// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorBuilder.when_handling_an_event;

public class and_a_handler_fails : given.a_reactor_builder
{
    readonly List<string> _calls = [];
    IReactorDefinition _definition;
    Exception _error;

    void Establish()
    {
        _builder
            .On<OrderPlaced>(_ => throw new Exception("Boom"))
            .Subscribe((_, _) => _calls.Add("catch-all"));
        _definition = _builder.Build("orders");
    }

    async Task Because() => _error = await Catch.Exception(() => _definition.Handle(new OrderPlaced("42"), EventContext.Empty));

    [Fact] void should_surface_the_failure() => _error.Message.ShouldEqual("Boom");
    [Fact] void should_not_call_later_handlers() => _calls.ShouldBeEmpty();
}
