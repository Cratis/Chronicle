// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_DecryptInitialState.when_performing;

public class and_initial_state_is_null : given.all_dependencies
{
    ProjectionEventContext _result;

    async Task Because()
    {
        var context = CreateContext(null);
        context.Changeset.InitialState = null!;
        _result = await _step.Perform(_projection, context);
    }

    [Fact] void should_not_call_compliance_manager() => _complianceManager.DidNotReceive().Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Schemas.JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>());
    [Fact] void should_return_context() => _result.ShouldNotBeNull();
}
