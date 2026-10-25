// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.MongoDB.Generations.Gates;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings;

/// <summary>Stores bindings with receipt-first, majority-committed publication.</summary>
/// <param name="database">The read-model database resolver.</param>
/// <param name="gates">The gate writes joining publication.</param>
/// <param name="timeProvider">The clock used for lifecycle stamps.</param>
public sealed class MongoDBMaterializationBindingStorage(IDatabase database, IGenerationGateWrites gates, TimeProvider? timeProvider = null) : IMaterializationBindingStorage, IGenerationBuildStorage, IGenerationWriterGrants
{
    static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;
    static readonly UpdateDefinitionBuilder<BsonDocument> _update = Builders<BsonDocument>.Update;
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    /// <inheritdoc/>
    public async Task<GenerationWriterGrant?> TryAcquire(EventStoreName store, GenerationObserverBinding binding, GenerationBuildOperationId attempt, long expectedEpoch)
    {
        if (expectedEpoch < 0 || attempt.Value == Guid.Empty) return null;
        var db = await PreparedBuilds(store, binding.Key);
        try
        {
            return await BuildTransaction<GenerationWriterGrant?>(db, async session =>
            {
                if (!await HasAdmissibleExecution(db, session, binding)) return null;
                var gate = new GenerationGateId(binding.Key, binding.Identity.Generation);
                var collection = db.GetCollection<BsonDocument>(GenerationGateCollections.Gates);
                var current = await collection.Find(session, _filter.Eq("_id", gate.Value)).FirstOrDefaultAsync();
                if ((current?[GenerationGateCollections.Epoch].AsInt64 ?? 0) != expectedEpoch) return null;
                var epoch = checked(expectedEpoch + 1);
                if (current is null)
                    await collection.InsertOneAsync(session, new BsonDocument { { "_id", gate.Value }, { GenerationGateCollections.Epoch, epoch }, { GenerationGateCollections.Admitted, 0L }, { GenerationGateCollections.Granted, epoch }, { "attempt", attempt.Value.ToString("N") } });
                else
                    await collection.UpdateOneAsync(session, _filter.Eq("_id", gate.Value) & _filter.Eq(GenerationGateCollections.Epoch, expectedEpoch), _update.Set(GenerationGateCollections.Epoch, epoch).Set(GenerationGateCollections.Granted, epoch).Set("attempt", attempt.Value.ToString("N")));
                return new GenerationWriterGrant(store, binding, attempt, epoch);
            });
        }
        catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<GenerationWriterGrant?> GetAdmitted(EventStoreName store, GenerationObserverBinding binding)
    {
        // A read: it must not create the build collections or indexes that the writers ensure.
        var db = database.GetReadModelDatabase(store, binding.Key.Namespace);
        return await BuildTransaction<GenerationWriterGrant?>(db, async session =>
        {
            var gate = new GenerationGateId(binding.Key, binding.Identity.Generation);
            var current = await db.GetCollection<BsonDocument>(GenerationGateCollections.Gates).Find(session, _filter.Eq("_id", gate.Value)).FirstOrDefaultAsync();
            if (current is null) return null;
            if (!current.TryGetValue(GenerationGateCollections.Granted, out var granted) || !granted.IsInt64) return null;
            if (!current.TryGetValue("attempt", out var stored) || !stored.IsString) return null;
            var epoch = current[GenerationGateCollections.Epoch].AsInt64;
            if (epoch <= 0 || granted.AsInt64 != epoch) return null;
            if (!Guid.TryParseExact(stored.AsString, "N", out var attempt) || attempt == Guid.Empty) return null;
            if (!await HasAdmissibleExecution(db, session, binding)) return null;
            return new GenerationWriterGrant(store, binding, new GenerationBuildOperationId(attempt), epoch);
        });
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidMaterializationExecution">The adopted execution is missing.</exception>
    public async Task<GenerationReservation> ReserveNext(EventStoreName store, MaterializationKey key, MaterializationExecutionSpecification execution, GenerationBuildOperationId operationId, DateTimeOffset at)
    {
        execution.EnsureValid();
        var db = await PreparedBuilds(store, key);
        return await BuildTransaction(db, async session =>
        {
            var operation = new BsonDocument { { "_id", $"{key.Incarnation.Value:N}/{operationId.Value:N}" }, { "key", key.Value }, { "execution", MaterializationBindingDocuments.Execution(execution) }, { "at", MaterializationBindingDocuments.Timestamp(at) } };
            var operations = db.GetCollection<BsonDocument>(MaterializationBindingCollections.Operations);
            var previous = await operations.Find(session, _filter.Eq("_id", operation["_id"])).FirstOrDefaultAsync();
            if (previous is not null)
            {
                return ReadBuildOperation(previous, operation, key);
            }
            var document = await Bindings(db).Find(session, Id(key)).FirstOrDefaultAsync();
            if (document is null) return new GenerationReservation(GenerationReservationResult.UnknownMaterialization);
            var binding = MaterializationBindingDocuments.Binding(document);
            if (binding.Epoch == ServingEpoch.Unset) return new GenerationReservation(GenerationReservationResult.NotBound);
            if (!await Executions(db).Find(session, ExecutionId(key, MaterializationGeneration.First)).AnyAsync()) throw new InvalidMaterializationExecution();
            var candidate = binding.Generations.FirstOrDefault(_ => _.State is MaterializationGenerationState.Building or MaterializationGenerationState.Following);
            if (candidate is not null) return new GenerationReservation(GenerationReservationResult.CandidateInFlight, candidate);
            var identity = MaterializationGenerationIdentity.For(key.Projection, binding.Next.Next(), binding.Container);
            var claim = new GenerationObserverBinding(key, identity, execution.EventSequence, execution.Revision);
            await EnsureBuildClaims((store, db, session), claim, false);
            var record = new MaterializationGenerationRecord(identity, MaterializationGenerationState.Building, execution.Revision, at);
            await Bindings(db).UpdateOneAsync(session, Id(key), _update.Set("next", checked((int)identity.Generation.Value)).Push("generations", MaterializationBindingDocuments.Encode(record)));
            await InsertExecution(db, session, claim, execution);
            operation.Add("record", MaterializationBindingDocuments.Encode(record));
            await operations.InsertOneAsync(session, operation);
            return new GenerationReservation(GenerationReservationResult.ReservationCreated, record);
        });
    }

    /// <inheritdoc/>
    /// <exception cref="GenerationBuildConflict">The first generation already has different execution inputs.</exception>
    public async Task<bool> TryAdoptExecution(EventStoreName store, MaterializationKey key, MaterializationExecutionSpecification execution)
    {
        execution.EnsureValid();
        var db = await PreparedBuilds(store, key);
        var snapshot = execution with { HasKnownHistoricalProvenance = false };
        return await BuildTransaction(db, async session =>
        {
            var previous = await Executions(db).Find(session, ExecutionId(key, MaterializationGeneration.First)).FirstOrDefaultAsync();
            if (previous is not null)
            {
                if (MaterializationBindingDocuments.Execution(previous["execution"].AsBsonDocument) != snapshot) throw new GenerationBuildConflict();
                return true;
            }
            var document = await Bindings(db).Find(session, Id(key)).FirstOrDefaultAsync();
            if (document is null) return false;
            var first = MaterializationBindingDocuments.Binding(document).Generations.Single(_ => _.Identity.Generation.IsFirst);
            var claim = new GenerationObserverBinding(key, first.Identity, snapshot.EventSequence, snapshot.Revision);
            await EnsureBuildClaims((store, db, session), claim, true);
            await InsertExecution(db, session, claim, snapshot);
            return true;
        });
    }

    /// <inheritdoc/>
    public async Task<MaterializationExecutionSpecification?> GetExecution(EventStoreName store, MaterializationKey key, MaterializationGeneration generation)
    {
        var document = await Executions(database.GetReadModelDatabase(store, key.Namespace)).Find(ExecutionId(key, generation)).FirstOrDefaultAsync();
        return document is null ? null : MaterializationBindingDocuments.Execution(document["execution"].AsBsonDocument);
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidMaterializationExecution">The observer has ambiguous incarnation claims.</exception>
    public async Task<GenerationObserverBinding?> ResolveObserver(EventStoreName store, EventStoreNamespaceName ns, EventSequenceId eventSequence, ObserverId observerId)
    {
        var documents = await database.GetReadModelDatabase(store, ns).GetCollection<BsonDocument>(MaterializationBindingCollections.Observers)
            .Find(_filter.Eq("namespace", ns.Value) & _filter.Eq("eventSequence", eventSequence.Value) & _filter.Eq("observer", observerId.Value)).Limit(2).ToListAsync();
        if (documents.Count > 1) throw new InvalidMaterializationExecution();
        if (documents.Count == 0) await EnsureUnbound(database.GetReadModelDatabase(store, ns), eventSequence, observerId);
        return documents.Count == 0 ? null : MaterializationBindingDocuments.Observer(documents[0]);
    }

    /// <inheritdoc/>
    public async Task<MaterializationBinding?> Get(EventStoreName store, MaterializationKey key)
    {
        var doc = await Bindings(database.GetReadModelDatabase(store, key.Namespace)).Find(Id(key)).FirstOrDefaultAsync();
        return doc is null ? null : MaterializationBindingDocuments.Binding(doc);
    }

    /// <inheritdoc/>
    public async Task<FirstGenerationAdoption> TryAdoptFirst(EventStoreName store, MaterializationKey key, MaterializationGenerationRecord record)
    {
        var db = await Prepared(store, key);
        try
        {
            await Bindings(db).InsertOneAsync(MaterializationBindingDocuments.First(key, record));
            return FirstGenerationAdoption.Adopted;
        }
        catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return (await Get(store, key))!.Generations.FirstOrDefault(_ => _.Identity.Generation.IsFirst) == record
                ? FirstGenerationAdoption.AlreadyAdopted : FirstGenerationAdoption.Conflicting;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TrySetBindingEpoch(EventStoreName store, MaterializationKey key, ServingEpoch expected, ServingEpoch next)
    {
        var db = await Prepared(store, key);
        var result = await Bindings(db).UpdateOneAsync(Id(key) & _filter.In<long>("epoch", [expected.Value, next.Value]), _update.Set("epoch", (long)next.Value));
        return result.MatchedCount == 1;
    }

    /// <inheritdoc/>
    public async Task<GenerationReservation> ReserveNext(EventStoreName store, MaterializationKey key, MaterializationRevision revision, DateTimeOffset at)
    {
        var db = await Prepared(store, key);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var binding = await Get(store, key);
            if (binding is null)
            {
                return new(GenerationReservationResult.UnknownMaterialization);
            }
            if (binding.Epoch == ServingEpoch.Unset)
            {
                return new(GenerationReservationResult.NotBound);
            }
            var candidate = binding.Generations.FirstOrDefault(_ => _.State is MaterializationGenerationState.Building or MaterializationGenerationState.Following);
            if (candidate is not null)
            {
                return new(GenerationReservationResult.CandidateInFlight, candidate);
            }
            var identity = MaterializationGenerationIdentity.For(key.Projection, binding.Next.Next(), binding.Container);
            if (await db.GetCollection<BsonDocument>(identity.Destination.Value).Find(_filter.Empty).Limit(1).AnyAsync())
            {
                return new(GenerationReservationResult.DestinationOccupied);
            }
            var record = new MaterializationGenerationRecord(identity, MaterializationGenerationState.Building, revision, at);
            var noCandidate = _filter.Not(_filter.ElemMatch("generations", _filter.In<int>("state", [(int)MaterializationGenerationState.Building, (int)MaterializationGenerationState.Following])));
            var result = await Bindings(db).UpdateOneAsync(
                Id(key) & _filter.Eq("next", (int)binding.Next.Value) & noCandidate,
                _update.Set("next", checked((int)identity.Generation.Value)).Push("generations", MaterializationBindingDocuments.Encode(record)));
            if (result.MatchedCount == 1)
            {
                return new(GenerationReservationResult.ReservationCreated, record);
            }
        }
        throw new InvalidOperationException("Materialization reservation conflicted repeatedly.");
    }

    /// <inheritdoc/>
    public Task<GenerationTransitionResult> TryMarkFollowing(EventStoreName store, MaterializationKey key, MaterializationGeneration generation) => Transition(store, key, generation, MaterializationGenerationState.Following);

    /// <inheritdoc/>
    public Task<GenerationTransitionResult> TryAbandon(EventStoreName store, MaterializationKey key, MaterializationGeneration generation) => Transition(store, key, generation, MaterializationGenerationState.Abandoned);

    /// <inheritdoc/>
    public Task<MaterializationPublication> TryPublish(EventStoreName store, MaterializationKey key, MaterializationGeneration expectedActive, ServingEpoch expectedEpoch, MaterializationGeneration candidate, PublicationReceiptId receipt) =>
        TryPublish(store, key, expectedActive, expectedEpoch, candidate, TimeSpan.Zero, receipt);

    /// <inheritdoc/>
    public Task<MaterializationPublication> TryPublish(EventStoreName store, MaterializationKey key, MaterializationGeneration expectedActive, ServingEpoch expectedEpoch, MaterializationGeneration candidate, TimeSpan rollbackWindow, PublicationReceiptId receipt) =>
        Activate(store, new(key, expectedActive, expectedEpoch, candidate, rollbackWindow, receipt, false));

    /// <inheritdoc/>
    public Task<MaterializationPublication> TryRestore(EventStoreName store, MaterializationKey key, MaterializationGeneration expectedActive, ServingEpoch expectedEpoch, MaterializationGeneration candidate, TimeSpan rollbackWindow, PublicationReceiptId receipt) =>
        Activate(store, new(key, expectedActive, expectedEpoch, candidate, rollbackWindow, receipt, true));

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<GenerationContainerClaim>> GetClaims(EventStoreName store, EventStoreNamespaceName ns, ReadModelContainerName container) =>
        await new MongoDBGenerationContainerClaims(database.GetReadModelDatabase(store, ns)).For(container.Value);

    static async Task<bool> HasAdmissibleExecution(IMongoDatabase db, IClientSessionHandle session, GenerationObserverBinding binding)
    {
        var document = await Bindings(db).Find(session, Id(binding.Key)).FirstOrDefaultAsync();
        if (document is null) return false;
        var record = MaterializationBindingDocuments.Binding(document).Generations.FirstOrDefault(_ => _.Identity == binding.Identity);
        if (record is null) return false;
        if (record.State == MaterializationGenerationState.Abandoned) return false;
        var execution = await Executions(db).Find(session, ExecutionId(binding.Key, binding.Identity.Generation)).FirstOrDefaultAsync();
        if (execution is null) return false;
        var snapshot = MaterializationBindingDocuments.Execution(execution);
        return snapshot.EventSequence == binding.EventSequence && snapshot.Revision == binding.Revision;
    }

    static async Task EnsureUnbound(IMongoDatabase db, EventSequenceId eventSequence, ObserverId observerId)
    {
        var documents = await Bindings(db).Find(_filter.Eq("generations.observer", observerId.Value)).ToListAsync();
        foreach (var binding in documents.Select(MaterializationBindingDocuments.Binding))
        {
            foreach (var generation in binding.Generations.Where(_ => _.Identity.ObserverId == observerId))
            {
                var document = await Executions(db).Find(ExecutionId(binding.Key, generation.Identity.Generation)).FirstOrDefaultAsync() ?? throw new InvalidMaterializationExecution();
                if (MaterializationBindingDocuments.Execution(document["execution"].AsBsonDocument).EventSequence == eventSequence) throw new InvalidMaterializationExecution();
            }
        }
    }

    static IMongoCollection<BsonDocument> Executions(IMongoDatabase db) => db.GetCollection<BsonDocument>(MaterializationBindingCollections.Executions);
    static FilterDefinition<BsonDocument> ExecutionId(MaterializationKey key, MaterializationGeneration generation) => _filter.Eq("_id", $"{key.Value}/{generation.Value}");

    static GenerationReservation ReadBuildOperation(BsonDocument previous, BsonDocument requested, MaterializationKey key)
    {
        if (previous["key"] != requested["key"]) throw new GenerationBuildConflict();
        if (previous["execution"] != requested["execution"]) throw new GenerationBuildConflict();
        if (previous["at"] != requested["at"]) throw new GenerationBuildConflict();
        return new(GenerationReservationResult.ReservationCreated, MaterializationBindingDocuments.Generation(previous["record"].AsBsonDocument, key.Projection));
    }

    static async Task InsertExecution(IMongoDatabase db, IClientSessionHandle session, GenerationObserverBinding claim, MaterializationExecutionSpecification execution)
    {
        var key = claim.Key;
        var identity = claim.Identity;
        await Executions(db).InsertOneAsync(session, new BsonDocument { { "_id", $"{key.Value}/{identity.Generation.Value}" }, { "execution", MaterializationBindingDocuments.Execution(execution) } });
        await db.GetCollection<BsonDocument>(MaterializationBindingCollections.Observers).InsertOneAsync(session, MaterializationBindingDocuments.Observer(key, identity, execution));
        await db.GetCollection<BsonDocument>(MaterializationBindingCollections.Destinations).InsertOneAsync(session, new BsonDocument { { "_id", identity.Destination.Value }, { "key", key.Value }, { "generation", checked((int)identity.Generation.Value) } });
    }

    static void ValidateDestination(IMongoDatabase db, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new GenerationBuildConflict();
        if (name.Contains('\0')) throw new GenerationBuildConflict();
        if (name.Contains('$')) throw new GenerationBuildConflict();
        if (name.StartsWith("system.", StringComparison.Ordinal)) throw new GenerationBuildConflict();
        if (Encoding.UTF8.GetByteCount($"{db.DatabaseNamespace.DatabaseName}.{name}") > 255) throw new GenerationBuildConflict();
    }

    static async Task<T> BuildTransaction<T>(IMongoDatabase db, Func<IClientSessionHandle, Task<T>> action)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var session = await db.Client.StartSessionAsync();
            try
            {
                return await session.WithTransactionAsync((transaction, _) => action(transaction), new TransactionOptions(readConcern: ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority));
            }
            catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                if (attempt >= 4) throw new GenerationBuildConflict();
            }
        }
    }

