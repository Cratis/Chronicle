// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Globalization;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB;

/// <summary>
/// MongoDB implementation of <see cref="IEncryptionKeyStorage"/> using a single-document fence and staged key publication.
/// </summary>
/// <param name="database">The scoped database.</param>
public partial class EncryptionKeyStorage(IDatabase database) : IEncryptionKeyStorage, IEncryptionKeyFence
{
    /// <summary>
    /// The encryption key collection name.
    /// </summary>
    internal const string CollectionName = WellKnownCollectionNames.EncryptionKeys;

    /// <summary>
    /// The internal secret collection name. It sits in the event store database and no subject key operation uses it.
    /// </summary>
    internal const string InternalSecretsCollectionName = WellKnownCollectionNames.InternalSecrets;

    const int MaximumAttempts = 8;
    const int MaximumRequests = 1024;
    static readonly TimeSpan _readTimeout = TimeSpan.FromSeconds(10);
    static readonly FilterDefinitionBuilder<EncryptionKeyForIdentifier> _keys = Builders<EncryptionKeyForIdentifier>.Filter;
    static readonly FilterDefinitionBuilder<EncryptionKeyErasureForIdentifier> _fences = Builders<EncryptionKeyErasureForIdentifier>.Filter;
    readonly ConcurrentDictionary<string, byte> _ensuredIndexes = new();

    /// <inheritdoc/>
    public EncryptionKeyFenceGuarantees FenceGuarantees => EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave;

    /// <summary>
    /// Gets the indexes for the collection.
    /// </summary>
    internal static IReadOnlyList<CreateIndexModel<EncryptionKeyForIdentifier>> Indexes { get; } =
    [
        new(
            Builders<EncryptionKeyForIdentifier>.IndexKeys.Ascending(_ => _.Id.Identifier).Descending(_ => _.Id.Revision),
            new CreateIndexOptions { Name = "id_identifier_id_revision", Background = true })
    ];

