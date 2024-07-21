// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactions;

/// <summary>
/// Defines a system for working with reaction registrations for the Kernel.
/// </summary>
public interface IReactions
{
    /// <summary>
    /// Discover all reactions from the entry assembly and dependencies.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Discover();

    /// <summary>
    /// Register all reactions with Chronicle.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Register();

    /// <summary>
    /// Gets a specific handler by its <see cref="ReactionId"/>.
    /// </summary>
    /// <param name="id"><see cref="ReactionId"/> to get for.</param>
    /// <returns><see cref="ReactionHandler"/> instance.</returns>
    ReactionHandler GetHandlerById(ReactionId id);
}
