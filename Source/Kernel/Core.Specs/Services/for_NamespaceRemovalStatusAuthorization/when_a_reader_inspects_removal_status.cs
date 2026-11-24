// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Services.for_NamespaceRemovalStatusAuthorization;

public class when_a_reader_inspects_removal_status : given.a_status_call
{
    void Establish() => _decisions.Decide(Arg.Any<ClaimsPrincipal?>(), WellKnownCapabilities.ReadEventStore, Arg.Any<AuthorizationScope>())
        .Returns(call => AuthorizationDecision.Allow(AuthorizationReason.Granted, call.ArgAt<Capability>(1), call.ArgAt<AuthorizationScope>(2)));

    async Task Because() => await Inspect();

    [Fact] void should_require_manage_access() => _asked.Single().Capability.ShouldEqual(WellKnownCapabilities.ManageEventStore);
    [Fact] void should_refuse_read_only_access() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_expose_an_operation_identity() => _result.Data.OperationId.ShouldEqual(Guid.Empty);
    [Fact] void should_not_expose_a_revision() => _result.Data.Revision.ShouldEqual(0L);
    [Fact] void should_not_resolve_storage() => _incarnations.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_read_the_lifecycle() => _lifecycle.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_resolve_casing() => _casing.ReceivedCalls().ShouldBeEmpty();
}
