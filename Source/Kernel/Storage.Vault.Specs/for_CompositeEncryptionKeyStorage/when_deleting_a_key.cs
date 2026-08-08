// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_CompositeEncryptionKeyStorage.when_deleting_a_key.context;

namespace Cratis.Chronicle.Storage.Vault.for_CompositeEncryptionKeyStorage;

[Collection(VaultCollection.Name)]
public class when_deleting_a_key(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_composite_over_vault_and_the_default_storage(fixture)
    {
        public bool VaultHasIt;
        public bool DefaultStorageHasIt;
        public EncryptionKey? ThroughTheComposite;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await _composite.GetOrAddFor(_eventStore, _namespace, _identifier, new EncryptionKey([1, 2, 3], [4, 5, 6]));

            await _composite.DeleteFor(_eventStore, _namespace, _identifier);

            VaultHasIt = await _vault.HasFor(_eventStore, _namespace, _identifier);
            DefaultStorageHasIt = await _defaultStorage.HasFor(_eventStore, _namespace, _identifier);
            ThroughTheComposite = await _composite.TryGetFor(_eventStore, _namespace, _identifier);
        }
    }

    [Fact] void should_erase_it_from_vault() => ctx.VaultHasIt.ShouldBeFalse();
    [Fact] void should_erase_it_from_the_default_storage() => ctx.DefaultStorageHasIt.ShouldBeFalse();
    [Fact] void should_not_heal_it_back_on_a_later_read() => ctx.ThroughTheComposite.ShouldBeNull();
}
