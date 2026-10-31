// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Chronicle.Reactors.for_ReactorInvoker.when_discovering_event_types;

public class and_registries_have_distinct_event_types : Specification
{
    IEventTypes _first;
    IEventTypes _second;
    IReactorSideEffectHandlers _handlers;
    readonly List<IImmutableList<EventType>> _results = [];

    void Establish()
    {
        _first = Registry(typeof(First), "first");
        _second = Registry(typeof(Second), "second");
        _handlers = Substitute.For<IReactorSideEffectHandlers>();
    }

    void Because()
    {
        foreach (var handlers in new IReactorSideEffectHandlers?[] { null, _handlers })
        {
            foreach (var registry in new[] { _first, _second, _second, _first })
            {
                _results.Add(ReactorInvoker.GetEventTypesFor(registry, typeof(Reactor), handlers));
            }
        }
    }

    [Fact] void should_use_only_the_first_registry_for_its_subscriptions() =>
        _results.Where((_, index) => index % 4 is 0 or 3).All(types => types.Count == 1 && types[0].Id.Value == "first").ShouldBeTrue();
    [Fact] void should_use_only_the_second_registry_for_its_subscriptions() =>
        _results.Where((_, index) => index % 4 is 1 or 2).All(types => types.Count == 1 && types[0].Id.Value == "second").ShouldBeTrue();

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
    public class Reactor : IReactor
    {
        public Task Handle(IKnown @event) => Task.CompletedTask;
    }
}
