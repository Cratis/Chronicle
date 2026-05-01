// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_DecryptInitialState.when_performing;

public class and_initial_state_has_pii_and_subject_is_stored : given.all_dependencies
{
    const string StoredSubject = "stored-subject";
    ProjectionEventContext _context;
    ProjectionEventContext _result;

    void Establish()
    {
        var property = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata(Guid.NewGuid(), string.Empty) } }
            }
        };
        _schema.Properties["name"] = property;

        dynamic state = new ExpandoObject();
        state.name = "encrypted-name";
        ((IDictionary<string, object?>)(ExpandoObject)state)[WellKnownProperties.Subject] = StoredSubject;
        _context = CreateContext(state);
    }

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_call_compliance_manager_release() => _complianceManager.Received(1).Release(EventStore, EventStoreNamespace, Arg.Any<JsonSchema>(), StoredSubject, Arg.Any<JsonObject>());
}
