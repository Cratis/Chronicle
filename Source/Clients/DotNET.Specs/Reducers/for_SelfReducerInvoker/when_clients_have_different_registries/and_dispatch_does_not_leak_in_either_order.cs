// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_SelfReducerInvoker.when_clients_have_different_registries;

public class and_dispatch_does_not_leak_in_either_order : Specification
{
    string[] _identities;

    void Because()
    {
        var first = Registry(typeof(First), "first");
        var second = Registry(typeof(Second), "second");
        var reverseSecond = Registry(typeof(Second), "second");
        var reverseFirst = Registry(typeof(First), "first");
        _identities = [Invoker(first).EventTypes.Single().Id.Value, Invoker(second).EventTypes.Single().Id.Value,
            Invoker(reverseSecond).EventTypes.Single().Id.Value, Invoker(reverseFirst).EventTypes.Single().Id.Value];
    }

    static IEventTypes Registry(Type type, string id)
    {
        var registry = Substitute.For<IEventTypes>();
        registry.AllClrTypes.Returns(ImmutableList.Create(type));
        registry.GetEventTypeFor(type).Returns(new EventType(id, 1));
        return registry;
    }

    static SelfReducerInvoker Invoker(IEventTypes registry) => new(registry, typeof(State), "state");

    [Fact] void should_retain_the_owning_registry_in_both_orders() => _identities.ShouldEqual(["first", "second", "second", "first"]);

    public interface IEvent;
    public record First : IEvent;
    public record Second : IEvent;
    public record State
    {
        public static State Reduce(IEvent @event, State? current) => new();
    }
}
