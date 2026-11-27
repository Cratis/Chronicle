// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceBinding;

public class when_the_namespace_is_closing : given.a_binding_lookup
{
    void Establish() => _state = _state.BeginRemoval(Guid.NewGuid(), DateTimeOffset.UtcNow);
    Task Because() => Lookup();

    [Fact] void should_not_issue_the_retired_identity() => _result.Incarnation.ShouldBeNull();
    [Fact] void should_report_closing() => _result.Status.ShouldEqual(NamespaceLifecycleStatus.Closing);
    [Fact] void should_not_recreate_the_namespace() => _grains.ReceivedCalls().ShouldBeEmpty();
}
