// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// Represents an implementation of <see cref="IManagedEncryptionKeyProvisioner"/>.
/// </summary>
/// <remarks>
/// Extracted verbatim from the provisioning logic <see cref="Compliance.GDPR.PIICompliancePropertyValueHandler"/>
/// carried before this type existed - moving it changes nothing about its behavior, only how many features share
/// it. The in-process gate is keyed by <c language="csharp">(eventStore, eventStoreNamespace, identifier)</c>, so
/// an identifier built for one value-protection feature never contends with, or is confused for, an identifier
/// built for another - the gate only ever serializes callers that are asking about the exact same stored key.
/// </remarks>
/// <param name="encryptionKeyStore"><see cref="IEncryptionKeyStorage"/> to provision keys in.</param>
/// <param name="encryption"><see cref="IEncryption"/> used to generate a key when none exists yet.</param>
[Singleton]
public class ManagedEncryptionKeyProvisioner(IEncryptionKeyStorage encryptionKeyStore, IEncryption encryption) : IManagedEncryptionKeyProvisioner
{
    static readonly ConcurrentDictionary<string, SemaphoreSlim> _keyCreationGates = new();

    /// <inheritdoc/>
    public async Task<EncryptionKey> EnsureKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        if (await encryptionKeyStore.TryGetFor(eventStore, eventStoreNamespace, identifier) is { } existing)
        {
            return existing;
        }

        // A key must be provisioned exactly once: a batch append and the sibling projections that observe it all
        // encrypt concurrently (Task.WhenAll), and the same identifier may be provisioned from more than one silo.
        // If two provisioners each generate and save a key, the store mints a second revision and the value
        // encrypted under the first key can no longer be decrypted ("padding check failed"). The in-process gate
        // serializes provisioning within this process to avoid generating throwaway keys; GetOrAddFor is the atomic
        // get-or-create that makes every provisioner converge on a single persisted key pair even across processes
        // / stale reads.
        var gate = _keyCreationGates.GetOrAdd($"{eventStore.Value}+{eventStoreNamespace.Value}+{identifier.Value}", _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            return await encryptionKeyStore.GetOrAddFor(eventStore, eventStoreNamespace, identifier, encryption.GenerateKey());
        }
        finally
        {
            gate.Release();
        }
    }
}
