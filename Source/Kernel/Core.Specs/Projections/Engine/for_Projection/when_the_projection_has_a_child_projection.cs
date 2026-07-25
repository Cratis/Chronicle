// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_Projection;

public class when_the_projection_has_a_child_projection : Specification
{
    static EventType _eventType = new("2b3c4d5e-6f70-4182-9a3b-4c5d6e7f8091", 1);

    Projection _projection;
    IKeyResolvers _keyResolvers;

    void Establish()
    {
        _keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        _projection = new Projection(
            EventSequenceId.Log,
            "5f2a1c9b-6d34-4a8e-9f01-7b2c3d4e5f60",
            new ExpandoObject(),
            string.Empty,
            string.Empty,
            string.Empty,
            new ReadModelDefinition(string.Empty, string.Empty, string.Empty, ReadModelOwner.None, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema>(), []),
            new JsonSchema(),
            true,
            AutoMap.Enabled,
            new HashSet<string>(),
            [Substitute.For<IProjection>()]);
    }

    void Because() => _projection.SetEventTypesWithKeyResolvers(
        [
            new EventTypeWithKeyResolver(_eventType, _keyResolvers.FromEventSourceId, ResolvesToEventSourceId: true)
        ],
        [_eventType],
        new Dictionary<EventType, ProjectionOperationType>());

    [Fact] void should_not_be_event_source_keyed() => _projection.IsEventSourceKeyed.ShouldBeFalse();
}
