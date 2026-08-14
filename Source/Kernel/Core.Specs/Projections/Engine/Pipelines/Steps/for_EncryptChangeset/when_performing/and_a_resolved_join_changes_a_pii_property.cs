// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_performing;

public class and_a_resolved_join_changes_a_pii_property : given.all_dependencies
{
    const string DefaultSubject = "document-owner";
    const string ResolvedSubject = "resolved-owner";

    ProjectionEventContext _context;

    void Establish()
    {
        _schema.Properties["name"] = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata("PII", string.Empty) } }
            }
        };

        _context = CreateContext(EventSourceIdValue);
        var state = (IDictionary<string, object?>)_context.Changeset.CurrentState;
        state[WellKnownProperties.Subject] = DefaultSubject;
        state["name"] = "value";

        var resolvedEvent = new AppendedEvent(
            EventContext.From(
                EventStore,
                EventStoreNamespace,
                EventType.Unknown,
                EventSourceType.Default,
                "resolved-event-source",
                EventStreamType.All,
                EventStreamId.Default,
                EventSequenceNumber.First,
                CorrelationId.NotSet,
                subject: (Subject)ResolvedSubject),
            new ExpandoObject());
        var propertyChange = new PropertiesChanged<ExpandoObject>(
            _context.Changeset.CurrentState,
            [new PropertyDifference(new PropertyPath("name"), null, "value")]);

        _context.Changeset.Add(new ResolvedJoin(
            _context.Changeset.CurrentState,
            "resolved-event-source",
            PropertyPath.Root,
            ArrayIndexers.NoIndexers,
            [propertyChange],
            resolvedEvent));
    }

    async Task Because() => await _step.Perform(_projection, _context);

    [Fact] void should_encrypt_the_property_under_the_resolved_events_subject() =>
        _complianceManager.Received().Apply(EventStore, EventStoreNamespace, _schema, ResolvedSubject, Arg.Is<JsonObject>(_ => _.ContainsKey("name")));

    [Fact]
    void should_track_the_resolved_property_subject()
    {
        var state = (IDictionary<string, object?>)_context.Changeset.CurrentState;
        var subjects = ReadModelSubjects.From(state[WellKnownProperties.Subjects]);
        subjects["name"].ShouldEqual(ResolvedSubject);
    }
}
