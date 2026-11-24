// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Cratis.Chronicle.Services;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemovalOperators;

public class when_resume_is_granted_only_at_another_store : given.an_operator_pipeline
{
    void Establish() => _grants.Add(("other", WellKnownCapabilities.ManageEventStore));

    async Task Because() => _result = await CommandExecutor.Execute(_pipeline, new ResumeNamespaceRemoval("shop", "tenant", _operation, Revision));

    [Fact] void should_refuse_the_wrong_scope() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_decide_at_the_requested_store() => _asked.Single().Scope.EventStore.Value.ShouldEqual("shop");
    [Fact] void should_not_reach_the_owner() => _incarnations.ReceivedCalls().ShouldBeEmpty();
}
