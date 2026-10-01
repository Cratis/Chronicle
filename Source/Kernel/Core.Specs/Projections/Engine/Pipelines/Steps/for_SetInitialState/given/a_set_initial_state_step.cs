// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SetInitialState.given;

public class a_set_initial_state_step : Specification
{
    protected ISink _sink;
    protected IProjection _projection;
    protected SetInitialState _step;

    void Establish()
    {
        _sink = Substitute.For<ISink>();
        _sink.FindOrDefault(Arg.Any<Key>()).Returns((ExpandoObject?)null);
        _projection = Substitute.For<IProjection>();
        _projection.InitialModelState.Returns(new ExpandoObject());
        var schema = new JsonSchema();
        schema.Properties["id"] = new JsonSchemaProperty("id", new JsonObject { ["type"] = "string" }, schema);
        _projection.TargetReadModelSchema.Returns(schema);
        _step = new SetInitialState(_sink, NullLogger<SetInitialState>.Instance);
    }

    protected static ProjectionEventContext CreateContext(ProjectionOperationType operationType)
    {
        var @event = AppendedEvent.EmptyWithEventType(new EventType("TheEvent", EventTypeGeneration.First));
        return new ProjectionEventContext(
            new Key("the-key", ArrayIndexers.NoIndexers),
            @event,
            new Changeset<AppendedEvent, ExpandoObject>(Substitute.For<IObjectComparer>(), @event, new ExpandoObject()),
            operationType,
            false);
    }
}
