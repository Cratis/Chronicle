// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EncryptionKeyStorageContract;

public class when_a_save_stages_during_a_legacy_read : given.a_fenced_store
{
    EncryptionKey _read;
    Task _pendingSave = Task.CompletedTask;
    bool _replacementWasUnpublished;

    async Task Establish() => await Client.SetSecretAsync(
        SecretLocations.For(EventStore, Namespace, Identifier).Name(1u),
        LegacyAzureKeyVaultEncryptionKeyStorage.Serialize(Original));

    async Task Because()
    {
        var name = SecretLocations.For(EventStore, Namespace, Identifier).Name(1u);
        var staged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Client.AfterUnversionedRead = async inspected =>
        {
            if (!inspected.Name.Equals(name, StringComparison.Ordinal)) return;
            Client.AfterUnversionedRead = null;
            Client.AfterStage = () =>
            {
                staged.SetResult();
                return release.Task;
            };
            _pendingSave = Storage.SaveFor(EventStore, Namespace, Identifier, Successor, 1u);
            await staged.Task;
            _replacementWasUnpublished = !_pendingSave.IsCompleted;
        };
        try
        {
            _read = await Storage.GetFor(EventStore, Namespace, Identifier, 1u);
        }
        finally
        {
            release.SetResult();
            await _pendingSave;
        }
    }

    [Fact] void should_stage_the_replacement_before_the_legacy_read_continues() => _replacementWasUnpublished.ShouldBeTrue();
    [Fact] void should_keep_the_inspected_legacy_value_instead_of_rereading_latest() => _read.Private.ShouldEqual(Original.Private);
}
