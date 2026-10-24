// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Storage.Generations.Bindings;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings;

/// <summary>
/// Encodes the versioned materialization storage shape.
/// </summary>
internal static class MaterializationBindingDocuments
{
    /// <summary>
    /// Converts or prepares materialization storage values.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="record">The record.</param>
    /// <returns>The stored representation or decoded value.</returns>
    internal static BsonDocument First(MaterializationKey key, MaterializationGenerationRecord record) => new()
    {
        { "_id", key.Value }, { "incarnation", new BsonBinaryData(key.Incarnation.Value, GuidRepresentation.Standard) },
        { "namespace", key.Namespace.Value }, { "projection", key.Projection.Value }, { "sinkType", key.SinkType.Value },
        { "sinkConfiguration", new BsonBinaryData(key.SinkConfiguration.Value, GuidRepresentation.Standard) },
        { "container", record.Identity.Destination.Value }, { "active", 1 }, { "destination", record.Identity.Destination.Value },
        { "epoch", 0L }, { "next", 1 }, { "fence", BsonNull.Value }, { "lastReceipt", BsonNull.Value },
        { "generations", new BsonArray { Encode(record) } }
    };

    /// <summary>
    /// Converts or prepares materialization storage values.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <returns>The stored representation or decoded value.</returns>
    internal static BsonDocument Encode(MaterializationGenerationRecord record) => new()
    {
        { "generation", checked((int)record.Identity.Generation.Value) }, { "observer", record.Identity.ObserverId.Value },
        { "destination", record.Identity.Destination.Value }, { "state", (int)record.State }, { "revision", record.Revision.Value },
        { "createdAt", record.CreatedAt.ToString("O", CultureInfo.InvariantCulture) },
        { "supersededBy", record.SupersededBy is null ? BsonNull.Value : checked((int)record.SupersededBy.Value) }
    };

    /// <summary>
    /// Converts or prepares materialization storage values.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="projection">The projection.</param>
    /// <returns>The stored representation or decoded value.</returns>
    internal static MaterializationGenerationRecord Generation(BsonDocument document, ProjectionId projection) => new(
        new(projection, new(checked((uint)document["generation"].AsInt32)), new(document["observer"].AsString), new(document["destination"].AsString)),
        (MaterializationGenerationState)document["state"].AsInt32,
        new(document["revision"].AsString),
        DateTimeOffset.Parse(document["createdAt"].AsString, CultureInfo.InvariantCulture),
        document["supersededBy"].IsBsonNull ? null : new(checked((uint)document["supersededBy"].AsInt32)));

    /// <summary>
    /// Converts or prepares materialization storage values.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <returns>The stored representation or decoded value.</returns>
    internal static MaterializationBinding Binding(BsonDocument document)
    {
        var key = new MaterializationKey(
            new(document["incarnation"].AsBsonBinaryData.ToGuid(GuidRepresentation.Standard)),
            new(document["namespace"].AsString),
            new(document["projection"].AsString),
            new(document["sinkType"].AsString),
            new(document["sinkConfiguration"].AsBsonBinaryData.ToGuid(GuidRepresentation.Standard)));
        var active = new MaterializationGeneration(checked((uint)document["active"].AsInt32));
        return new(
            key,
            new(document["container"].AsString),
            active,
            new(document["destination"].AsString),
            new(checked((uint)document["epoch"].AsInt64)),
            new(checked((uint)document["next"].AsInt32)),
            document["fence"].IsBsonNull ? null : new(new(key, active), document["fence"]["epoch"].AsInt64),
            document["lastReceipt"].IsBsonNull ? null : new(Guid.Parse(document["lastReceipt"].AsString)),
            Array.AsReadOnly(document["generations"].AsBsonArray.Select(_ => Generation(_.AsBsonDocument, key.Projection)).ToArray()));
    }

    /// <summary>
    /// Converts or prepares materialization storage values.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="expectedActive">The expectedActive.</param>
    /// <param name="expectedEpoch">The expectedEpoch.</param>
    /// <param name="candidate">The candidate.</param>
    /// <param name="receipt">The receipt.</param>
    /// <param name="outcome">The outcome.</param>
    /// <returns>The stored representation or decoded value.</returns>
    internal static BsonDocument Receipt(MaterializationKey key, MaterializationGeneration expectedActive, ServingEpoch expectedEpoch, MaterializationGeneration candidate, PublicationReceiptId receipt, MaterializationPublication outcome) => new()
    {
        { "_id", receipt.Value.ToString("N") }, { "key", key.Value }, { "expectedActive", checked((int)expectedActive.Value) },
        { "expectedEpoch", expectedEpoch.Value }, { "candidate", checked((int)candidate.Value) }, { "result", (int)outcome.Result },
        { "active", checked((int)outcome.Active.Value) }, { "epoch", outcome.Epoch.Value }, { "at", DateTime.UtcNow }
    };

    /// <summary>
    /// Converts or prepares materialization storage values.
    /// </summary>
    /// <param name="receipt">The receipt.</param>
    /// <param name="key">The key.</param>
    /// <param name="active">The active.</param>
    /// <param name="epoch">The epoch.</param>
    /// <param name="candidate">The candidate.</param>
    /// <returns>The stored representation or decoded value.</returns>
    internal static MaterializationPublication Outcome(BsonDocument receipt, MaterializationKey key, MaterializationGeneration active, ServingEpoch epoch, MaterializationGeneration candidate) => new(
        receipt["key"].AsString == key.Value && receipt["expectedActive"].AsInt32 == active.Value && receipt["expectedEpoch"].AsInt64 == epoch.Value && receipt["candidate"].AsInt32 == candidate.Value
            ? (MaterializationPublicationResult)receipt["result"].AsInt32 : MaterializationPublicationResult.ReceiptConflict,
        new(checked((uint)receipt["active"].AsInt32)),
        new(checked((uint)receipt["epoch"].AsInt64)));
}
