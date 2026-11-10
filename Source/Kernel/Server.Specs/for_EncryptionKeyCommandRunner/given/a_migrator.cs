// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner.given;

public class a_migrator : a_check
{
    protected IEncryptionKeyStoreMigrator _migrator = default!;

    void Establish() => _migrator = Substitute.For<IEncryptionKeyStoreMigrator>();

    protected static EncryptionKeyMigrationReport Report(
        bool dryRun = false,
        bool layoutUnusable = false,
        bool complete = true,
        long copied = 0,
        long conflicts = 0,
        long ambiguous = 0,
        long failed = 0) =>
        new(dryRun, layoutUnusable, complete, copied + conflicts + ambiguous, 0, copied, 0, conflicts, ambiguous, failed, copied, 0);

    protected void Returns(EncryptionKeyMigrationReport report) =>
        _migrator.Migrate(Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(report);

    protected Task<int?> Migrate(bool dryRun = false, bool remove = false, IEncryptionKeyStoreMigrator? migrator = null) =>
        EncryptionKeyCommandRunner.Run(_check, NullLogger.Instance, new EncryptionKeyCommand(false, [], true, dryRun, remove), _output, migrator ?? _migrator);

    protected Task<int?> Start(LegacyKeyHandling handling) =>
        EncryptionKeyCommandRunner.Run(_check, NullLogger.Instance, new EncryptionKeyCommand(false, []), _output, _migrator, handling);
}
