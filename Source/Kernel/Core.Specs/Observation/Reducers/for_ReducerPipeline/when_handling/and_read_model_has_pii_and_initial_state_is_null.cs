// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_handling;

public class and_read_model_has_pii_and_initial_state_is_null : given.all_dependencies
{
    ExpandoObject _returnedState;

    void Establish()
    {
        _schema.Properties["name"] = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                {
                    ComplianceJsonSchemaExtensions.ComplianceKey,
                    new[] { new ComplianceSchemaMetadata(Guid.NewGuid(), string.Empty) }
                }
            }
        };

        _returnedState = new ExpandoObject();
        _sink.FindOrDefault(Arg.Any<Concepts.Keys.Key>()).Returns(Task.FromResult<ExpandoObject?>(null));
    }

    async Task Because() => await _pipeline.Handle(
        CreateContext(EventSourceIdValue),
        CreateReducer(_returnedState));

    [Fact] void should_not_call_release() => _complianceManager.DidNotReceive().Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>());
    [Fact] void should_call_apply_with_event_source_id_as_identifier() => _complianceManager.Received(1).Apply(EventStore, EventStoreNamespace, Arg.Any<JsonSchema>(), EventSourceIdValue, Arg.Any<JsonObject>());
}
