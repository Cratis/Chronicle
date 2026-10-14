// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Defines owned global seed registration without changing the original seeding grain contracts.
/// </summary>
public interface IGlobalEventSeeding : IResultAwareEventSeeding
{
    /// <summary>
    /// Registers global entries, optionally replacing the owner's previous declaration and legacy entries.
    /// </summary>
    /// <param name="entries">The global entries to register.</param>
    /// <param name="authoritative">Whether the entries are the owner's complete declaration.</param>
    /// <param name="owner">The program identifier supplying the entries.</param>
    /// <returns>Whether every namespace accepted the entries still to seed.</returns>
    Task<SeedingResult> SeedGlobalWithResult(IEnumerable<SeedingEntry> entries, bool authoritative, string owner);
}
