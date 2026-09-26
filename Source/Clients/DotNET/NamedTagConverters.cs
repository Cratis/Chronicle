// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Converts structured named tags between client and sequence contracts.
/// </summary>
internal static class NamedTagConverters
{
    /// <summary>
    /// Converts a named tag to its sequence contract.
    /// </summary>
    /// <param name="tag">The named tag.</param>
    /// <returns>The sequence contract.</returns>
    internal static Contracts.Sequences.NamedTag ToSequencesContract(this NamedTag tag) => new()
    {
        Name = tag.Name.Value,
        Value = tag.Value
    };

    /// <summary>
    /// Converts a sequence contract to a named tag.
    /// </summary>
    /// <param name="tag">The sequence contract.</param>
    /// <returns>The named tag.</returns>
    internal static NamedTag ToClient(this Contracts.Sequences.NamedTag tag) => new(tag.Name, tag.Value);

    /// <summary>
    /// Combines per-event and call-level named tags, retaining the first occurrence of each ordinal name/value pair.
    /// </summary>
    /// <param name="eventTags">Tags for the event.</param>
    /// <param name="callTags">Tags for the append call.</param>
    /// <returns>A materialized list of distinct named tags.</returns>
    internal static IReadOnlyList<NamedTag> Merge(IEnumerable<NamedTag> eventTags, IEnumerable<NamedTag> callTags) =>
        eventTags.Concat(callTags).DistinctBy(_ => (_.Name.Value, _.Value)).ToArray();
}
