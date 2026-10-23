// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_a_from_event_key_is_explicitly_the_event_source_id : given.a_child_collection_projection
{
    static readonly EventType _eventType = new("ValueSet", EventTypeGeneration.First);
    static readonly EventSourceId _eventSourceId = "source";
    IProjection _projection;
    KeyResolverResult _key;

    void Establish()
    {
        _definition.Children.Clear();
        _definition.From[_eventType] = new FromDefinition(new Dictionary<PropertyPath, string>(), WellKnownExpressions.EventSourceId, null);
    }

    async Task Because()
    {
        _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, []);
        var @event = new AppendedEvent(EventContext.EmptyWithEventSourceId(_eventSourceId) with { EventType = _eventType }, new ExpandoObject());
        _key = await _projection.GetKeyResolverFor(_eventType)(_eventSequenceStorage, NullSink.Instance, @event);
    }

    [Fact] void should_be_event_source_keyed() => _projection.IsEventSourceKeyed.ShouldBeTrue();
    [Fact] void should_resolve_the_event_source_id() => ((ResolvedKey)_key).Key.Value.ShouldEqual(_eventSourceId.Value);
}
