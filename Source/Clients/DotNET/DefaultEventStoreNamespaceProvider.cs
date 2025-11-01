// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Represents a default implementation of <see cref="IEventStoreNamespaceProvider"/> that always returns the default namespace.
/// </summary>
public class DefaultEventStoreNamespaceProvider : IEventStoreNamespaceProvider
{
    /// <inheritdoc/>
    public EventStoreNamespaceName GetNamespace() => EventStoreNamespaceName.Default;
}
