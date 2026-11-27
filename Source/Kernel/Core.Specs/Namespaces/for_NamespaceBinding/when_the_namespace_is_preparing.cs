// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceBinding;

public class when_the_namespace_is_preparing : given.a_binding_lookup
{
    void Establish() => _state = _state with { Status = NamespaceLifecycleStatus.Preparing };
    Task Because() => Lookup();

    [Fact] void should_not_issue_an_uninstalled_identity() => _result.Incarnation.ShouldBeNull();
    [Fact] void should_report_preparing() => _result.Status.ShouldEqual(NamespaceLifecycleStatus.Preparing);
    [Fact] void should_not_complete_creation() => _grains.ReceivedCalls().ShouldBeEmpty();
}
