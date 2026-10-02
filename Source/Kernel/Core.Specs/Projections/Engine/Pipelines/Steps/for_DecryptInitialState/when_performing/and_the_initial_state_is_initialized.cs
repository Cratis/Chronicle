// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_DecryptInitialState.when_performing;

public class and_the_initial_state_is_initialized : given.all_dependencies
{
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
        _schema.Properties["capacity"] = new JsonSchemaProperty { Type = JsonObjectType.Integer };

        dynamic released = new ExpandoObject();
        released.name = "released-name";
        released.capacity = 0;
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns((ExpandoObject)released);

        dynamic state = new ExpandoObject();
        state.name = "encrypted-name";
        var storedState = (IDictionary<string, object?>)(ExpandoObject)state;
        storedState[WellKnownProperties.Subject] = EventSourceIdValue;
        storedState[WellKnownProperties.ReadModelInstanceInitialized] = true;
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(_ => new JsonObject { ["name"] = "encrypted-name" });
        _context = CreateContext(state);
    }

    async Task Because() => await _step.Perform(_projection, _context);

    [Fact] void should_keep_the_schema_synthesized_property() => ((IDictionary<string, object?>)_context.Changeset.InitialState)["capacity"].ShouldEqual(0);
    [Fact] void should_restore_the_initialization_flag() => ((IDictionary<string, object?>)_context.Changeset.InitialState)[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
    [Fact] void should_keep_the_released_value_of_a_present_property() => ((IDictionary<string, object?>)_context.Changeset.InitialState)["name"].ShouldEqual("released-name");
}
