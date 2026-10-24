// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Bounds equality probes for order-sensitive MongoDB document ids.
/// </summary>
internal static class CompositeKeyPermutations
{
    /// <summary>
    /// Produces every element order, identity first, for at most five parts.
    /// </summary>
    /// <param name="id">The write-path BSON id.</param>
    /// <returns>The permutations, or none when the cap is exceeded.</returns>
    internal static IReadOnlyList<BsonDocument> For(BsonDocument id) => id.ElementCount > 5 ? [] : Permute(id.Elements.ToArray()).ToArray();

    static IEnumerable<BsonDocument> Permute(BsonElement[] elements)
    {
        if (elements.Length == 0)
        {
            yield return new BsonDocument();
            yield break;
        }

        for (var index = 0; index < elements.Length; index++)
        {
            var remaining = elements.Where((_, position) => position != index).ToArray();
            foreach (var tail in Permute(remaining))
            {
                yield return new BsonDocument { elements[index] }.AddRange(tail.Elements);
            }
        }
    }
}
