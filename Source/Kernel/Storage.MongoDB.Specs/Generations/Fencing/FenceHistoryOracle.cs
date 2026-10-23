// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Judges the persisted history a fault scenario left behind, never in-memory state.
/// </summary>
/// <remarks>
/// Every batch writes a fixed number of history documents stamped with its label and the epoch it wrote under. The history is
/// consistent when no revoked batch left a document, every batch committed before revocation left all of its documents, and no
/// document stamped with the revoked epoch belongs to anything but a batch committed before revocation.
/// </remarks>
public static class FenceHistoryOracle
{
    public const string Label = "label";
    public const string Epoch = "epoch";

    public static BsonDocument Document(string label, int index, long epoch) => new()
    {
        { "_id", $"{label}-{index}" },
        { Label, label },
        { Epoch, epoch }
    };

    public static async Task<IReadOnlyList<string>> Check(
        IMongoCollection<BsonDocument> history,
        long revokedEpoch,
        int documentsPerBatch,
        IEnumerable<string> committedBeforeRevocation,
        IEnumerable<string> revoked)
    {
        var documents = await history.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var byLabel = documents.GroupBy(_ => _[Label].AsString).ToDictionary(_ => _.Key, _ => _.Count());
        var committed = committedBeforeRevocation.ToHashSet();
        var violations = new List<string>();

        violations.AddRange(revoked.Where(byLabel.ContainsKey).Select(label => $"revoked batch '{label}' committed {byLabel[label]} document(s)"));
        violations.AddRange(committed.Where(label => byLabel.GetValueOrDefault(label) != documentsPerBatch)
            .Select(label => $"batch '{label}' committed before revocation left {byLabel.GetValueOrDefault(label)} of {documentsPerBatch} document(s)"));
        violations.AddRange(documents
            .Where(_ => _[Epoch].ToInt64() <= revokedEpoch && !committed.Contains(_[Label].AsString))
            .Select(_ => $"document '{_["_id"]}' carries revoked epoch {_[Epoch]} but was not committed before revocation"));
        return violations;
    }
}