    /// <inheritdoc/>
    public async Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision? revision = null)
    {
        var collection = await GetCollection(eventStore, eventStoreNamespace);
        var observed = await EnsureFence(eventStore, eventStoreNamespace, identifier);
        var lifecycle = observed.Lifecycle;
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var next = IsLatest(revision) ? await GetNextRevisionFor(collection, identifier, observed) : revision!;
            observed.EnsureCanSave(identifier, next, key);
            observed.EnsureStillAllowed(identifier, new(key, next, observed.Lifecycle));
            try
            {
                await StageAndPublish(eventStore, eventStoreNamespace, collection, identifier, key, next, observed);
                return;
            }
            catch (MongoWriteException exception) when (IsLatest(revision) && IsDuplicate(exception))
            {
                // A duplicate insert is a definite non-write, unlike a timeout or lost acknowledgment.
                observed = await RequiredFence(eventStore, eventStoreNamespace, identifier);
                if (observed.Lifecycle != lifecycle)
                {
                    throw new EncryptionKeyFenceChanged(identifier);
                }

                await Backoff(attempt);
            }
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision?> TryGetLatestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var document = await TryReadPublishedFor(eventStore, eventStoreNamespace, identifier);
        return document is null ? null : AtRevision(document);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision?> TryGetAtRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision revision)
    {
        var document = await TryReadPublishedFor(eventStore, eventStoreNamespace, identifier, revision);
        return document is null ? null : AtRevision(document);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision> GetOrAddWithRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        var collection = await GetCollection(eventStore, eventStoreNamespace);
        var initial = await EnsureFence(eventStore, eventStoreNamespace, identifier);
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            // Validate provisioning even when returning an existing key: a closed fence cannot be bypassed by a hit.
            var observed = attempt == 0 ? initial : await RequiredFence(eventStore, eventStoreNamespace, identifier);
            if (observed.Lifecycle != initial.Lifecycle)
            {
                throw new EncryptionKeyFenceChanged(identifier);
            }

            var revision = observed.RevisionForNewKey(identifier, key);
            var existing = await ReadPublishedFor(collection, identifier, null, observed);
            if (existing is not null)
            {
                return AtRevision(existing);
            }

            try
            {
                return await StageAndPublish(eventStore, eventStoreNamespace, collection, identifier, key, revision, observed);
            }
            catch (MongoWriteException exception) when (IsDuplicate(exception))
            {
                await Backoff(attempt);
            }
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetOrAddFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key) =>
        (await GetOrAddWithRevisionFor(eventStore, eventStoreNamespace, identifier, key)).Key;

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision) is not null;

    /// <inheritdoc/>
    public async Task<EncryptionKey?> TryGetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        var document = await TryReadPublishedFor(eventStore, eventStoreNamespace, identifier, revision);
        return document is null ? null : new(document.PublicKey, document.PrivateKey);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision) ?? throw new MissingEncryptionKey(identifier);

    /// <inheritdoc/>
    public async Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        var collection = await GetCollection(eventStore, eventStoreNamespace);
        if (IsLatest(revision))
        {
            await collection.DeleteManyAsync(_ => _.Id.Identifier == identifier);
        }
        else
        {
            await collection.DeleteOneAsync(_ => _.Id == new EncryptionKeyId(identifier, revision!));
        }
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyErasure?> GetFenceFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        try
        {
            var document = await GetErasureCollection(eventStore, eventStoreNamespace)
                .WithReadConcern(ReadConcern.Linearizable).WithReadPreference(ReadPreference.Primary)
                .Find(_ => _.Id == identifier, new FindOptions { MaxTime = _readTimeout }).FirstOrDefaultAsync();
            var fence = document?.ToFence();
            if (fence is not null && !HasValidSeal(fence))
            {
                throw new EncryptionKeyFenceUnavailable(identifier);
            }

            return fence;
        }
        catch (MongoException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
        catch (TimeoutException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }

    /// <inheritdoc/>
    public Task<EncryptionKeyErasure?> GetErasureForRelease(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        GetFenceFor(eventStore, eventStoreNamespace, identifier);

    /// <inheritdoc/>
    public async Task<EncryptionKeyErasure?> GetErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var fence = await GetFenceFor(eventStore, eventStoreNamespace, identifier);
        return fence is not null && (fence.Version > 0 || !fence.NewKeyAllowed || fence.ErasedThrough.Value > 0 || fence.ErasedKeyFingerprints.Count > 0 || fence.ErasedLifecycle > 0 || fence.Requests?.Count > 0) ? fence : null;
    }

    /// <inheritdoc/>
    public async Task RecordErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyErasureRequest request)
    {
        var collection = await GetCollection(eventStore, eventStoreNamespace);
        var initial = await EnsureFence(eventStore, eventStoreNamespace, identifier);
        var lifecycle = request.Lifecycle == 0 ? initial.Lifecycle : request.Lifecycle;
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var observed = await RequiredFence(eventStore, eventStoreNamespace, identifier);
            if (observed.Requests?.Any(_ => _.RequestId == request.RequestId) == true)
            {
                return;
            }

            if (await EncryptionKeyRequestArchive.Read(this, eventStore, eventStoreNamespace, identifier, request.RequestId) is not null)
            {
                // A no-op CAS validates the Version observed before consulting the separate archive.
                var stamped = observed with { Version = checked(observed.Version + 1) };
                if (await WriteFence(eventStore, eventStoreNamespace, EncryptionKeyFenceUpdates.Observed(identifier, observed), EncryptionKeyFenceUpdates.Compact(stamped))) return;
                await Backoff(attempt);
                continue;
            }

            if (lifecycle != observed.Lifecycle)
            {
                throw new EncryptionKeyFenceChanged(identifier);
            }

            if (observed.Requests?.Count >= MaximumRequests)
            {
                throw new EncryptionKeyRequestLogFull(identifier);
            }

            var present = await collection.Find(_ => _.Id.Identifier == identifier).ToListAsync();
            var next = EncryptionKeyErasure.Covering(observed, present.Select(_ => (_.Id.Revision, new EncryptionKey(_.PublicKey, _.PrivateKey))));
            var recorded = new EncryptionKeyErasureRequest(request.RequestId, next.ErasedThrough, observed.Lifecycle, next.ErasedKeyFingerprints);
            var filter = EncryptionKeyFenceUpdates.Observed(identifier, observed) & _fences.Ne("requests.requestId", request.RequestId);
            if (await WriteFence(eventStore, eventStoreNamespace, filter, EncryptionKeyFenceUpdates.Raise(next, recorded)))
            {
                return;
            }

            await Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <inheritdoc/>
    public Task RecordErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        RecordErasureFor(eventStore, eventStoreNamespace, identifier, new(Guid.NewGuid().ToString("N")));

    /// <inheritdoc/>
    public async Task DeleteThroughFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        var collection = await GetCollection(eventStore, eventStoreNamespace);
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var observed = await RequiredFence(eventStore, eventStoreNamespace, identifier);
            var request = observed.Requests?.SingleOrDefault(_ => _.RequestId == requestId) ?? await EncryptionKeyRequestArchive.Read(this, eventStore, eventStoreNamespace, identifier, requestId) ?? throw new EncryptionKeyFenceUnavailable(identifier);
            if (request.Confirmed) return;
            var covered = Covered(identifier, request);
            var present = await collection.Find(covered).ToListAsync();
            var fingerprints = (request.Fingerprints ?? []).Concat(present.Select(_ => new EncryptionKey(_.PublicKey, _.PrivateKey).Fingerprint)).Distinct(StringComparer.Ordinal).ToArray();
            if (fingerprints.Length != (request.Fingerprints?.Count ?? 0))
            {
                var updatedRequest = request with { Fingerprints = fingerprints };
                var next = observed with
                {
                    Version = checked(observed.Version + 1),
                    ErasedKeyFingerprints = observed.ErasedKeyFingerprints.Concat(fingerprints).Distinct(StringComparer.Ordinal).ToArray()
                };
                if (!await WriteRequest(eventStore, eventStoreNamespace, identifier, observed, next, updatedRequest))
                {
                    await Backoff(attempt);
                    continue;
                }
            }

            // The predicate, not the observed row list, also removes a writer staging after the read above.
            await collection.WithWriteConcern(WriteConcern.WMajority).DeleteManyAsync(covered);
            return;
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <inheritdoc/>
    public async Task ConfirmErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        var collection = await GetCollection(eventStore, eventStoreNamespace);
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var observed = await RequiredFence(eventStore, eventStoreNamespace, identifier);
            var request = observed.Requests?.SingleOrDefault(_ => _.RequestId == requestId) ?? await EncryptionKeyRequestArchive.Read(this, eventStore, eventStoreNamespace, identifier, requestId) ?? throw new EncryptionKeyFenceUnavailable(identifier);
            if (request.Confirmed) return;
            await VerifySwept(collection, identifier, request);
            if (request.Confirmed)
            {
                return;
            }

            var next = observed with { Version = checked(observed.Version + 1) };
            if (await WriteRequest(eventStore, eventStoreNamespace, identifier, observed, next, request with { Confirmed = true }))
            {
                return;
            }

            await Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, ulong observedVersion, uint observedLifecycle)
    {
        var observed = await RequiredFence(eventStore, eventStoreNamespace, identifier);
        if (IsAuthorizationRetry(observed, observedVersion, observedLifecycle))
        {
            return;
        }

        if (observed.NewKeyAllowed || observed.Version != observedVersion || observed.Lifecycle != observedLifecycle)
        {
            throw new EncryptionKeyFenceChanged(identifier);
        }

        var next = observed with { Version = checked(observed.Version + 1), Lifecycle = checked(observed.Lifecycle + 1), NewKeyAllowed = true };
        var filter = EncryptionKeyFenceUpdates.Observed(identifier, observed) & _fences.Eq(_ => _.NewKeyAllowed, false);
        if (!await WriteFence(eventStore, eventStoreNamespace, filter, EncryptionKeyFenceUpdates.Authorize(next)))
        {
            var current = await RequiredFence(eventStore, eventStoreNamespace, identifier);
            if (!IsAuthorizationRetry(current, observedVersion, observedLifecycle))
            {
                throw new EncryptionKeyFenceChanged(identifier);
            }
        }
    }

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var observed = await GetFenceFor(eventStore, eventStoreNamespace, identifier);
        if (observed?.NewKeyAllowed == false)
        {
            await AllowNewKeyFor(eventStore, eventStoreNamespace, identifier, observed.Version, observed.Lifecycle);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The secret is a document in <see cref="InternalSecretsCollectionName"/>, keyed by the exact event store and purpose. Inserting it is the
    /// ordering point: a concurrent creator that loses the race reads back the winner's document.
    /// </remarks>
    public async Task<EncryptionKey> GetOrAddInternalSecretFor(EventStoreName eventStore, InternalSecretPurpose purpose, EncryptionKey candidate)
    {
        var collection = database
            .GetEventStoreDatabase(eventStore)
            .GetCollection<InternalSecretForPurpose>(InternalSecretsCollectionName);
        var id = BuildInternalSecretId(eventStore, purpose);
        if (await collection.Find(_ => _.Id == id).FirstOrDefaultAsync() is { } existing)
        {
            return new(existing.PublicKey, existing.PrivateKey);
        }

        try
        {
            await collection.InsertOneAsync(new InternalSecretForPurpose(id, candidate.Public, candidate.Private));
            return candidate;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            var winner = await collection.Find(_ => _.Id == id).FirstOrDefaultAsync()
                ?? throw new InternalSecretProvisioningConflict(purpose);
            return new(winner.PublicKey, winner.PrivateKey);
        }
    }

    /// <summary>
    /// Builds an unambiguous identity even when event stores share a physical database.
    /// </summary>
    /// <param name="eventStore">The exact event store name.</param>
    /// <param name="purpose">The secret purpose.</param>
    /// <returns>The length-prefixed event store name followed by the purpose.</returns>
    internal static string BuildInternalSecretId(EventStoreName eventStore, InternalSecretPurpose purpose) =>
        $"{eventStore.Value.Length.ToString(CultureInfo.InvariantCulture)}:{eventStore.Value}{purpose.Value}";

    /// <summary>
    /// Include published and legacy rows, but never staged rows.
    /// </summary>
    /// <param name="identifier">The subject.</param>
    /// <param name="revision">The optional revision.</param>
    /// <param name="fence">The authoritative boundary, if present.</param>
    /// <returns>The visibility predicate.</returns>
    internal static FilterDefinition<EncryptionKeyForIdentifier> Visible(EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null, EncryptionKeyErasure? fence = null)
    {
        var filter = (IsLatest(revision) ? _keys.Eq(_ => _.Id.Identifier, identifier) : _keys.Eq(_ => _.Id, new EncryptionKeyId(identifier, revision!))) & _keys.Ne(_ => _.Published, false);
        if (fence is null)
        {
            return filter;
        }

        return filter & _keys.Gt(_ => _.Id.Revision, fence.ErasedThrough) &
            (_keys.Eq(_ => _.StagedLifecycle, null) | _keys.Gt(_ => _.StagedLifecycle, fence.ErasedLifecycle));
    }

    /// <summary>
    /// Select only revisions or staged lifecycles covered by this immutable request.
    /// </summary>
    /// <param name="identifier">The subject.</param>
    /// <param name="request">The recorded boundary.</param>
    /// <returns>The bounded sweep predicate.</returns>
    internal static FilterDefinition<EncryptionKeyForIdentifier> Covered(EncryptionKeyIdentifier identifier, EncryptionKeyErasureRequest request) =>
        _keys.Eq(_ => _.Id.Identifier, identifier) &
        (_keys.Lte(_ => _.Id.Revision, request.Through ?? EncryptionKeyRevision.Initial) |
         (_keys.Ne(_ => _.StagedLifecycle, null) & _keys.Lte(_ => _.StagedLifecycle, request.Lifecycle)));

    /// <summary>
    /// Select only the stage owned by this writer, including its immutable lifecycle.
    /// </summary>
    /// <param name="document">The writer's stage.</param>
    /// <returns>The conditional publication and undo predicate.</returns>
    internal static FilterDefinition<EncryptionKeyForIdentifier> Owned(EncryptionKeyForIdentifier document) =>
        _keys.Eq(_ => _.Id, document.Id) & _keys.Eq(_ => _.WriterNonce, document.WriterNonce) & _keys.Eq(_ => _.StagedLifecycle, document.StagedLifecycle);

    /// <summary>Validate N seals, or require a seal after the N+1 rollout backfill.</summary>
    /// <param name="fence">The persisted fence.</param>
    /// <param name="requireSeal">Whether the N+1 protocol floor has been established.</param>
    /// <returns>Whether the fence has a valid rollout seal.</returns>
    internal static bool HasValidSeal(EncryptionKeyErasure fence, bool requireSeal = false) =>
        fence.Seal is { } seal ? StringComparer.Ordinal.Equals(seal, EncryptionKeyFenceUpdates.Seal(fence)) : !requireSeal && fence.Version == 0;
    static bool IsAuthorizationRetry(EncryptionKeyErasure fence, ulong version, uint lifecycle) =>
        fence.NewKeyAllowed && fence.Version > version && fence.Version - version == 1 && fence.Lifecycle > lifecycle && fence.Lifecycle - lifecycle == 1;
    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;
    static bool IsDuplicate(MongoWriteException exception) => exception.WriteError?.Category == ServerErrorCategory.DuplicateKey;
    static Task Backoff(int attempt) => Task.Delay(Random.Shared.Next(1, 4) * (attempt + 1));
    static EncryptionKeyAtRevision AtRevision(EncryptionKeyForIdentifier document) => new(new(document.PublicKey, document.PrivateKey), document.Id.Revision, document.StagedLifecycle);
    static async Task<EncryptionKeyForIdentifier?> ReadPublishedFor(IMongoCollection<EncryptionKeyForIdentifier> collection, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision, EncryptionKeyErasure? fence)
    {
        var document = await collection.Find(Visible(identifier, revision, fence)).SortByDescending(_ => _.Id.Revision).FirstOrDefaultAsync();
        return document is not null && fence?.Covers(AtRevision(document)) != true ? document : null;
    }

    static async Task VerifySwept(IMongoCollection<EncryptionKeyForIdentifier> collection, EncryptionKeyIdentifier identifier, EncryptionKeyErasureRequest request)
    {
        try
        {
            if (await collection.WithReadConcern(ReadConcern.Linearizable).WithReadPreference(ReadPreference.Primary)
                .Find(Covered(identifier, request), new FindOptions { MaxTime = _readTimeout }).Limit(1).AnyAsync())
            {
                throw new EncryptionKeyFenceUnavailable(identifier);
            }
        }
        catch (MongoException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
        catch (TimeoutException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }

    static async Task<EncryptionKeyRevision> GetNextRevisionFor(IMongoCollection<EncryptionKeyForIdentifier> collection, EncryptionKeyIdentifier identifier, EncryptionKeyErasure observed)
    {
        var highest = await collection.Find(_ => _.Id.Identifier == identifier).SortByDescending(_ => _.Id.Revision).FirstOrDefaultAsync();
        return new(checked(Math.Max(highest?.Id.Revision.Value ?? 0, observed.ErasedThrough.Value) + 1));
    }

    async Task<EncryptionKeyForIdentifier?> TryReadPublishedFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        var collection = await GetCollection(eventStore, eventStoreNamespace);
        var fence = await GetFenceFor(eventStore, eventStoreNamespace, identifier);
        return await ReadPublishedFor(collection, identifier, revision, fence);
    }

    async Task<EncryptionKeyAtRevision> StageAndPublish(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, IMongoCollection<EncryptionKeyForIdentifier> collection, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision revision, EncryptionKeyErasure observed)
    {
        var staged = new EncryptionKeyForIdentifier(new(identifier, revision), key.Public, key.Private, false, observed.Lifecycle, Guid.NewGuid().ToString("N"));
        var durable = collection.WithWriteConcern(WriteConcern.WMajority);
        await durable.InsertOneAsync(staged);
        try
        {
            var current = await RequiredFence(eventStore, eventStoreNamespace, identifier);
            if (current.Version < observed.Version)
            {
                throw new EncryptionKeyFenceUnavailable(identifier);
            }

            current.EnsureStillAllowed(identifier, AtRevision(staged));
            var result = await durable.UpdateOneAsync(Owned(staged), Builders<EncryptionKeyForIdentifier>.Update.Set(_ => _.Published, true));
            if (result.MatchedCount == 0)
            {
                current = await RequiredFence(eventStore, eventStoreNamespace, identifier);
                current.EnsureStillAllowed(identifier, AtRevision(staged));
                throw new EncryptionKeyFenceUnavailable(identifier);
            }

            return AtRevision(staged);
        }
        catch (Exception exception) when (exception is EncryptionKeyErased or EncryptionKeyFenceUnavailable)
        {
            await durable.DeleteOneAsync(Owned(staged));
            throw;
        }
    }

    async Task<EncryptionKeyErasure> EnsureFence(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        // The id-only upsert cannot weaken an existing fence. An acknowledged result is still read linearly.
        try
        {
            await GetErasureCollection(eventStore, eventStoreNamespace).WithWriteConcern(WriteConcern.WMajority)
                .UpdateOneAsync(_ => _.Id == identifier, EncryptionKeyFenceUpdates.Ensure(identifier), new UpdateOptions { IsUpsert = true });
        }
        catch (MongoWriteException exception) when (IsDuplicate(exception))
        {
            // Another initializer won; this insert definitely did not apply.
        }

        return await RequiredFence(eventStore, eventStoreNamespace, identifier);
    }

    async Task<EncryptionKeyErasure> RequiredFence(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        await GetFenceFor(eventStore, eventStoreNamespace, identifier) ?? throw new EncryptionKeyFenceUnavailable(identifier);

    async Task<bool> WriteFence(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, FilterDefinition<EncryptionKeyErasureForIdentifier> filter, UpdateDefinition<EncryptionKeyErasureForIdentifier> update)
    {
        var result = await GetErasureCollection(eventStore, eventStoreNamespace).WithWriteConcern(WriteConcern.WMajority)
            .FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<EncryptionKeyErasureForIdentifier> { ReturnDocument = ReturnDocument.After, MaxTime = _readTimeout });
        return result is not null;
    }

    Task<bool> WriteRequest(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyErasure observed, EncryptionKeyErasure next, EncryptionKeyErasureRequest request) =>
        WriteFence(eventStore, eventStoreNamespace, EncryptionKeyFenceUpdates.Observed(identifier, observed) & _fences.Eq("requests.requestId", request.RequestId), EncryptionKeyFenceUpdates.UpdateRequest(next, request));

    IMongoCollection<EncryptionKeyErasureForIdentifier> GetErasureCollection(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace) =>
        database.GetEventStoreDatabase(eventStore).GetNamespaceDatabase(eventStoreNamespace).GetCollection<EncryptionKeyErasureForIdentifier>(WellKnownCollectionNames.EncryptionKeyErasures);

    async Task<IMongoCollection<EncryptionKeyForIdentifier>> GetCollection(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace)
    {
        var collection = database.GetEventStoreDatabase(eventStore).GetNamespaceDatabase(eventStoreNamespace).GetCollection<EncryptionKeyForIdentifier>(CollectionName);
        await collection.EnsureIndexesOnceAsync(_ensuredIndexes, Indexes.ToArray()).ConfigureAwait(false);
        return collection;
    }
}
