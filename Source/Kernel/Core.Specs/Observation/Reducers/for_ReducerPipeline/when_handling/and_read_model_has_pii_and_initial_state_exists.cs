// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_handling;

public class and_read_model_has_pii_and_initial_state_exists : given.all_dependencies
{
    ExpandoObject _initialState;
    ExpandoObject _returnedState;

    void Establish()
    {
        var property = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                {
                    ComplianceJsonSchemaExtensions.ComplianceKey,
                    new[] { new ComplianceSchemaMetadata(Guid.NewGuid(), string.Empty) }
                }
            }
        };
        _schema.Properties["name"] = property;

        _initialState = new ExpandoObject();
        ((IDictionary<string, object?>)_initialState)[WellKnownProperties.Subject] = EventSourceIdValue;

        _returnedState = new ExpandoObject();
        _sink.FindOrDefault(Arg.Any<Concepts.Keys.Key>()).Returns(Task.FromResult<ExpandoObject?>(_initialState));
    }

    async Task Because() => await _pipeline.Handle(
        CreateContext(EventSourceIdValue),
        CreateReducer(_returnedState));

    [Fact] void should_call_release_for_initial_state() => _complianceManager.Received(1).Release(EventStore, EventStoreNamespace, Arg.Any<JsonSchema>(), EventSourceIdValue, Arg.Any<JsonObject>());
    [Fact] void should_call_apply_on_result() => _complianceManager.Received(1).Apply(EventStore, EventStoreNamespace, Arg.Any<JsonSchema>(), EventSourceIdValue, Arg.Any<JsonObject>());
}
