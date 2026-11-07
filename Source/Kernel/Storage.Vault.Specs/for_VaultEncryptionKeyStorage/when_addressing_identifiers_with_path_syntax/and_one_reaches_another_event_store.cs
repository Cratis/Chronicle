// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_identifiers_with_path_syntax.and_one_reaches_another_event_store.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_identifiers_with_path_syntax;

[Collection(VaultCollection.Name)]
public class and_one_reaches_another_event_store(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_vault_encryption_key_storage(fixture)
    {
        public EncryptionKey VictimKey = new([1, 2, 3], [4, 5, 6]);
        public EncryptionKey? ReadThroughHostile;
        public EncryptionKey? VictimAfterDelete;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            EventStoreNameForVictim = $"other-{Guid.NewGuid():N}";
            await _storage.SaveFor(EventStoreNameForVictim, "default", "victim", VictimKey);

            var hostile = $"../../{EventStoreNameForVictim}/default/victim";
            ReadThroughHostile = await _storage.TryGetFor(_eventStore, _namespace, hostile);
            await _storage.DeleteFor(_eventStore, _namespace, hostile);
            VictimAfterDelete = await _storage.TryGetFor(EventStoreNameForVictim, "default", "victim");
        }

        public string EventStoreNameForVictim { get; private set; } = string.Empty;
    }

    [Fact] void should_not_read_the_other_event_stores_key() => ctx.ReadThroughHostile.ShouldBeNull();
    [Fact] void should_not_erase_the_other_event_stores_key() => ctx.VictimAfterDelete!.Fingerprint.ShouldEqual(ctx.VictimKey.Fingerprint);
}
