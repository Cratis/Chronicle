// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker.when_creating_for;

public class and_registries_have_distinct_event_types : Specification
{
    IEventTypes _first;
    IEventTypes _second;
    readonly List<ReducerInvoker> _invokers = [];

    void Establish()
    {
        _first = Registry(typeof(First), "first");
        _second = Registry(typeof(Second), "second");
    }

    void Because()
    {
        foreach (var registry in new[] { _first, _second, _second, _first })
        {
            _invokers.Add(new(registry, Substitute.For<IClientArtifactsActivator>(), typeof(Reducer), typeof(Model), nameof(Model)));
        }
    }

    [Fact] void should_use_the_first_registry_in_both_initialization_orders() =>
        new[] { _invokers[0], _invokers[3] }.All(invoker => invoker.EventTypes.Count == 1 && invoker.EventTypes[0].Id.Value == "first").ShouldBeTrue();
    [Fact] void should_use_the_second_registry_in_both_initialization_orders() =>
        new[] { _invokers[1], _invokers[2] }.All(invoker => invoker.EventTypes.Count == 1 && invoker.EventTypes[0].Id.Value == "second").ShouldBeTrue();

    static IEventTypes Registry(Type type, string id)
    {
        var registry = Substitute.For<IEventTypes>();
        registry.AllClrTypes.Returns(new[] { type }.ToImmutableList());
        registry.GetEventTypeFor(type).Returns(new EventType(id, 1));
        return registry;
    }

    public interface IKnown;
    public record First : IKnown;
    public record Second : IKnown;
    public record Model;
    public class Reducer : IReducerFor<Model>
    {
        public Model Handle(IKnown @event, Model? current) => new();
    }
}
