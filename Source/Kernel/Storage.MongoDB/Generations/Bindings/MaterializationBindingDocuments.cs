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
        { "supersededBy", record.SupersededBy is null ? BsonNull.Value : checked((int)record.SupersededBy.Value) },
        { "promotedAt", Timestamp(record.PromotedAt) }, { "retiredAt", Timestamp(record.RetiredAt) },
        { "rollbackDeadline", Timestamp(record.RollbackDeadline) },
        { "rolledBackFrom", record.RolledBackFrom is null ? BsonNull.Value : checked((int)record.RolledBackFrom.Value) }
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
        document["supersededBy"].IsBsonNull ? null : new(checked((uint)document["supersededBy"].AsInt32)),
        PromotedAt: Timestamp(document, "promotedAt"),
        RetiredAt: Timestamp(document, "retiredAt"),
        RollbackDeadline: Timestamp(document, "rollbackDeadline"),
        RolledBackFrom: document.GetValue("rolledBackFrom", BsonNull.Value) is { IsBsonNull: false } from ? new(checked((uint)from.AsInt32)) : null);

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
    /// <param name="request">The activation request.</param>
    /// <param name="outcome">The outcome.</param>
    /// <param name="now">The publication time.</param>
    /// <returns>The stored representation or decoded value.</returns>
    internal static BsonDocument Receipt(MaterializationPublicationRequest request, MaterializationPublication outcome, DateTimeOffset now) => new()
    {
        { "_id", request.Receipt.Value.ToString("N") }, { "key", request.Key.Value }, { "expectedActive", checked((int)request.Active.Value) },
        { "expectedEpoch", request.Epoch.Value }, { "candidate", checked((int)request.Candidate.Value) }, { "result", (int)outcome.Result },
        { "active", checked((int)outcome.Active.Value) }, { "epoch", outcome.Epoch.Value }, { "at", now.UtcDateTime },
        { "rollbackWindow", request.RollbackWindow.Ticks }, { "restore", request.Restore }
    };

    /// <summary>
    /// Converts or prepares materialization storage values.
    /// </summary>
    /// <param name="receipt">The receipt.</param>
    /// <param name="request">The activation request.</param>
    /// <returns>The stored representation or decoded value.</returns>
    internal static MaterializationPublication Outcome(BsonDocument receipt, MaterializationPublicationRequest request) => new(
        (receipt["key"].AsString, receipt["expectedActive"].AsInt32, receipt["expectedEpoch"].AsInt64, receipt["candidate"].AsInt32, receipt.GetValue("rollbackWindow", BsonNull.Value), receipt.GetValue("restore", false).AsBoolean)
            == (request.Key.Value, checked((int)request.Active.Value), request.Epoch.Value, checked((int)request.Candidate.Value), new BsonInt64(request.RollbackWindow.Ticks), request.Restore)
            ? (MaterializationPublicationResult)receipt["result"].AsInt32 : MaterializationPublicationResult.ReceiptConflict,
        new(checked((uint)receipt["active"].AsInt32)),
        new(checked((uint)receipt["epoch"].AsInt64)));

    /// <summary>
    /// Encodes a lifecycle timestamp without losing precision.
    /// </summary>
    /// <param name="at">The timestamp.</param>
    /// <returns>The stored timestamp or null.</returns>
    internal static BsonValue Timestamp(DateTimeOffset? at) => at is { } value ? new BsonString(value.ToString("O", CultureInfo.InvariantCulture)) : BsonNull.Value;

    static DateTimeOffset? Timestamp(BsonDocument document, string field) =>
        document.TryGetValue(field, out var value) && !value.IsBsonNull ? DateTimeOffset.Parse(value.AsString, CultureInfo.InvariantCulture) : null;
}
