// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_notifying_changes.given;

/// <summary>
/// A pinned subscriber whose changeset holds a PII read model state, a stale watermark and whatever lineage a
/// derived specification adds. The converter stand-in returns the schema-declared top-level keys of the state, as
/// the real converter does.
/// </summary>
public class a_change_for_a_model_with_pii : for_ProjectionObserverSubscriber.given.a_pinned_subscriber
{
    protected JsonObject _document;
    protected ReadModelChangeContext _change;
    protected ExpandoObject _state;

    protected override JsonSchema ReadModelSchema()
    {
        var schema = new JsonSchema();
        schema.Properties["name"] = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata("PII", string.Empty) } }
            }
        };
        DeclareMore(schema);
        return schema;
    }

    protected virtual void DeclareMore(JsonSchema schema)
    {
    }

    protected virtual void AddToState(IDictionary<string, object?> state)
    {
    }

    void Establish()
    {
        _event = _event with { Context = _event.Context with { EventType = _pin } };
        _state = new ExpandoObject();
        var values = (IDictionary<string, object?>)_state;
        values[WellKnownProperties.ReadModelInstanceInitialized] = true;
        values["name"] = "Ada Lovelace";
        values[WellKnownProperties.LastHandledEventSequenceNumber] = 7UL;
        AddToState(values);

        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(call =>
        {
            var schema = call.Arg<JsonSchema>();
            var document = new JsonObject();
            foreach (var (key, value) in (IDictionary<string, object?>)call.Arg<ExpandoObject>())
            {
                if (schema.Properties.ContainsKey(key))
                {
                    document[key] = JsonValue.Create(value);
                }
            }

            return document;
        });

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.HasChanges.Returns(true);
        changeset.CurrentState.Returns(_state);
        _cachedPipeline.Handle(_event).Returns(new ProjectionEventContext("the-partition", _event, changeset, ProjectionOperationType.From, false));

        _notifier.When(_ => _.Notify(Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelKey>(), Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>()))
            .Do(call =>
            {
                _document = call.Arg<JsonObject>();
                _change = call.Arg<ReadModelChangeContext>();
            });
    }
}
