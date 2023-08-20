using System.Collections.Immutable;
// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Auditing;

/// <summary>
/// Defines a system that manages <see cref="CausedBy">caused by</see> instances.
/// </summary>
public interface ICausedByStore
{
    /// <summary>
    /// Populate the caused by store.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Populate();

    /// <summary>
    /// Check if a <see cref="CausedBy"/> exists for a given <see cref="CausedById"/>.
    /// </summary>
    /// <param name="causedById"><see cref="CausedById"/> to check.</param>
    /// <returns>True if it does, false if not.</returns>
    Task<bool> HasFor(CausedById causedById);

    /// <summary>
    /// Get a <see cref="CausedBy"/> for a given <see cref="CausedById"/>.
    /// </summary>
    /// <param name="causedById"><see cref="CausedById"/> to get for.</param>
    /// <returns><see cref="CausedBy"/> instance.</returns>
    Task<CausedBy> GetSingleFor(CausedById causedById);

    /// <summary>
    /// Get a <see cref="CausedBy"/> for a given <see cref="CausedById"/>.
    /// </summary>
    /// <param name="chain">Collection of <see cref="CausedById"/> representing the chain to get for.</param>
    /// <returns><see cref="CausedBy"/> instance.</returns>
    Task<CausedBy> GetFor(IEnumerable<CausedById> chain);

    /// <summary>
    /// Get the top level identity for a given <see cref="CausedBy"/> as <see cref="CausedById"/> .
    /// </summary>
    /// <param name="causedBy"><see cref="CausedBy"/> to get for.</param>
    /// <returns>The <see cref="CausedById"/> representation for the <see cref="CausedBy"/>.</returns>
    /// <remarks>
    /// If the <see cref="CausedBy"/> does not exist, it will be created.
    /// </remarks>
    Task<CausedById> GetSingleFor(CausedBy causedBy);

    /// <summary>
    /// Get the chain of <see cref="CausedById"/> for a given <see cref="CausedBy"/>.
    /// </summary>
    /// <param name="causedBy"><see cref="CausedBy"/> to get for.</param>
    /// <returns>Collection representing a chain of <see cref="CausedById"/>.</returns>
    Task<IImmutableList<CausedById>> GetChainFor(CausedBy causedBy);
}
