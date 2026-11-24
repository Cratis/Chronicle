// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Cratis.Chronicle.Services;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemovalOperators;

public class when_a_reader_requests_abort : given.an_operator_pipeline
{
    void Establish() => _grants.Add(("shop", WellKnownCapabilities.ReadEventStore));

    async Task Because() => _result = await CommandExecutor.Execute(_pipeline, new AbortNamespaceRemoval("shop", "tenant", _operation, Revision));

    [Fact] void should_refuse_before_the_handler() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_require_manage_access() => _asked.Single().Capability.ShouldEqual(WellKnownCapabilities.ManageEventStore);
    [Fact] void should_not_reach_the_owner() => _incarnations.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_read_namespace_casing() => _casing.ReceivedCalls().ShouldBeEmpty();
}
