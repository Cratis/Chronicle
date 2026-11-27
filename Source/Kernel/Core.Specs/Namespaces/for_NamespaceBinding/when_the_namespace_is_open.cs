// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceBinding;

public class when_the_namespace_is_open : given.a_binding_lookup
{
    Task Because() => Lookup();

    [Fact] void should_return_the_stored_casing() => _result.Namespace.ShouldEqual(_namespace);
    [Fact] void should_issue_the_installed_identity() => _result.Incarnation.ShouldEqual(_incarnation);
    [Fact] void should_report_open() => _result.Status.ShouldEqual(NamespaceLifecycleStatus.Open);
    [Fact] void should_report_the_bound_protocol_version() => _result.ProtocolVersion.ShouldEqual(1);
    [Fact] async Task should_read_the_canonical_lifecycle() => await _lifecycle.Received(1).Get(_namespace);
    [Fact] void should_not_activate_or_recreate_a_namespace() => _grains.ReceivedCalls().ShouldBeEmpty();
}
