// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges.given;

public class a_save_changes_step : Specification
{
    protected RecordingSink _sink;
    protected IProjection _projection;
    protected ProjectionEventContext _context;

    void Establish()
    {
        _sink = new RecordingSink();

        _projection = Substitute.For<IProjection>();
        _projection.Identifier.Returns(new ProjectionId("test-projection"));
        _projection.IsEventSourceKeyed.Returns(true);
        _projection.ReadModel.Returns(CreateReadModelDefinition());

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.HasChanges.Returns(true);
        changeset.Changes.Returns([]);
        changeset.CurrentState.Returns(new ExpandoObject());

        _context = new ProjectionEventContext(
            new Key("test-key", ArrayIndexers.NoIndexers),
            AppendedEvent.EmptyWithEventType(new EventType("TestEvent", EventTypeGeneration.First)),
            changeset,
            ProjectionOperationType.None,
            NeedsInitialState: false);
    }

    protected SaveChanges CreateStep(bool guardWritesOnWatermark) =>
        new(_sink, Substitute.For<IChangesetStorage>(), guardWritesOnWatermark, Substitute.For<ILogger<SaveChanges>>());

    protected SinkWriteMode WriteMode => _sink.WriteModes.Single();

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "test-read-model",
            "TestReadModel",
            "TestReadModel",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, new JsonSchema() }
            },
            []);
}
