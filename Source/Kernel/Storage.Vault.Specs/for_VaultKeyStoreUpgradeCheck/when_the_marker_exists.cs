// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck.when_the_marker_exists.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck;

[Collection(VaultCollection.Name)]
public class when_the_marker_exists(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.an_empty_mount(fixture)
    {
        public EncryptionKeyStoreUpgradeStatus? Status;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            await _raw.Write("~layout", new Dictionary<string, object> { ["format"] = "k1" });
            await _raw.WriteKey("k1/x/y/z/1", new EncryptionKey([1, 2, 3], [4, 5, 6]));
            Status = await _check.EnsureLayout();
        }
    }

    [Fact] void should_verify_the_layout() => ctx.Status!.LayoutVerified.ShouldBeTrue();
    [Fact] void should_not_report_a_conflict() => ctx.Status!.LayoutConflict.ShouldBeFalse();
}
