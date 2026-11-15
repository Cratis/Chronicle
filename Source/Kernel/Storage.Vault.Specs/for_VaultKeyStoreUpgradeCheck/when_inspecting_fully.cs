// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck.when_inspecting_fully.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck;

[Collection(VaultCollection.Name)]
public class when_inspecting_fully(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.an_empty_mount(fixture)
    {
        public EncryptionKeyStoreUpgradeStatus? Full;
        public EncryptionKeyStoreUpgradeStatus? Startup;
        public EncryptionKeyStoreUpgradeStatus? Empty;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            Empty = await _check.Inspect(true);

            var key = new EncryptionKey([1, 2, 3], [4, 5, 6]);
            await _raw.WriteKey("store/default/customer-42/1", key);
            await _raw.WriteKey("store/default/customer-42/2", key);
            await _raw.Write("store/default/customer-42/erasure", new Dictionary<string, object> { ["erasedThrough"] = "1" });
            await _raw.WriteKey("store/default/a/b/1", key);
            await _raw.WriteKey("k1/store/default/new-one/1", key);

            Full = await _check.Inspect(true);
            Startup = await _check.Inspect(false);
        }
    }

    [Fact] void should_report_nothing_for_an_empty_mount() => ctx.Empty!.EarlierMaterialPresent.ShouldBeFalse();
    [Fact] void should_count_the_earlier_keys() => ctx.Full!.EarlierNameKeys.ShouldEqual(2L);
    [Fact] void should_count_the_earlier_erasure() => ctx.Full!.EarlierNameErasures.ShouldEqual(1L);
    [Fact] void should_count_the_shape_that_cannot_be_addressed_unambiguously() => ctx.Full!.UnresolvedShapes.ShouldEqual(1L);
    [Fact] void should_be_complete() => ctx.Full!.Complete.ShouldBeTrue();
    [Fact] void should_not_count_what_this_version_wrote() => (ctx.Full!.EarlierNameKeys + ctx.Full.EarlierNameErasures + ctx.Full.UnresolvedShapes).ShouldEqual(4L);
    [Fact] void should_see_earlier_material_at_startup_without_walking() => ctx.Startup!.EarlierMaterialPresent.ShouldBeTrue();
    [Fact] void should_not_claim_completeness_at_startup() => ctx.Startup!.Complete.ShouldBeFalse();
}
