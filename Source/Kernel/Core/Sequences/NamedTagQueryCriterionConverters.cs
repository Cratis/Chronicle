// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Converts named tag query criteria from the generated contract.
/// </summary>
internal static class NamedTagQueryCriterionConverters
{
    /// <summary>
    /// Converts a wire criterion to a Core criterion.
    /// </summary>
    /// <param name="criterion">The wire criterion.</param>
    /// <returns>The Core criterion.</returns>
    /// <exception cref="InvalidNamedTagCriterion">The criterion has no explicit name-only mode or usable values.</exception>
    public static NamedTagQueryCriterion ToApi(this Contracts.Sequences.NamedTagQueryCriterion criterion)
    {
        var values = criterion.Values?.ToArray();

        // Protobuf represents an omitted repeated field as an empty collection.
        if (criterion.AnyValue)
        {
            if (values is { Length: > 0 })
            {
                throw new InvalidNamedTagCriterion();
            }

            return new(criterion.Name, AnyValue: true);
        }

        if (values is not { Length: > 0 })
        {
            throw new InvalidNamedTagCriterion();
        }

        return new(criterion.Name, values);
    }
}
