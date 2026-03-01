// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patching;

namespace Cratis.Chronicle.Storage.Patching;

/// <summary>
/// Defines a storage interface for patches.
/// </summary>
public interface IPatchStorage
{
    /// <summary>
    /// Get all patches that have been applied.
    /// </summary>
    /// <returns>Collection of <see cref="Patch"/>.</returns>
    Task<IEnumerable<Patch>> GetAll();

    /// <summary>
    /// Save a patch as applied.
    /// </summary>
    /// <param name="patch">The <see cref="Patch"/> to save.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(Patch patch);

    /// <summary>
    /// Check if a patch has been applied.
    /// </summary>
    /// <param name="patchName">The name of the patch.</param>
    /// <returns>True if the patch has been applied, false otherwise.</returns>
    Task<bool> Has(string patchName);

    /// <summary>
    /// Remove a patch record (for downgrade scenarios).
    /// </summary>
    /// <param name="patchName">The name of the patch to remove.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(string patchName);
}