    static IMongoCollection<BsonDocument> Bindings(IMongoDatabase db) => db.GetCollection<BsonDocument>(MaterializationBindingCollections.Bindings).WithWriteConcern(WriteConcern.WMajority).WithReadConcern(ReadConcern.Majority);
    static IMongoCollection<BsonDocument> Receipts(IMongoDatabase db) => db.GetCollection<BsonDocument>(MaterializationBindingCollections.Receipts);
    static FilterDefinition<BsonDocument> Id(MaterializationKey key) => _filter.Eq("_id", key.Value);
    static FilterDefinition<BsonDocument> ReceiptId(PublicationReceiptId receipt) => _filter.Eq("_id", receipt.Value.ToString("N"));
    static Task Abort(IClientSessionHandle session) => session.IsInTransaction ? session.AbortTransactionAsync() : Task.CompletedTask;

    async Task EnsureBuildClaims((EventStoreName Store, IMongoDatabase Database, IClientSessionHandle Session) scope, GenerationObserverBinding claim, bool adopting)
    {
        var (store, db, session) = scope;
        var key = claim.Key;
        var identity = claim.Identity;
        var name = identity.Destination.Value;
        ValidateDestination(db, name);
        var others = _filter.Ne("_id", key.Value);
        var observer = _filter.Eq("incarnation", new BsonBinaryData(key.Incarnation.Value, GuidRepresentation.Standard)) & _filter.Eq("generations.observer", identity.ObserverId.Value);
        if (await Bindings(db).Find(session, others & (_filter.Eq("generations.destination", name) | observer)).AnyAsync()) throw new GenerationBuildConflict();
        if (!adopting)
        {
            using var names = await db.ListCollectionNamesAsync(new ListCollectionNamesOptions { Filter = new BsonDocument("name", name) });
            if (await names.AnyAsync()) throw new GenerationBuildConflict();
            if (await new Observation.ObserverDefinitionsStorage(database.GetEventStoreDatabase(store)).Has(identity.ObserverId)) throw new GenerationBuildConflict();
        }
    }

