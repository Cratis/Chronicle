// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceBinding;

public class when_the_namespace_requires_recovery : given.a_binding_lookup
{
    void Establish() => _state = _state with { Status = NamespaceLifecycleStatus.RecoveryRequired };
    Task Because() => Lookup();

    [Fact] void should_not_issue_an_identity() => _result.Incarnation.ShouldBeNull();
    [Fact] void should_report_recovery_required() => _result.Status.ShouldEqual(NamespaceLifecycleStatus.RecoveryRequired);
    [Fact] void should_not_try_automatic_recovery() => _grains.ReceivedCalls().ShouldBeEmpty();
}
