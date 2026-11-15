// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck.when_an_earlier_event_store_starts_with_a_tilde.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck;

[Collection(VaultCollection.Name)]
public class when_an_earlier_event_store_starts_with_a_tilde(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.an_empty_mount(fixture)
    {
        public EncryptionKeyStoreUpgradeStatus? Full;
        public EncryptionKeyStoreUpgradeStatus? Startup;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var key = new EncryptionKey([1, 2, 3], [4, 5, 6]);
            await _raw.WriteKey("~store/default/customer-42/1", key);
            await _raw.WriteKey("~layoutx/default/customer-42/1", key);

            Full = await _check.Inspect(true);
            Startup = await _check.Inspect(false);
        }
    }

    [Fact] void should_verify_the_layout() => ctx.Full!.LayoutVerified.ShouldBeTrue();
    [Fact] void should_report_earlier_material() => ctx.Full!.EarlierMaterialPresent.ShouldBeTrue();
    [Fact] void should_count_both_event_stores_when_inspecting_fully() => (ctx.Full!.EarlierNameKeys + ctx.Full.EarlierNameErasures + ctx.Full.UnresolvedShapes).ShouldEqual(2L);
    [Fact] void should_see_both_event_stores_at_startup_but_not_the_marker() => ctx.Startup!.EarlierNameKeys.ShouldEqual(2L);
}
