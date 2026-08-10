// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_CompositeEncryptionKeyStorage.when_getting_a_key_only_the_default_storage_has.context;

namespace Cratis.Chronicle.Storage.Vault.for_CompositeEncryptionKeyStorage;

[Collection(VaultCollection.Name)]
public class when_getting_a_key_only_the_default_storage_has(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_composite_over_vault_and_the_default_storage(fixture)
    {
        public EncryptionKey Key = default!;
        public EncryptionKey? Result;
        public EncryptionKey? ServedByVaultAfterwards;
        public bool VaultHasTheInitialRevision;
        public bool VaultHasASecondRevision;
        public bool DefaultStorageHasASecondRevision;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            Key = new EncryptionKey([1, 2, 3], [4, 5, 6]);
            await _defaultStorage.SaveFor(_eventStore, _namespace, _identifier, Key);

            // Two reads: a store written to on every read would grow a revision each time.
            await _composite.TryGetFor(_eventStore, _namespace, _identifier);
            Result = await _composite.TryGetFor(_eventStore, _namespace, _identifier);

            ServedByVaultAfterwards = await _vault.TryGetFor(_eventStore, _namespace, _identifier);
            VaultHasTheInitialRevision = await _vault.HasFor(_eventStore, _namespace, _identifier, EncryptionKeyRevision.Initial);
            VaultHasASecondRevision = await _vault.HasFor(_eventStore, _namespace, _identifier, new EncryptionKeyRevision(2u));
            DefaultStorageHasASecondRevision = await _defaultStorage.HasFor(_eventStore, _namespace, _identifier, new EncryptionKeyRevision(2u));
        }
    }

    [Fact] void should_return_the_key() => ctx.Result!.Private.ShouldEqual(ctx.Key.Private);
    [Fact] void should_heal_it_into_vault() => ctx.ServedByVaultAfterwards!.Private.ShouldEqual(ctx.Key.Private);
    [Fact] void should_heal_it_as_the_initial_revision() => ctx.VaultHasTheInitialRevision.ShouldBeTrue();
    [Fact] void should_not_mint_a_revision_per_read() => ctx.VaultHasASecondRevision.ShouldBeFalse();
    [Fact] void should_not_write_backwards_into_the_default_storage_once_vault_serves_the_key() => ctx.DefaultStorageHasASecondRevision.ShouldBeFalse();
}
