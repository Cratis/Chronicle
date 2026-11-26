// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_performing;

public class and_the_protected_document_is_kept_for_notification : given.all_dependencies
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

        _context = CreateContext(EventSourceIdValue);
        var state = (IDictionary<string, object?>)_context.Changeset.CurrentState;
        state["name"] = "plain-name";
        state[WellKnownProperties.ReadModelInstanceInitialized] = true;

        dynamic encrypted = new ExpandoObject();
        encrypted.name = "encrypted-name";
        _encryptedState = encrypted;
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(_encryptedState);
    }

    async Task Because() => await _step.Perform(_projection, _context);

    [Fact] void should_keep_the_document_handed_to_the_sink() => _context.ProtectedState.ShouldBeSame(_encryptedState);
    [Fact] void should_leave_the_current_state_in_plaintext() => ((IDictionary<string, object?>)_context.Changeset.CurrentState)["name"].ShouldEqual("plain-name");
}
