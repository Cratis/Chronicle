// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_handling;

public class and_read_model_is_rekeyed : given.all_dependencies
{
    const string ResolvedKey = "resolved-key";

    ExpandoObject _returnedState;

    void Establish()
    {
        _schema.Properties["name"] = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                {
                    ComplianceJsonSchemaExtensions.ComplianceKey,
                    new[] { new ComplianceSchemaMetadata("PII", string.Empty) }
                }
            }
        };

        _returnedState = new ExpandoObject();
        _sink.FindOrDefault(Arg.Any<Concepts.Keys.Key>()).Returns(Task.FromResult<ExpandoObject?>(null));
    }

    async Task Because() => await _pipeline.Handle(
        CreateContext(EventSourceIdValue, key: ResolvedKey),
        CreateReducer(_returnedState));

    [Fact] void should_call_apply_with_the_resolved_key_as_identifier() => _complianceManager.Received(1).Apply(EventStore, EventStoreNamespace, Arg.Any<JsonSchema>(), ResolvedKey, Arg.Any<JsonObject>());
    [Fact] void should_not_use_the_event_source_id_as_identifier() => _complianceManager.DidNotReceive().Apply(EventStore, EventStoreNamespace, Arg.Any<JsonSchema>(), EventSourceIdValue, Arg.Any<JsonObject>());
}
