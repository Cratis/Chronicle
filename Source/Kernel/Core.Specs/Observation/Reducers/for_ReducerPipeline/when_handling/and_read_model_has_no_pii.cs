// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_handling;

public class and_read_model_has_no_pii : given.all_dependencies
{
    ExpandoObject _returnedState;

    void Establish()
    {
        _returnedState = new ExpandoObject();
        _sink.FindOrDefault(Arg.Any<Concepts.Keys.Key>()).Returns(Task.FromResult<ExpandoObject?>(null));
    }

    async Task Because() => await _pipeline.Handle(
        CreateContext(EventSourceIdValue),
        CreateReducer(_returnedState));

    [Fact] void should_not_call_release() => _complianceManager.DidNotReceive().Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Schemas.JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>());
    [Fact] void should_not_call_apply() => _complianceManager.DidNotReceive().Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Schemas.JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>());
}
