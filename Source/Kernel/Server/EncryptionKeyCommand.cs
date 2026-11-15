// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

/// <summary>
/// The command line options for inspecting and migrating encryption keys stored under earlier names.
/// </summary>
/// <param name="Verify">Whether the whole key store is inspected and the process exits afterwards.</param>
/// <param name="RemainingArgs">The arguments left for the host.</param>
/// <param name="Migrate">Whether keys stored under earlier names are copied to the current names and the process exits afterwards.</param>
/// <param name="DryRun">Whether a migration only counts what it would do.</param>
/// <param name="RemoveMigratedLegacy">Whether a migration removes the keys under earlier names whose copies were verified.</param>
internal record EncryptionKeyCommand(bool Verify, string[] RemainingArgs, bool Migrate = false, bool DryRun = false, bool RemoveMigratedLegacy = false)
{
    /// <summary>
    /// The flag that inspects the whole key store and exits.
    /// </summary>
    internal const string VerifyFlag = "--verify-encryption-keys";

    /// <summary>
    /// The flag that migrates keys stored under earlier names and exits.
    /// </summary>
    internal const string MigrateFlag = "--migrate-encryption-keys";

    /// <summary>
    /// The flag that makes a migration only count what it would do.
    /// </summary>
    internal const string DryRunFlag = "--dry-run";

    /// <summary>
    /// The flag that makes a migration remove the keys under earlier names whose copies were verified.
    /// </summary>
    internal const string RemoveMigratedLegacyFlag = "--remove-migrated-legacy-keys";

    static readonly string[] _flags = [VerifyFlag, MigrateFlag, DryRunFlag, RemoveMigratedLegacyFlag];

    /// <summary>
    /// Gets whether the command runs and exits instead of starting the server.
    /// </summary>
    internal bool ExitsAfterwards => Verify || Migrate;

    /// <summary>
    /// Parses the encryption key options out of the arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The <see cref="EncryptionKeyCommand"/>.</returns>
    /// <exception cref="ArgumentException">The flags are combined incorrectly.</exception>
    internal static EncryptionKeyCommand Parse(string[] args)
    {
        var verify = args.Contains(VerifyFlag, StringComparer.Ordinal);
        var migrate = args.Contains(MigrateFlag, StringComparer.Ordinal);
        var dryRun = args.Contains(DryRunFlag, StringComparer.Ordinal);
        var remove = args.Contains(RemoveMigratedLegacyFlag, StringComparer.Ordinal);

        if (verify && migrate)
        {
            throw new ArgumentException($"{VerifyFlag} and {MigrateFlag} cannot be used together.");
        }

        var migrationOptions = dryRun || remove;
        if (migrationOptions && !migrate)
        {
            throw new ArgumentException($"{DryRunFlag} and {RemoveMigratedLegacyFlag} can only be used with {MigrateFlag}.");
        }

        if (dryRun && remove)
        {
            throw new ArgumentException($"{DryRunFlag} and {RemoveMigratedLegacyFlag} cannot be used together.");
        }

        return new(verify, args.Where(argument => !_flags.Contains(argument, StringComparer.Ordinal)).ToArray(), migrate, dryRun, remove);
    }
}
