// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrationOnEmulator;

[Collection(AzureKeyVaultCollection.Name)]
public class and_another_migration_runs_concurrently(and_another_migration_runs_concurrently.context ctx) : IClassFixture<and_another_migration_runs_concurrently.context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_migration_on_the_emulator(fixture)
    {
        public bool FirstOwns;
        public bool SecondOwns;
        public bool SecondOwnsAfterRelease;
        public EncryptionKeyMigrationReport Report = default!;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var first = new MigrationOwnership(_vault);
            var second = new MigrationOwnership(_vault);
            const string firstOwner = "first";
            const string secondOwner = "second";
            FirstOwns = await first.TryAcquire(firstOwner, CancellationToken.None);
            try
            {
                SecondOwns = await second.TryAcquire(secondOwner, CancellationToken.None);
                Report = await Migrate();
            }
            finally
            {
                await first.Release(firstOwner, CancellationToken.None);
            }

            SecondOwnsAfterRelease = await second.TryAcquire(secondOwner, CancellationToken.None);
            await second.Release(secondOwner, CancellationToken.None);
        }
    }

    [Fact] void should_let_the_first_own_the_lock() => ctx.FirstOwns.ShouldBeTrue();
    [Fact] void should_not_let_the_second_own_the_lock() => ctx.SecondOwns.ShouldBeFalse();
    [Fact] void should_report_the_lock_to_a_migrator() => ctx.Report.MigrationLocked.ShouldBeTrue();
    [Fact] void should_let_another_own_it_once_released() => ctx.SecondOwnsAfterRelease.ShouldBeTrue();
}
