// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorBuilder.when_handling_an_event;

public class with_every_kind_of_handler : given.a_reactor_builder
{
    readonly List<string> _calls = [];
    readonly OrderPlaced _event = new("42");
    readonly EventContext _context = EventContext.Empty with { SequenceNumber = 7 };
    EventContext _receivedContext;
    EventContext _receivedAsyncContext;
    EventContext _receivedCatchAllContext;
    object _receivedCatchAllEvent;
    IReactorDefinition _definition;

    void Establish()
    {
        _builder
            .On<OrderPlaced>(_ => _calls.Add("event"))
            .On<OrderPlaced>((_, context) =>
            {
                _calls.Add("event and context");
                _receivedContext = context;
            })
            .On<OrderPlaced>(_ =>
            {
                _calls.Add("async event");
                return Task.CompletedTask;
            })
            .On<OrderPlaced>((_, context) =>
            {
                _calls.Add("async event and context");
                _receivedAsyncContext = context;
                return Task.CompletedTask;
            })
            .On<OrderShipped>(_ => _calls.Add("other event"))
            .Subscribe((@event, context) =>
            {
                _calls.Add("catch-all");
                _receivedCatchAllEvent = @event;
                _receivedCatchAllContext = context;
            });
        _definition = _builder.Build("orders");
    }

    Task Because() => _definition.Handle(_event, _context);

    [Fact] void should_call_the_handlers_for_the_type_in_order_and_the_catch_all_last() => string.Join(" | ", _calls).ShouldEqual("event | event and context | async event | async event and context | catch-all");
    [Fact] void should_pass_the_context() => _receivedContext.ShouldEqual(_context);
    [Fact] void should_pass_the_context_to_the_async_handler() => _receivedAsyncContext.ShouldEqual(_context);
    [Fact] void should_pass_the_event_to_the_catch_all() => _receivedCatchAllEvent.ShouldEqual(_event);
    [Fact] void should_pass_the_context_to_the_catch_all() => _receivedCatchAllContext.ShouldEqual(_context);
}
