// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespaceBindings;

public class when_the_namespace_is_closing : given.a_namespace_lookup
{
    void Establish() => _response.Status = Contracts.Storage.Namespaces.NamespaceLifecycleStatus.Closing;
    Task Because() => Lookup();

    [Fact] void should_keep_the_status() => _binding.Status.ShouldEqual(NamespaceBindingStatus.Closing);
    [Fact] void should_not_issue_an_identity() => _binding.Incarnation.ShouldBeNull();
    [Fact] void should_not_reopen_the_namespace() => _namespaces.ReceivedCalls().Count().ShouldEqual(1);
}
