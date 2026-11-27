// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceBinding.given;

public class a_binding_lookup : for_NamespaceIncarnations.given.incarnation_routing
{
    protected NamespaceBinding _result;
    protected NamespaceIncarnationId _incarnation;

    void Establish()
    {
        _incarnation = NamespaceIncarnationId.New();
        _state = NamespaceLifecycle.Open(_namespace, _incarnation, DateTimeOffset.UtcNow);
    }

    protected async Task Lookup() => _result = await NamespaceBinding.LookupNamespaceBinding(_eventStore, "tenant", _routing);
}
