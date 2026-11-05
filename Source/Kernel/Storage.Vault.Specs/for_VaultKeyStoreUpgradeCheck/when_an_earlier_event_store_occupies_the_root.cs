// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck.when_an_earlier_event_store_occupies_the_root.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck;

[Collection(VaultCollection.Name)]
public class when_an_earlier_event_store_occupies_the_root(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.an_empty_mount(fixture)
    {
        public EncryptionKeyStoreUpgradeStatus? Status;
        public bool MarkerWritten;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            await _raw.WriteKey("k1/x/y/z/1", new EncryptionKey([1, 2, 3], [4, 5, 6]));
            Status = await _check.EnsureLayout();
            MarkerWritten = await _raw.Exists("~layout");
        }
    }

    [Fact] void should_report_a_conflict() => ctx.Status!.LayoutConflict.ShouldBeTrue();
    [Fact] void should_not_verify_the_layout() => ctx.Status!.LayoutVerified.ShouldBeFalse();
    [Fact] void should_not_write_the_marker() => ctx.MarkerWritten.ShouldBeFalse();
}
