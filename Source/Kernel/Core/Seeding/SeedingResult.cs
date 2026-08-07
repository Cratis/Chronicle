// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Represents the outcome of a seeding operation.
/// </summary>
/// <param name="AllEntriesSeeded">Whether every entry handed to the operation has been appended.</param>
/// <remarks>
/// The outcome is deliberately coarse. Re-seeding is idempotent - every seeding grain skips the entries it
/// has already appended - so a caller that learns only that something was left behind can safely re-offer
/// the whole set, and does not need to know which entries those were.
/// </remarks>
public record SeedingResult(bool AllEntriesSeeded)
{
    /// <summary>
    /// Every entry was appended.
    /// </summary>
    public static readonly SeedingResult Complete = new(true);

    /// <summary>
    /// At least one entry was not appended and is still waiting to be seeded.
    /// </summary>
    public static readonly SeedingResult Incomplete = new(false);
}
