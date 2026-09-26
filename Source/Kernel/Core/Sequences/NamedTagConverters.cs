// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Converts named-tag transport values to validated domain values.
/// </summary>
internal static class NamedTagConverters
{
    /// <summary>
    /// Converts named tags from an append command.
    /// </summary>
    /// <param name="tags">The named tags.</param>
    /// <returns>Validated named tags.</returns>
    public static IReadOnlyCollection<Concepts.Events.NamedTag> ToChronicleNamedTags(this IEnumerable<NamedTag>? tags) =>
        (tags ?? []).Select(tag => tag is null
            ? throw new Concepts.Events.InvalidNamedTag()
            : new Concepts.Events.NamedTag(new Concepts.Events.TagName(tag.Name), tag.Value)).ToArray();

    /// <summary>
    /// Converts a named tag from its generated wire contract.
    /// </summary>
    /// <param name="tag">The wire tag.</param>
    /// <returns>The append command tag.</returns>
    public static NamedTag ToApi(this Contracts.Sequences.NamedTag tag) => new(tag.Name, tag.Value);

    /// <summary>
    /// Converts a named tag to its generated wire contract.
    /// </summary>
    /// <param name="tag">The named tag.</param>
    /// <returns>The wire tag.</returns>
    public static Contracts.Sequences.NamedTag ToContract(this NamedTag tag) => new()
    {
        Name = tag.Name,
        Value = tag.Value
    };
}
