// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_DecryptInitialState.when_performing;

public class and_initial_state_has_no_pii : given.all_dependencies
{
    ProjectionEventContext _context;
    ProjectionEventContext _result;

    void Establish()
    {
        dynamic state = new ExpandoObject();
        state.value = "some-value";
        ((IDictionary<string, object?>)(ExpandoObject)state)[WellKnownProperties.Subject] = "some-subject";
        _context = CreateContext(state);
    }

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_not_call_compliance_manager() => _complianceManager.DidNotReceive().Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Schemas.JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>());
}
