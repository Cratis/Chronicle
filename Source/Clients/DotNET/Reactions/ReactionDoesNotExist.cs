// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactions;

/// <summary>
/// Exception that gets thrown when an reaction does not exist.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ReactionDoesNotExist"/>.
/// </remarks>
/// <param name="reactionId">The invalid <see cref="ReactionId"/>.</param>
public class ReactionDoesNotExist(ReactionId reactionId) : Exception($"Reaction with id '{reactionId}' does not exist")
{
    /// <summary>
    /// Throw if the reaction does not exist.
    /// </summary>
    /// <param name="reactionId">The <see cref="ReactionId"/> of the reaction.</param>
    /// <param name="reaction">The possible null <see cref="ReactionHandler"/> >value to check.</param>
    /// <exception cref="ReactionDoesNotExist">Thrown if the reaction handler value is null.</exception>
    public static void ThrowIfDoesNotExist(ReactionId reactionId, ReactionHandler? reaction)
    {
        if (reaction is null) throw new ReactionDoesNotExist(reactionId);
    }
}
