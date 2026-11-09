// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>Immutable confirmed request archive. Acknowledgment always precedes removal from the fence.</summary>
public static class EncryptionKeyRequestArchive
{
    /// <summary>Read a confirmed archived request.</summary>
    /// <param name="records">The durable metadata authority.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="eventStoreNamespace">The namespace.</param>
    /// <param name="identifier">The key identifier.</param>
    /// <param name="requestId">The retry identity.</param>
    /// <returns>The original immutable request.</returns>
    /// <exception cref="EncryptionKeyFenceUnavailable">The archive is invalid.</exception>
    public static async Task<EncryptionKeyErasureRequest?> Read(IEncryptionKeyFenceRecords records, EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        var value = await records.ReadRecord(eventStore, eventStoreNamespace, Id(identifier, requestId));
        if (value is null) return null;
        try
        {
            var request = JsonSerializer.Deserialize<EncryptionKeyErasureRequest>(value);
            if (request?.Confirmed != true || request.RequestId != requestId) throw new EncryptionKeyFenceUnavailable(identifier);
            return request;
        }
        catch (JsonException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }

    /// <summary>Archive confirmed requests and verify any existing insert-only records agree.</summary>
    /// <param name="records">The durable metadata authority.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="eventStoreNamespace">The namespace.</param>
    /// <param name="identifier">The key identifier.</param>
    /// <param name="fence">The observed fence.</param>
    /// <param name="retain">The maximum number of confirmed inline requests to retain.</param>
    /// <returns>The CAS replacement; the caller must condition removal on the observed Version.</returns>
    /// <exception cref="EncryptionKeyFenceUnavailable">An archived record disagrees with the fence.</exception>
    public static async Task<EncryptionKeyErasure> Prepare(IEncryptionKeyFenceRecords records, EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyErasure fence, int retain)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(retain);
        var removable = (fence.Requests ?? []).Where(_ => _.Confirmed).Reverse().Skip(retain).ToArray();
        foreach (var request in removable)
        {
            var serialized = JsonSerializer.Serialize(request);
            var id = Id(identifier, request.RequestId);
            await records.WriteRecord(eventStore, eventStoreNamespace, id, serialized);
            if (!serialized.Equals(await records.ReadRecord(eventStore, eventStoreNamespace, id), StringComparison.Ordinal)) throw new EncryptionKeyFenceUnavailable(identifier);
        }

        var removed = removable.Select(_ => _.RequestId).ToHashSet(StringComparer.Ordinal);
        return fence with { Version = checked(fence.Version + 1), Requests = (fence.Requests ?? []).Where(_ => !removed.Contains(_.RequestId)).ToArray() };
    }

    /// <summary>Validate an external-fence transition, allowing removal only after confirmed archival.</summary>
    /// <param name="records">The archive authority.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="eventStoreNamespace">The namespace.</param>
    /// <param name="identifier">The authority scope.</param>
    /// <param name="observed">The observed state.</param>
    /// <param name="next">The replacement.</param>
    /// <returns>Awaitable validation.</returns>
    /// <exception cref="EncryptionKeyFenceChanged">A removal has not been acknowledged by the archive.</exception>
    public static async Task ValidateTransition(IEncryptionKeyFenceRecords records, EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyFenceState observed, EncryptionKeyFenceState next)
    {
        var retained = (next.Erasure.Requests ?? []).Select(_ => _.RequestId).ToHashSet(StringComparer.Ordinal);
        var removed = (observed.Erasure.Requests ?? []).Where(_ => !retained.Contains(_.RequestId)).ToArray();
        foreach (var request in removed)
        {
            var archived = await Read(records, eventStore, eventStoreNamespace, identifier, request.RequestId);
            if (!request.Confirmed || JsonSerializer.Serialize(request) != JsonSerializer.Serialize(archived)) throw new EncryptionKeyFenceChanged(identifier);
        }

        observed.EnsureMonotonic(identifier, next with { Erasure = next.Erasure with { Requests = [.. next.Erasure.Requests ?? [], .. removed] } });
    }

    static string Id(EncryptionKeyIdentifier identifier, string requestId) =>
        "request/" + Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[] { identifier.Value, requestId })));
}
