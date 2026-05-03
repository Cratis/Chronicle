// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_DecryptInitialState.given;

public class all_dependencies : Specification
{
    protected static readonly EventStoreName EventStore = "test-store";
    protected static readonly EventStoreNamespaceName EventStoreNamespace = "test-namespace";
    protected const string EventSourceIdValue = "event-source-id";

    protected IJsonComplianceManager _complianceManager;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected IProjection _projection;
    protected DecryptInitialState _step;
    protected JsonSchema _schema;
    protected IChangeset<AppendedEvent, ExpandoObject> _changeset;
    protected IObjectComparer _objectComparer;

    void Establish()
    {
        _complianceManager = Substitute.For<IJsonComplianceManager>();
        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();
        _projection = Substitute.For<IProjection>();
        _objectComparer = Substitute.For<IObjectComparer>();

        _schema = new JsonSchema();
        _projection.TargetReadModelSchema.Returns(_schema);

        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(_ => new JsonObject());
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>())
            .Returns(_ => new ExpandoObject());

        _complianceManager.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(ci => Task.FromResult(new JsonObject()));

        _step = new DecryptInitialState(_complianceManager, _expandoObjectConverter, EventStore, EventStoreNamespace);
    }

    protected ProjectionEventContext CreateContext(ExpandoObject? initialState)
    {
        var @event = new AppendedEvent(
            EventContext.From(
                EventStore,
                EventStoreNamespace,
                EventType.Unknown,
                EventSourceType.Default,
                EventSourceIdValue,
                EventStreamType.All,
                EventStreamId.Default,
                EventSequenceNumber.First,
                CorrelationId.NotSet),
            new ExpandoObject());

        var changeset = new Changeset<AppendedEvent, ExpandoObject>(_objectComparer, @event, initialState ?? new ExpandoObject());
        if (initialState is not null)
        {
            changeset.InitialState = initialState;
        }

        return new ProjectionEventContext(
            new Key(EventSourceIdValue, new ArrayIndexers([])),
            @event,
            changeset,
            ProjectionOperationType.None,
            false);
    }
}
