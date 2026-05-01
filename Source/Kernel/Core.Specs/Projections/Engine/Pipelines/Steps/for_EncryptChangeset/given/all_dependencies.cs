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

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.given;

public class all_dependencies : Specification
{
    protected static readonly EventStoreName EventStore = "test-store";
    protected static readonly EventStoreNamespaceName EventStoreNamespace = "test-namespace";
    protected const string EventSourceIdValue = "event-source-id";
    protected const string SubjectValue = "explicit-subject";

    protected IJsonComplianceManager _complianceManager;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected IObjectComparer _objectComparer;
    protected IProjection _projection;
    protected EncryptChangeset _step;
    protected JsonSchema _schema;

    void Establish()
    {
        _complianceManager = Substitute.For<IJsonComplianceManager>();
        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();
        _objectComparer = Substitute.For<IObjectComparer>();
        _projection = Substitute.For<IProjection>();

        _schema = new JsonSchema();
        _projection.TargetReadModelSchema.Returns(_schema);

        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(_ => new JsonObject());
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>())
            .Returns(_ => new ExpandoObject());

        _complianceManager.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(ci => Task.FromResult(new JsonObject()));

        _objectComparer.Compare(Arg.Any<ExpandoObject>(), Arg.Any<ExpandoObject>(), out Arg.Any<IEnumerable<PropertyDifference>>())
            .Returns(true);  // true = equal (no differences)

        _step = new EncryptChangeset(_complianceManager, _expandoObjectConverter, _objectComparer, EventStore, EventStoreNamespace);
    }

    protected ProjectionEventContext CreateContext(string eventSourceId, Subject? subject = null)
    {
        var @event = new AppendedEvent(
            EventContext.From(
                EventStore,
                EventStoreNamespace,
                EventType.Unknown,
                EventSourceType.Default,
                eventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                EventSequenceNumber.First,
                CorrelationId.NotSet,
                subject: subject),
            new ExpandoObject());

        var changeset = new Changeset<AppendedEvent, ExpandoObject>(_objectComparer, @event, new ExpandoObject());

        return new ProjectionEventContext(
            new Key(eventSourceId, new ArrayIndexers([])),
            @event,
            changeset,
            ProjectionOperationType.None,
            false);
    }
}
