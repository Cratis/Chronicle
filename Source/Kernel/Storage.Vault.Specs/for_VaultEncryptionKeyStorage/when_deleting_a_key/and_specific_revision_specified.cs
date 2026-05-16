// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_deleting_a_key.and_specific_revision_specified.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_deleting_a_key;

[Collection(VaultCollection.Name)]
public class and_specific_revision_specified(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_vault_encryption_key_storage(fixture)
    {
        public EncryptionKeyIdentifier Identifier;
        public EncryptionKeyRevision RevisionOne;
        public EncryptionKeyRevision RevisionTwo;
        public bool HasRevisionOneAfterDelete;
        public bool HasRevisionTwoAfterDelete;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            Identifier = Guid.NewGuid().ToString();
            RevisionOne = 1u;
            RevisionTwo = 2u;
            await _storage.SaveFor(_eventStore, _namespace, Identifier, new EncryptionKey([1, 2, 3], [4, 5, 6]), RevisionOne);
            await _storage.SaveFor(_eventStore, _namespace, Identifier, new EncryptionKey([7, 8, 9], [10, 11, 12]), RevisionTwo);
            await _storage.DeleteFor(_eventStore, _namespace, Identifier, RevisionOne);
            HasRevisionOneAfterDelete = await _storage.HasFor(_eventStore, _namespace, Identifier, RevisionOne);
            HasRevisionTwoAfterDelete = await _storage.HasFor(_eventStore, _namespace, Identifier, RevisionTwo);
        }
    }

    [Fact] void should_not_have_the_deleted_revision() => ctx.HasRevisionOneAfterDelete.ShouldBeFalse();
    [Fact] void should_still_have_the_other_revision() => ctx.HasRevisionTwoAfterDelete.ShouldBeTrue();
}
