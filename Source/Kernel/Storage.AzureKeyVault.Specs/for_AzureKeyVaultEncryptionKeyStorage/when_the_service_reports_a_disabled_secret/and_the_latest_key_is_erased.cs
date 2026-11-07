// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_the_service_reports_a_disabled_secret;

public class and_the_latest_key_is_erased : given.a_storage_over_a_disabling_client
{
    EncryptionKeyErasure? _erasure;
    Exception? _provisionError;

    async Task Because()
    {
        await Seed("SecretDisabled", true);
        _client.Disable(KeyName);
        await _storage.RecordErasureFor(_eventStore, _namespace, Identifier);
        await _storage.DeleteFor(_eventStore, _namespace, Identifier);
        _erasure = await _storage.GetErasureFor(_eventStore, _namespace, Identifier);
        _provisionError = await Catch.Exception(() => _storage.GetOrAddFor(_eventStore, _namespace, Identifier, Fresh));
    }

    [Fact] void should_cover_the_disabled_revision() => (_erasure!.ErasedThrough.Value >= 1u).ShouldBeTrue();
    [Fact] void should_purge_the_disabled_secret() => _client.Purged.ShouldContain(KeyName);
    [Fact] void should_leave_no_live_key() => _client.LiveNames.ShouldNotContain(KeyName);
    [Fact] void should_keep_the_erasure() => _client.LiveNames.ShouldContain(ErasureName);
    [Fact] void should_refuse_to_mint_the_erased_key_again() => _provisionError.ShouldBeOfExactType<EncryptionKeyErased>();
}
