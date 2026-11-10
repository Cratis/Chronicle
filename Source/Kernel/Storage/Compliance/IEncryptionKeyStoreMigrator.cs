// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Defines a migration of encryption keys stored under earlier names to the names this version uses.
/// </summary>
/// <remarks>
/// A migration copies; it never overwrites what is already stored under the current names, and it leaves the keys
/// under earlier names in place unless asked to remove them after the copies are verified. Running it again is safe.
/// </remarks>
public interface IEncryptionKeyStoreMigrator
{
    /// <summary>
    /// Migrates the keys stored under earlier names.
    /// </summary>
    /// <param name="dryRun">Whether to only count what would be copied, without writing or removing anything.</param>
    /// <param name="removeVerifiedLegacy">Whether to remove the keys under earlier names whose copies were verified, when nothing in the run failed or was kept because it conflicts. Entries that cannot be tied to one address never block this and are never removed.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="EncryptionKeyMigrationReport"/>.</returns>
    Task<EncryptionKeyMigrationReport> Migrate(bool dryRun, bool removeVerifiedLegacy, CancellationToken cancellationToken = default);
}
