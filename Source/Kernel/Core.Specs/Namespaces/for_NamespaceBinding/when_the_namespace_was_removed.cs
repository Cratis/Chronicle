// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceBinding;

public class when_the_namespace_was_removed : given.a_binding_lookup
{
    void Establish()
    {
        _state = _state with { Status = NamespaceLifecycleStatus.Removed };
        _names.GetAll().Returns(Task.FromResult<IEnumerable<NamespaceState>>([]));
    }

    Task Because() => Lookup();

    [Fact] void should_not_issue_the_deleted_identity() => _result.Incarnation.ShouldBeNull();
    [Fact] void should_return_the_stored_casing_from_the_tombstone() => _result.Namespace.ShouldEqual(_namespace);
    [Fact] void should_report_removed() => _result.Status.ShouldEqual(NamespaceLifecycleStatus.Removed);
    [Fact] void should_not_create_a_replacement() => _grains.ReceivedCalls().ShouldBeEmpty();
}
