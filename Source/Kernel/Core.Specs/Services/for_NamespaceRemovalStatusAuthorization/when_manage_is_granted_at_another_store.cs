// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Services.for_NamespaceRemovalStatusAuthorization;

public class when_manage_is_granted_at_another_store : given.a_status_call
{
    void Establish() => _decisions.Decide(Arg.Any<ClaimsPrincipal?>(), WellKnownCapabilities.ManageEventStore, Arg.Is<AuthorizationScope>(scope => scope.EventStore.Value == "other"))
        .Returns(call => AuthorizationDecision.Allow(AuthorizationReason.Granted, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));

    async Task Because() => await Inspect();

    [Fact] void should_refuse_the_wrong_scope() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_decide_at_the_requested_store() => _asked.Single().Scope.EventStore.Value.ShouldEqual("shop");
    [Fact] void should_not_resolve_storage() => _incarnations.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_expose_an_operation_identity() => _result.Data.OperationId.ShouldEqual(Guid.Empty);
    [Fact] void should_not_expose_a_revision() => _result.Data.Revision.ShouldEqual(0L);
}