    async Task<IMongoDatabase> PreparedBuilds(EventStoreName store, MaterializationKey key)
    {
        var db = database.GetReadModelDatabase(store, key.Namespace);
        await MaterializationBindingCollections.EnsureBuilds(db);
        return db;
    }

    async Task<MaterializationPublication> Activate(EventStoreName store, MaterializationPublicationRequest request)
    {
        var db = await Prepared(store, request.Key);
        for (var attempt = 0; ; attempt++)
        {
            using var session = await db.Client.StartSessionAsync();
            try
            {
                session.StartTransaction(new(ReadConcern.Snapshot, ReadPreference.Primary, WriteConcern.WMajority));
                var previous = await Receipts(db).Find(session, ReceiptId(request.Receipt)).FirstOrDefaultAsync();
                if (previous is not null)
                {
                    await session.AbortTransactionAsync();
                    return MaterializationBindingDocuments.Outcome(previous, request);
                }
                var doc = await Bindings(db).Find(session, Id(request.Key)).FirstOrDefaultAsync();
                if (doc is null)
                {
                    await session.AbortTransactionAsync();
                    return new(MaterializationPublicationResult.UnknownMaterialization, MaterializationGeneration.Unspecified, ServingEpoch.Unset);
                }
                var binding = MaterializationBindingDocuments.Binding(doc);
                var now = _timeProvider.GetUtcNow();
                var outcome = request.Decide(binding, now);
                if (outcome.Result == MaterializationPublicationResult.Published && !await Publish(db, session, binding, request, now))
                {
                    await session.AbortTransactionAsync();
                    if (attempt >= 4)
                    {
                        throw new InvalidOperationException("Materialization publication conflicted repeatedly.");
                    }
                    continue;
                }
                await Receipts(db).InsertOneAsync(session, MaterializationBindingDocuments.Receipt(request, outcome, now));
                await session.CommitTransactionAsync();
                return outcome;
            }
            catch (MongoException error) when (error.HasErrorLabel("UnknownTransactionCommitResult"))
            {
                throw new MaterializationPublicationOutcomeUnknown(error);
            }
            catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                await Abort(session);
                var previous = await Receipts(db).WithReadConcern(ReadConcern.Majority).Find(ReceiptId(request.Receipt)).FirstOrDefaultAsync();
                if (previous is not null)
                {
                    return MaterializationBindingDocuments.Outcome(previous, request);
                }
                if (attempt >= 4) throw;
            }
            catch (MongoException error) when (error.HasErrorLabel("TransientTransactionError") && attempt < 4)
            {
                await Abort(session);
            }
            catch
            {
                await Abort(session);
                throw;
            }
        }
    }

    async Task<bool> Publish(IMongoDatabase db, IClientSessionHandle session, MaterializationBinding binding, MaterializationPublicationRequest request, DateTimeOffset now)
    {
        var candidate = request.Candidate;
        var active = request.Active;
        var requiredState = request.Restore ? MaterializationGenerationState.Retired : MaterializationGenerationState.Following;
        var filter = Id(binding.Key) & _filter.Eq("active", (int)active.Value) & _filter.Eq("epoch", (long)request.Epoch.Value)
            & _filter.ElemMatch("generations", _filter.Eq("generation", (int)candidate.Value) & _filter.Eq("state", (int)requiredState));
        var update = _update.Set("active", (int)candidate.Value).Set("epoch", (long)request.Epoch.Next().Value)
            .Set("destination", binding.Generations.Single(_ => _.Identity.Generation == candidate).Identity.Destination.Value)
            .Set("lastReceipt", request.Receipt.Value.ToString("N")).Set("generations.$[candidate].state", (int)MaterializationGenerationState.Active)
            .Set("generations.$[candidate].promotedAt", MaterializationBindingDocuments.Timestamp(now))
            .Set("generations.$[old].state", (int)MaterializationGenerationState.Retired).Set("generations.$[old].supersededBy", (int)candidate.Value)
            .Set("generations.$[old].retiredAt", MaterializationBindingDocuments.Timestamp(now))
            .Set("generations.$[old].rollbackDeadline", MaterializationBindingDocuments.Timestamp(now + request.RollbackWindow));
        if (request.Restore)
        {
            update = update.Set("generations.$[candidate].rolledBackFrom", (int)active.Value)
                .Set("generations.$[candidate].retiredAt", BsonNull.Value).Set("generations.$[candidate].rollbackDeadline", BsonNull.Value);
        }
        var result = await Bindings(db).UpdateOneAsync(session, filter, update, new UpdateOptions
        {
            ArrayFilters = [new BsonDocumentArrayFilterDefinition<BsonDocument>(new("candidate.generation", (int)candidate.Value)), new BsonDocumentArrayFilterDefinition<BsonDocument>(new("old.generation", (int)active.Value))]
        });
        if (result.MatchedCount != 1)
        {
            return false;
        }
        var gate = new GenerationGateId(binding.Key, candidate);
        var gateEpoch = await gates.Revoke(db, session, gate);
        await gates.Revoke(db, session, new GenerationGateId(binding.Key, active));
        await Bindings(db).UpdateOneAsync(session, Id(binding.Key), _update.Set("fence", new BsonDocument { { "gate", gate.Value }, { "epoch", gateEpoch } }));
        return true;
    }

    async Task<GenerationTransitionResult> Transition(EventStoreName store, MaterializationKey key, MaterializationGeneration generation, MaterializationGenerationState target)
    {
        var db = await Prepared(store, key);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var record = (await Get(store, key))?.Generations.FirstOrDefault(_ => _.Identity.Generation == generation);
            if (record is null)
            {
                return GenerationTransitionResult.UnknownGeneration;
            }
            if (record.State == target)
            {
                return GenerationTransitionResult.AlreadyInState;
            }
            if (!record.State.CanTransitionTo(target))
            {
                return GenerationTransitionResult.Refused;
            }
            var filter = Id(key) & _filter.ElemMatch("generations", _filter.Eq("generation", (int)generation.Value) & _filter.Eq("state", (int)record.State));
            if ((await Bindings(db).UpdateOneAsync(filter, _update.Set("generations.$.state", (int)target))).MatchedCount == 1)
            {
                return GenerationTransitionResult.Transitioned;
            }
        }
        throw new InvalidOperationException("Materialization transition conflicted repeatedly.");
    }

    async Task<IMongoDatabase> Prepared(EventStoreName store, MaterializationKey key)
    {
        var db = database.GetReadModelDatabase(store, key.Namespace);
        await MaterializationBindingCollections.Ensure(db);
        return db;
    }
}
