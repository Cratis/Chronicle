// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_SelfReducerInvoker.when_resolving_handlers;

public class and_event_specificity_is_unique : Specification
{
    string _exact;
    string _base;
    string _interface;

    async Task Because()
    {
        var registry = Substitute.For<IEventTypes>();
        registry.AllClrTypes.Returns(ImmutableList.Create(typeof(Derived), typeof(Base), typeof(InterfaceOnly)));
        registry.GetEventTypeFor(Arg.Any<Type>()).Returns(call => new EventType(call.Arg<Type>().Name, 1));
        var invoker = new SelfReducerInvoker(registry, typeof(State), "state");
        _exact = await Reduce(invoker, new Derived());
        _base = await Reduce(invoker, new Base());
        _interface = await Reduce(invoker, new InterfaceOnly());
    }

    static async Task<string> Reduce(SelfReducerInvoker invoker, object @event)
    {
        var result = await invoker.Invoke(Substitute.For<IServiceProvider>(), [new(@event, EventContext.Empty)], null);
        return ((State)result.ReadModelState!).Handler;
    }

    [Fact] void should_choose_exact_over_base_and_interface() => _exact.ShouldEqual("exact");
    [Fact] void should_choose_base_over_interface() => _base.ShouldEqual("base");
    [Fact] void should_dispatch_registered_interface_implementations() => _interface.ShouldEqual("interface");

    public interface IEvent;
    public record Base : IEvent;
    public record Derived : Base;
    public record InterfaceOnly : IEvent;

    public record State(string Handler)
    {
        public static State Reduce(IEvent @event, State? current) => new("interface");
        public static State Reduce(Base @event, State? current) => new("base");
        public static State Reduce(Derived @event, State? current) => new("exact");
    }
}
