// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Defines a read-only source of the resources in use by the existing event stores and namespaces of a backend.
/// </summary>
public interface IResourceInventory
{
    /// <summary>
    /// Read the resources in use. Implementations never create, change or remove anything.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="ResourceUse"/> items.</returns>
    Task<IEnumerable<ResourceUse>> Read(CancellationToken cancellationToken);
}
