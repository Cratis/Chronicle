// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Seeding.for_IEventSeedingBuilder;

public class when_routing_with_a_legacy_builder : Specification
{
    IEventSeedingBuilder _builder;
    IEventSeedingScopeBuilder _scope;
    Exception[] _errors;

    void Establish()
    {
        _builder = new LegacyBuilder();
        _scope = _builder.ForNamespace("tenant");
    }

    void Because() => _errors = [
        Catch.Exception(() => _builder.For<object>("source", "Stream", "id", [])),
        Catch.Exception(() => _builder.ForEventSource("source", "Stream", "id", [])),
        Catch.Exception(() => _builder.ForEvents([])),
        Catch.Exception(() => _scope.For<object>("source", "Stream", "id", [])),
        Catch.Exception(() => _scope.ForEventSource("source", "Stream", "id", [])),
        Catch.Exception(() => _scope.ForEvents([]))
    ];

    [Fact] void should_refuse_all_new_routing_members() => _errors.All(_ => _ is EventSeedingRoutingNotSupported).ShouldBeTrue();

    class LegacyBuilder : IEventSeedingBuilder
    {
        public IEventSeedingBuilder For<TEvent>(EventSourceId eventSourceId, IEnumerable<TEvent> events)
            where TEvent : class => this;

        public IEventSeedingBuilder ForEventSource(EventSourceId eventSourceId, IEnumerable<object> events) => this;

        public IEventSeedingScopeBuilder ForNamespace(EventStoreNamespaceName @namespace) => new LegacyScopeBuilder();
    }

    class LegacyScopeBuilder : IEventSeedingScopeBuilder
    {
        public IEventSeedingScopeBuilder For<TEvent>(EventSourceId eventSourceId, IEnumerable<TEvent> events)
            where TEvent : class => this;

        public IEventSeedingScopeBuilder ForEventSource(EventSourceId eventSourceId, IEnumerable<object> events) => this;
    }
}
