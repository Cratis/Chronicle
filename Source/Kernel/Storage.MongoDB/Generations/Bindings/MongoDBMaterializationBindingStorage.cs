// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
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
public sealed class MongoDBMaterializationBindingStorage(IDatabase database, IGenerationGateWrites gates) : IMaterializationBindingStorage
{
    static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;
    static readonly UpdateDefinitionBuilder<BsonDocument> _update = Builders<BsonDocument>.Update;

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
    public async Task<MaterializationPublication> TryPublish(EventStoreName store, MaterializationKey key, MaterializationGeneration expectedActive, ServingEpoch expectedEpoch, MaterializationGeneration candidate, PublicationReceiptId receipt)
    {
        var db = await Prepared(store, key);
        for (var attempt = 0; ; attempt++)
        {
            using var session = await db.Client.StartSessionAsync();
            try
            {
                session.StartTransaction(new(ReadConcern.Snapshot, ReadPreference.Primary, WriteConcern.WMajority));
                var previous = await Receipts(db).Find(session, ReceiptId(receipt)).FirstOrDefaultAsync();
                if (previous is not null)
                {
                    await session.AbortTransactionAsync();
                    return MaterializationBindingDocuments.Outcome(previous, key, expectedActive, expectedEpoch, candidate);
                }
                var doc = await Bindings(db).Find(session, Id(key)).FirstOrDefaultAsync();
                if (doc is null)
                {
                    await session.AbortTransactionAsync();
                    return new(MaterializationPublicationResult.UnknownMaterialization, MaterializationGeneration.Unspecified, ServingEpoch.Unset);
                }
                var binding = MaterializationBindingDocuments.Binding(doc);
                var outcome = Decide(binding, expectedActive, expectedEpoch, candidate);
                if (outcome.Result == MaterializationPublicationResult.Published && !await Publish(db, session, binding, expectedActive, expectedEpoch, candidate, receipt))
                {
                    await session.AbortTransactionAsync();
                    if (attempt >= 4)
                    {
                        throw new InvalidOperationException("Materialization publication conflicted repeatedly.");
                    }
                    continue;
                }
                await Receipts(db).InsertOneAsync(session, MaterializationBindingDocuments.Receipt(key, expectedActive, expectedEpoch, candidate, receipt, outcome));
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
                var previous = await Receipts(db).WithReadConcern(ReadConcern.Majority).Find(ReceiptId(receipt)).FirstOrDefaultAsync();
                if (previous is not null)
                {
                    return MaterializationBindingDocuments.Outcome(previous, key, expectedActive, expectedEpoch, candidate);
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

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<GenerationContainerClaim>> GetClaims(EventStoreName store, EventStoreNamespaceName ns, ReadModelContainerName container) =>
        await new MongoDBGenerationContainerClaims(database.GetReadModelDatabase(store, ns)).For(container.Value);

    static IMongoCollection<BsonDocument> Bindings(IMongoDatabase db) => db.GetCollection<BsonDocument>(MaterializationBindingCollections.Bindings).WithWriteConcern(WriteConcern.WMajority).WithReadConcern(ReadConcern.Majority);
    static IMongoCollection<BsonDocument> Receipts(IMongoDatabase db) => db.GetCollection<BsonDocument>(MaterializationBindingCollections.Receipts);
    static FilterDefinition<BsonDocument> Id(MaterializationKey key) => _filter.Eq("_id", key.Value);
    static FilterDefinition<BsonDocument> ReceiptId(PublicationReceiptId receipt) => _filter.Eq("_id", receipt.Value.ToString("N"));
    static Task Abort(IClientSessionHandle session) => session.IsInTransaction ? session.AbortTransactionAsync() : Task.CompletedTask;

    static MaterializationPublication Decide(MaterializationBinding binding, MaterializationGeneration active, ServingEpoch epoch, MaterializationGeneration candidate)
    {
        if (binding.Active != active || binding.Epoch != epoch)
        {
            return new(MaterializationPublicationResult.StaleBinding, binding.Active, binding.Epoch);
        }
        if (!binding.Generations.Any(_ => _.Identity.Generation == candidate && _.State == MaterializationGenerationState.Following))
        {
            return new(MaterializationPublicationResult.CandidateNotFollowing, binding.Active, binding.Epoch);
        }
        return new(MaterializationPublicationResult.Published, candidate, epoch.Next());
    }

    async Task<bool> Publish(IMongoDatabase db, IClientSessionHandle session, MaterializationBinding binding, MaterializationGeneration active, ServingEpoch epoch, MaterializationGeneration candidate, PublicationReceiptId receipt)
    {
        var filter = Id(binding.Key) & _filter.Eq("active", (int)active.Value) & _filter.Eq("epoch", (long)epoch.Value)
            & _filter.ElemMatch("generations", _filter.Eq("generation", (int)candidate.Value) & _filter.Eq("state", (int)MaterializationGenerationState.Following));
        var update = _update.Set("active", (int)candidate.Value).Set("epoch", (long)epoch.Next().Value)
            .Set("destination", binding.Generations.Single(_ => _.Identity.Generation == candidate).Identity.Destination.Value)
            .Set("lastReceipt", receipt.Value.ToString("N")).Set("generations.$[candidate].state", (int)MaterializationGenerationState.Active)
            .Set("generations.$[old].state", (int)MaterializationGenerationState.Retired).Set("generations.$[old].supersededBy", (int)candidate.Value);
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
