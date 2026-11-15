// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_identifiers_with_path_syntax.and_one_resolves_to_another_subject.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_identifiers_with_path_syntax;

[Collection(VaultCollection.Name)]
public class and_one_resolves_to_another_subject(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_vault_encryption_key_storage(fixture)
    {
        public EncryptionKey VictimKey = new([1, 2, 3], [4, 5, 6]);
        public EncryptionKey DotsKey = new([7, 8, 9], [10, 11, 12]);
        public EncryptionKey FragmentKey = new([13, 14, 15], [16, 17, 18]);
        public EncryptionKey? DotsStored;
        public EncryptionKey? FragmentStored;
        public EncryptionKey? VictimAfterDeletes;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            await _storage.SaveFor(_eventStore, _namespace, "victim", VictimKey);
            DotsStored = await _storage.GetOrAddFor(_eventStore, _namespace, "x/../victim", DotsKey);
            FragmentStored = await _storage.GetOrAddFor(_eventStore, _namespace, "victim#", FragmentKey);
            await _storage.DeleteFor(_eventStore, _namespace, "x/../victim");
            await _storage.DeleteFor(_eventStore, _namespace, "victim#");
            VictimAfterDeletes = await _storage.TryGetFor(_eventStore, _namespace, "victim");
        }
    }

    [Fact] void should_give_the_dotted_identifier_its_own_key() => ctx.DotsStored!.Fingerprint.ShouldEqual(ctx.DotsKey.Fingerprint);
    [Fact] void should_give_the_fragment_identifier_its_own_key() => ctx.FragmentStored!.Fingerprint.ShouldEqual(ctx.FragmentKey.Fingerprint);
    [Fact] void should_keep_the_other_subjects_key_when_they_are_deleted() => ctx.VictimAfterDeletes!.Fingerprint.ShouldEqual(ctx.VictimKey.Fingerprint);
}
