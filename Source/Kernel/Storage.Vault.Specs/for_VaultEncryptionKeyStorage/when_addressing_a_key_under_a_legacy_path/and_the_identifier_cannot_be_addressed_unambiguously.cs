// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path.and_the_identifier_cannot_be_addressed_unambiguously.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path;

[Collection(VaultCollection.Name)]
public class and_the_identifier_cannot_be_addressed_unambiguously(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_vault_encryption_key_storage(fixture)
    {
        public EncryptionKey ExistingKey = new([1, 2, 3], [4, 5, 6]);
        public EncryptionKey FreshKey = new([7, 8, 9], [10, 11, 12]);
        public Exception? LatestException;
        public Exception? ProvisionException;
        public Exception? ErasureReadException;
        public bool NewLocationWritten;
        public bool ErasureRecorded;
        public bool EarlierKeyUntouched;
        public EncryptionKeyAtRevision? LatestAfterErasure;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var raw = new given.raw_vault(Fixture.VaultAddress);
            await raw.WriteKey($"{_eventStore}/default/a/b/1", ExistingKey);

            LatestException = await Catch(() => _storage.TryGetLatestFor(_eventStore, _namespace, "a/b"));
            ProvisionException = await Catch(() => _storage.GetOrAddFor(_eventStore, _namespace, "a/b", FreshKey));
            ErasureReadException = await Catch(() => _storage.GetErasureFor(_eventStore, _namespace, "a/b"));
            NewLocationWritten = await raw.Exists($"k1/{_eventStore}/default/a~2Fb/1");

            await _storage.RecordErasureFor(_eventStore, _namespace, "a/b");
            ErasureRecorded = await raw.Exists($"k1/{_eventStore}/default/a~2Fb/erasure");
            EarlierKeyUntouched = await raw.Exists($"{_eventStore}/default/a/b/1");
            LatestAfterErasure = await _storage.TryGetLatestFor(_eventStore, _namespace, "a/b");
        }

        static async Task<Exception?> Catch(Func<Task> action)
        {
            try
            {
                await action();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }

    [Fact] void should_refuse_reading() => ctx.LatestException.ShouldBeOfExactType<EncryptionKeyNeedsUpgrade>();
    [Fact] void should_refuse_provisioning() => ctx.ProvisionException.ShouldBeOfExactType<EncryptionKeyNeedsUpgrade>();
    [Fact] void should_refuse_reading_the_erasure() => ctx.ErasureReadException.ShouldBeOfExactType<EncryptionKeyNeedsUpgrade>();
    [Fact] void should_not_write_a_new_key() => ctx.NewLocationWritten.ShouldBeFalse();
    [Fact] void should_not_put_the_identifier_in_the_message() => ctx.LatestException!.Message.ShouldNotContain("a/b");
    [Fact] void should_record_the_erasure_at_the_new_location() => ctx.ErasureRecorded.ShouldBeTrue();
    [Fact] void should_leave_the_earlier_key_untouched() => ctx.EarlierKeyUntouched.ShouldBeTrue();
    [Fact] void should_read_as_empty_after_the_erasure() => ctx.LatestAfterErasure.ShouldBeNull();
}
