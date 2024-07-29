// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactions;

/// <summary>
/// Exception that gets thrown when an reaction identifier is unknown.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="UnknownReactionId"/>.
/// </remarks>
/// <param name="reactionId">The identifier of the unknown reducer.</param>
public class UnknownReactionId(ReactionId reactionId) : Exception($"Reaction with identifier '{reactionId}' is not a known reaction")
{
}
