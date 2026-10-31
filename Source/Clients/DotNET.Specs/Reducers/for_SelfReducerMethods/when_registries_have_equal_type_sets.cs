// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_SelfReducerMethods;

public class when_registries_have_equal_type_sets : Specification
{
    FrozenDictionary<Type, MethodInfo> _first;
    FrozenDictionary<Type, MethodInfo> _second;
    IEventTypes _registry;
    SelfReducerInvoker _invoker;

    void Establish()
    {
        _registry = Registry([typeof(First), typeof(Second)], "original");
        _first = SelfReducerMethods.Get(_registry, typeof(State));
    }

    void Because()
    {
        var other = Registry([typeof(Second), typeof(First)], "other");
        _second = SelfReducerMethods.Get(other, typeof(State));
        _invoker = new(other, typeof(State), "state");
    }

    [Fact] void should_reuse_the_immutable_map() => _second.ShouldBeSame(_first);
    [Fact] void should_keep_exact_method_precedence() => _second[typeof(First)].GetParameters()[0].ParameterType.ShouldEqual(typeof(First));
    [Fact] void should_keep_interface_dispatch() => _second[typeof(Second)].GetParameters()[0].ParameterType.ShouldEqual(typeof(IEvent));
    [Fact] void should_resolve_identities_from_the_owning_registry() => _invoker.EventTypes.Select(type => type.Id.Value).ShouldContainOnly("other-First", "other-Second");

    static IEventTypes Registry(Type[] types, string prefix)
    {
        var registry = Substitute.For<IEventTypes>();
        registry.AllClrTypes.Returns(types.ToImmutableList());
        foreach (var type in types)
        {
            registry.GetEventTypeFor(type).Returns(new EventType($"{prefix}-{type.Name}", 1));
        }
        return registry;
    }

    public interface IEvent;
    public record First : IEvent;
    public record Second : IEvent;
    public record State
    {
        public static State Reduce(IEvent @event, State? current) => new();
        public static State Reduce(First @event, State? current) => new();
    }
}
