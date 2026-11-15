// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck.when_two_nodes_race.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck;

[Collection(VaultCollection.Name)]
public class when_two_nodes_race(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.an_empty_mount(fixture)
    {
        public EncryptionKeyStoreUpgradeStatus[] Statuses = [];

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var other = new VaultKeyStoreUpgradeCheck(_client, _mountPoint);
            Statuses = await Task.WhenAll(Enumerable.Range(0, 8).Select(index => (index % 2 == 0 ? _check : other).EnsureLayout()));
        }
    }

    [Fact] void should_verify_the_layout_for_every_node() => ctx.Statuses.All(status => status.LayoutVerified).ShouldBeTrue();
    [Fact] void should_report_no_conflict_for_any_node() => ctx.Statuses.Any(status => status.LayoutConflict).ShouldBeFalse();
}
