// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck.when_the_mount_is_empty.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck;

[Collection(VaultCollection.Name)]
public class when_the_mount_is_empty(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.an_empty_mount(fixture)
    {
        public EncryptionKeyStoreUpgradeStatus? Status;
        public bool MarkerWritten;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            Status = await _check.EnsureLayout();
            MarkerWritten = await _raw.Exists("~layout");
        }
    }

    [Fact] void should_verify_the_layout() => ctx.Status!.LayoutVerified.ShouldBeTrue();
    [Fact] void should_not_report_a_conflict() => ctx.Status!.LayoutConflict.ShouldBeFalse();
    [Fact] void should_write_the_marker() => ctx.MarkerWritten.ShouldBeTrue();
}
