// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces;

/// <summary>An opt-in namespace lookup capability, separate from the existing event store interface.</summary>
public interface INamespaceBindings
{
    /// <summary>Looks up an authorized snapshot without ensuring, creating, or reopening a namespace.</summary>
    /// <param name="namespace">The namespace to look up.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The canonical name, status, protocol version, and open identity when available.</returns>
    /// <exception cref="NamespaceBindingsNotSupported">The kernel cannot safely supply a binding.</exception>
    Task<NamespaceBinding> LookupNamespaceBinding(EventStoreNamespaceName @namespace, CancellationToken cancellationToken = default);
}
