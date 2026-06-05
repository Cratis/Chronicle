// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_DecryptInitialState.when_performing;

public class and_initial_state_has_pii_and_subject_is_guid : given.all_dependencies
{
    static readonly Guid _storedSubject = Guid.Parse("00000000-0001-0000-0000-000000000001");

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

        dynamic state = new ExpandoObject();
        state.name = "encrypted-name";
        ((IDictionary<string, object?>)(ExpandoObject)state)[WellKnownProperties.Subject] = _storedSubject;
        _context = CreateContext(state);
    }

    async Task Because() => await _step.Perform(_projection, _context);

    [Fact] void should_call_compliance_manager_release_with_string_identifier() => _complianceManager.Received(1).Release(EventStore, EventStoreNamespace, Arg.Any<JsonSchema>(), _storedSubject.ToString(), Arg.Any<JsonObject>());
}
