// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries;

/// <summary>
/// Parses what a saved query orders its results by from what the caller sent.
/// </summary>
/// <remarks>
/// The field arrives as a string rather than as the enum itself, because that is what survives the
/// trip through the generated proxy - and because an unrecognized value should fall back to the
/// natural order of the sequence rather than fail the command.
/// </remarks>
public static class SequenceQuerySortByParser
{
    /// <summary>
    /// Parse the field a query is ordered by.
    /// </summary>
    /// <param name="sortBy">The field name, case insensitive.</param>
    /// <returns>The <see cref="SequenceQuerySortBy"/>, defaulting to the sequence number.</returns>
    public static SequenceQuerySortBy Parse(string? sortBy) =>
        Enum.TryParse<SequenceQuerySortBy>(sortBy, ignoreCase: true, out var parsed)
            ? parsed
            : SequenceQuerySortBy.SequenceNumber;
}
