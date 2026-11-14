// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_ensuring_an_existing_namespace_with_a_comma : given.a_namespaces_grain
{
    EventStoreNamespaceName _result;

    void Establish()
    {
        _namespace = "raalabs,raalabs";
        _state.Namespaces.Add(new NamespaceState(_namespace, DateTimeOffset.UtcNow));
    }

    async Task Because() => _result = await _namespaces.EnsureCanonical(_namespace);

    [Fact] void should_still_return_the_stored_name() => _result.Value.ShouldEqual("raalabs,raalabs");
}
