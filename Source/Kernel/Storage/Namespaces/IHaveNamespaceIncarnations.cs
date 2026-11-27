// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>Optional event-store capability, advertised only when every supplied-token commit path is fenced.</summary>
public interface IHaveNamespaceIncarnations
{
    /// <summary>Gets durable lifecycle control storage outside namespace databases.</summary>
    INamespaceLifecycleStorage NamespaceLifecycles { get; }

    /// <summary>Gets the namespace-local marker used by the provider's transactional append fence.</summary>
    /// <param name="namespace">The canonical namespace.</param>
    /// <returns>The marker storage; getting it must not open or upsert a marker.</returns>
    INamespaceMarkerStorage GetNamespaceMarker(EventStoreNamespaceName @namespace);
}
