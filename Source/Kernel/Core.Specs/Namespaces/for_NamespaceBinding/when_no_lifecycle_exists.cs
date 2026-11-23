// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceBinding;

public class when_no_lifecycle_exists : given.a_binding_lookup
{
    Exception _error;

    void Establish()
    {
        _state = null!;
        _names.GetAll().Returns(Task.FromResult<IEnumerable<NamespaceState>>([]));
    }

    async Task Because() => _error = await Catch.Exception(Lookup);

    [Fact] void should_refuse_binding() => _error.ShouldBeOfExactType<NamespaceIncarnationUnavailable>();
    [Fact] void should_not_create_a_namespace() => _grains.ReceivedCalls().ShouldBeEmpty();
}
