// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_performing;

public class and_read_model_has_no_pii : given.all_dependencies
{
    ProjectionEventContext _context;
    ProjectionEventContext _result;

    void Establish() => _context = CreateContext(EventSourceIdValue);

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_not_call_compliance_manager_apply() => _complianceManager.DidNotReceive().Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Schemas.JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>());
    [Fact] void should_return_context() => _result.ShouldNotBeNull();
}
