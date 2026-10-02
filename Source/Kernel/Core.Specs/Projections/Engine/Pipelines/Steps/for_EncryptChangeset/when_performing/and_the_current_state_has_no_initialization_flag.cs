// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_performing;

public class and_the_current_state_has_no_initialization_flag : given.all_dependencies
{
    ProjectionEventContext _context;
    ExpandoObject _encryptedState;

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

        _context = CreateContext(EventSourceIdValue);
        var state = (IDictionary<string, object?>)_context.Changeset.CurrentState;
        state["name"] = "plain-name";

        dynamic encrypted = new ExpandoObject();
        encrypted.name = "encrypted-name";
        encrypted.capacity = 0;
        _encryptedState = encrypted;
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(_encryptedState);
    }

    async Task Because() => await _step.Perform(_projection, _context);

    [Fact] void should_keep_the_schema_synthesized_property_for_a_legacy_initialized_document() => ((IDictionary<string, object?>)_encryptedState)["capacity"].ShouldEqual(0);
    [Fact] void should_not_add_an_initialization_flag_to_a_legacy_document() => ((IDictionary<string, object?>)_encryptedState).ContainsKey(WellKnownProperties.ReadModelInstanceInitialized).ShouldBeFalse();
    [Fact] void should_keep_the_encrypted_value_of_a_present_property() => ((IDictionary<string, object?>)_encryptedState)["name"].ShouldEqual("encrypted-name");
    [Fact] void should_compare_the_unfiltered_encrypted_state() => _objectComparer.Received(1).Compare(_context.Changeset.CurrentState, _encryptedState, out Arg.Any<IEnumerable<PropertyDifference>>());
}
