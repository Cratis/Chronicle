// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_performing;

public class and_a_join_changes_a_pii_property : given.all_dependencies
{
    const string DefaultSubject = "document-owner";

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

        _context = CreateContext(EventSourceIdValue, (Subject)SubjectValue) with
        {
            OperationType = ProjectionOperationType.Join
        };
        var state = (IDictionary<string, object?>)_context.Changeset.CurrentState;
        state[WellKnownProperties.Subject] = DefaultSubject;
        state["name"] = "value";
        var propertyChange = new PropertiesChanged<ExpandoObject>(
            _context.Changeset.CurrentState,
            [new PropertyDifference(new PropertyPath("name"), null, "value")]);
        _context.Changeset.Add(new Joined(
            _context.Changeset.CurrentState,
            SubjectValue,
            new PropertyPath("subjectId"),
            ArrayIndexers.NoIndexers,
            new List<Change> { propertyChange }));

        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>())
            .Returns(_ => new { name = "encrypted" }.AsExpandoObject());
        _objectComparer.Compare(Arg.Any<ExpandoObject>(), Arg.Any<ExpandoObject>(), out Arg.Any<IEnumerable<PropertyDifference>>())
            .Returns(callInfo =>
            {
                callInfo[2] = new[] { new PropertyDifference(new PropertyPath("Name"), "value", null) };
                return false;
            });
    }

    async Task Because() => await _step.Perform(_projection, _context);

    [Fact] void should_encrypt_the_property_under_the_joined_events_subject() =>
        _complianceManager.Received().Apply(EventStore, EventStoreNamespace, _schema, SubjectValue, Arg.Is<JsonObject>(_ => _.ContainsKey("name")));

    [Fact] void should_keep_the_document_default_subject() =>
        ((IDictionary<string, object?>)_context.Changeset.CurrentState)[WellKnownProperties.Subject].ShouldEqual(DefaultSubject);

    [Fact]
    void should_track_the_joined_property_subject()
    {
        var state = (IDictionary<string, object?>)_context.Changeset.CurrentState;
        var subjects = ReadModelSubjects.From(state[WellKnownProperties.Subjects]);
        subjects["name"].ShouldEqual(SubjectValue);
    }

    [Fact] void should_persist_the_subject_map_as_a_change() => _context.Changeset.Changes
        .OfType<PropertiesChanged<ExpandoObject>>()
        .SelectMany(_ => _.Differences)
        .ShouldContain(_ => _.PropertyPath == new PropertyPath($"{WellKnownProperties.Subjects}.name"));

    [Fact] void should_replace_the_joined_change_with_the_encrypted_value() => _context.Changeset.Changes
        .OfType<Joined>()
        .SelectMany(_ => _.Changes)
        .OfType<PropertiesChanged<ExpandoObject>>()
        .SelectMany(_ => _.Differences)
        .Single(_ => _.PropertyPath == "name")
        .Changed.ShouldEqual("encrypted");

    [Fact] void should_not_persist_a_root_snapshot_difference_for_the_joined_property() => _context.Changeset.Changes
        .OfType<PropertiesChanged<ExpandoObject>>()
        .SelectMany(_ => _.Differences)
        .ShouldNotContain(_ => _.PropertyPath == "Name");
}
