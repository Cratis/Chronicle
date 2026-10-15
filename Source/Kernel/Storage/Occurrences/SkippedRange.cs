// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// A durable explanation of deliberately skipped work.
/// </summary>
/// <param name="Id">The stable skip identity.</param>
/// <param name="Token">The schedule token.</param>
/// <param name="Revision">The definition revision.</param>
/// <param name="From">The exclusive lower epoch second.</param>
/// <param name="Through">The inclusive upper epoch second.</param>
/// <param name="Count">The number skipped.</param>
/// <param name="Policy">The policy or migration directive.</param>
/// <param name="Principal">The operator, if applicable.</param>
public record SkippedRange(
    string Id,
    ScheduleToken Token,
    long Revision,
    long From,
    long Through,
    long Count,
    string Policy,
    string? Principal);
