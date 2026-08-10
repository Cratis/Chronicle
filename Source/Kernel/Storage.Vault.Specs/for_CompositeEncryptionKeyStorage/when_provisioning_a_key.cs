// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_CompositeEncryptionKeyStorage.when_provisioning_a_key.context;

namespace Cratis.Chronicle.Storage.Vault.for_CompositeEncryptionKeyStorage;

[Collection(VaultCollection.Name)]
public class when_provisioning_a_key(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_composite_over_vault_and_the_default_storage(fixture)
    {
        public EncryptionKey First = default!;
        public EncryptionKey Second = default!;
        public EncryptionKey FirstResult = default!;
        public EncryptionKey SecondResult = default!;
        public EncryptionKey? InVault;
        public EncryptionKey? MirroredToTheDefaultStorage;
        public bool VaultHasASecondRevision;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            First = new EncryptionKey([1, 2, 3], [4, 5, 6]);
            Second = new EncryptionKey([7, 8, 9], [10, 11, 12]);

            FirstResult = await _composite.GetOrAddFor(_eventStore, _namespace, _identifier, First);
            SecondResult = await _composite.GetOrAddFor(_eventStore, _namespace, _identifier, Second);

            InVault = await _vault.TryGetFor(_eventStore, _namespace, _identifier);
            MirroredToTheDefaultStorage = await _defaultStorage.TryGetFor(_eventStore, _namespace, _identifier);
            VaultHasASecondRevision = await _vault.HasFor(_eventStore, _namespace, _identifier, new EncryptionKeyRevision(2u));
        }
    }

    [Fact] void should_return_the_provisioned_key() => ctx.FirstResult.Private.ShouldEqual(ctx.First.Private);
    [Fact] void should_converge_a_repeated_provisioning_on_the_same_key() => ctx.SecondResult.Private.ShouldEqual(ctx.First.Private);
    [Fact] void should_provision_it_in_vault() => ctx.InVault!.Private.ShouldEqual(ctx.First.Private);
    [Fact] void should_mirror_it_to_the_default_storage() => ctx.MirroredToTheDefaultStorage!.Private.ShouldEqual(ctx.First.Private);
    [Fact] void should_not_mint_a_second_revision_in_vault() => ctx.VaultHasASecondRevision.ShouldBeFalse();
}
