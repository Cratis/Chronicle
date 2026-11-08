// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EncryptionKeyStorageContract.given;

[PerFactSpecificationLifecycle]
public class a_fenced_store : Specification
{
    protected static readonly EventStoreName EventStore = "contract";
    protected static readonly EventStoreNamespaceName Namespace = "contract";
    protected static readonly EncryptionKeyIdentifier Identifier = "subject";
    protected static readonly EncryptionKey Original = new([1], [2]);
    protected static readonly EncryptionKey Successor = new([3], [4]);
    protected VersionedSecretClient Client;
    protected FenceAuthority Authority;
    protected AzureKeyVaultEncryptionKeyStorage Storage;
    protected RecordingLogger Logger;

    protected Task<EncryptionKeyErasure?> Fence() => Storage.GetFenceFor(EventStore, Namespace, Identifier);
    protected Task CloseLegacy() => Client.SetSecretAsync(SecretLocations.For(EventStore, Namespace, Identifier).ErasureName, JsonSerializer.Serialize(new Dictionary<string, string>
    {
        ["erasedThrough"] = "0",
        ["erasedKeyFingerprints"] = "",
        ["newKeyAllowed"] = bool.FalseString
    }));

    protected async Task Authorize()
    {
        var fence = await Fence();
        await Storage.AllowNewKeyFor(EventStore, Namespace, Identifier, fence.Version, fence.Lifecycle);
    }

    protected bool Logged(string message) => Logger.Messages.Exists(_ => _.Contains(message, StringComparison.Ordinal));

    void Establish()
    {
        Client = new();
        Authority = new();
        Logger = new();
        Storage = new(Client, Logger, Authority, _ => Task.CompletedTask);
    }
}
