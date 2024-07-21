// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.EventSequences;
using Cratis.Reflection;

namespace Cratis.Chronicle.Reactions;

/// <summary>
/// Extension methods for working with reactions and type discovery.
/// </summary>
public static class TypesExtensions
{
    /// <summary>
    /// Get the reaction id for a reaction type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>The <see cref="ReactionId"/> for the type.</returns>
    public static ReactionId GetReactionId(this Type type)
    {
        var reactionAttribute = type.GetCustomAttribute<ReactionAttribute>();
        var id = reactionAttribute?.Id.Value ?? string.Empty;
        return id switch
        {
            "" => new ReactionId(type.FullName ?? $"{type.Namespace}.{type.Name}"),
            _ => new ReactionId(id)
        };
    }

    /// <summary>
    /// Get the event sequence id for a reaction type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>The <see cref="EventSequenceId"/> for the type.</returns>
    public static EventSequenceId GetEventSequenceId(this Type type)
    {
        var reactionAttribute = type.GetCustomAttribute<ReactionAttribute>();
        return reactionAttribute?.EventSequenceId.Value ?? EventSequenceId.Log;
    }

    /// <summary>
    /// Find all reactions.
    /// </summary>
    /// <param name="types">Collection of types.</param>
    /// <returns>Collection of types that are reactions.</returns>
    public static IEnumerable<Type> AllReactions(this IEnumerable<Type> types) => types.Where(_ => _.HasAttribute<ReactionAttribute>()).ToArray();
}
