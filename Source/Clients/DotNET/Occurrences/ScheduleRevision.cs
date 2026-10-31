// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Identifies the stored revision of a schedule definition.
/// </summary>
/// <param name="Value">The revision.</param>
public record ScheduleRevision(long Value) : ConceptAs<long>(Value)
{
    /// <summary>
    /// Converts a stored revision to its typed representation.
    /// </summary>
    /// <param name="value">The revision.</param>
    public static implicit operator ScheduleRevision(long value) => new(value);
}
