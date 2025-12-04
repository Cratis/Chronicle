// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Defines a system that can resolve the event store namespace to use.
/// </summary>
public interface IEventStoreNamespaceResolver
{
    /// <summary>
    /// Resolves the current event store namespace.
    /// </summary>
    /// <returns>The <see cref="EventStoreNamespaceName"/> to use.</returns>
    EventStoreNamespaceName Resolve();
}
