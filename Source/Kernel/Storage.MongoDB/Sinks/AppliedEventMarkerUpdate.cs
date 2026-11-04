// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using static Cratis.Chronicle.Storage.MongoDB.Sinks.AppliedEventMarkerExpressions;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Builds the single guarded pipeline update that writes the data and the applied event markers of a changeset together.
/// </summary>
/// <remarks>
/// Every predicate is evaluated against the document as it was before the update, and the update is one document, so
/// data and marker either both move or neither does. A write of the key document applies when its own slot does not record the
/// event or a later one. A resolved join applies when the document references the join key it was resolved for, either
/// already or because this very write sets that reference, and when its slot does not record a later event of the stream. The
/// first condition keeps a stale snapshot from overwriting data after the reference moved on; the second lets a reference that
/// returns to an earlier target be filled again. Overlapping writes to a field are folded in change order.
/// </remarks>
internal static class AppliedEventMarkerUpdate
{
    /// <summary>
    /// Builds the update.
    /// </summary>
    /// <param name="groups">The groups of the changeset in change order; at least one group is the write of the key document.</param>
    /// <returns>The pipeline update, registered with <see cref="AppliedEventMarkerUpdates"/>.</returns>
    /// <exception cref="AppliedEventMarkerUpdateNotSupported">An operation cannot be expressed in a pipeline.</exception>
    public static UpdateDefinition<BsonDocument> Build(IReadOnlyList<AppliedEventMarkerGroup> groups)
    {
        var operations = groups.Select(Parse).ToArray();
        var fields = new Dictionary<string, List<Write>>();
        var watermarks = new BsonDocument();

        for (var index = 0; index < groups.Count; index++)
        {
            var group = groups[index];
            var predicate = PredicateOf(group, groups, operations);
            AddSlot(fields, group, predicate);
            foreach (var operation in operations[index].Where(_ => _.Path != WellKnownProperties.LastHandledEventSequenceNumber))
            {
                var segments = operation.Path.Split('.');
                Add(fields, segments[0], new Write(segments[1..], operation.Value, predicate));
            }
        }

        foreach (var watermark in operations.SelectMany(_ => _).Where(_ => _.Path == WellKnownProperties.LastHandledEventSequenceNumber))
        {
            var field = $"${WellKnownProperties.LastHandledEventSequenceNumber}";
            watermarks[WellKnownProperties.LastHandledEventSequenceNumber] = new BsonDocument("$max", new BsonArray { field, Literal(watermark.Value!) });
        }

        var stage = new BsonDocument();
        foreach (var (field, writes) in fields)
        {
            stage[field] = writes.Aggregate((BsonValue)$"${field}", (current, write) => When(current, write.Predicate, reference => Apply(reference, write)));
        }

        stage.Merge(watermarks);
        var pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>([new BsonDocument("$set", stage)]);
        return AppliedEventMarkerUpdates.Mark(new PipelineUpdateDefinition<BsonDocument>(pipeline));
    }

    static BsonValue Apply(BsonValue reference, Write write)
    {
        if (write.Below.Count == 0)
        {
            return Written(write.Value);
        }

        return SetBelow(reference, write.Below, write.Value);
    }

    static void Add(Dictionary<string, List<Write>> fields, string field, Write write)
    {
        if (!fields.TryGetValue(field, out var writes))
        {
            fields[field] = writes = [];
        }

        writes.Add(write);
    }

    static void AddSlot(Dictionary<string, List<Write>> fields, AppliedEventMarkerGroup group, BsonValue predicate) =>
        Add(fields, group.Slot, new([], new BsonDocument { { "s", group.Stream }, { "n", group.SequenceNumber } }, predicate));

    static BsonDocument PredicateOf(AppliedEventMarkerGroup group, IReadOnlyList<AppliedEventMarkerGroup> groups, Operation[][] operations)
    {
        var notApplied = NotApplied(group.Slot, group.Stream, group.SequenceNumber);
        if (!group.IsResolvedJoin)
        {
            return notApplied;
        }

        return ReferencedAndNotApplied(ReferenceAfterOwnWrites(group.OnProperty!, groups, operations), group.OnProperty!, group.JoinKey!, notApplied);
    }

    static BsonValue ReferenceAfterOwnWrites(string onProperty, IReadOnlyList<AppliedEventMarkerGroup> groups, Operation[][] operations)
    {
        BsonValue reference = $"${onProperty}";
        for (var index = 0; index < groups.Count; index++)
        {
            if (groups[index].IsResolvedJoin)
            {
                continue;
            }

            var own = operations[index].LastOrDefault(_ => _.Path == onProperty);
            if (own is null)
            {
                continue;
            }

            var guard = NotApplied(groups[index].Slot, groups[index].Stream, groups[index].SequenceNumber);
            reference = new BsonDocument("$cond", new BsonArray { guard, Literal(own.Value ?? BsonNull.Value), reference });
        }

        return reference;
    }

    static Operation[] Parse(AppliedEventMarkerGroup group)
    {
        if (group.Update is null)
        {
            return [];
        }

        var rendered = group.Update.Render(new(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)) as BsonDocument
            ?? throw new AppliedEventMarkerUpdateNotSupported("(pipeline update)");
        var operations = new List<Operation>();
        foreach (var element in rendered.Elements)
        {
            foreach (var entry in element.Value.AsBsonDocument.Elements)
            {
                RequireSupportedPath(entry.Name);
                operations.Add(element.Name switch
                {
                    "$set" => new Operation(entry.Name, entry.Value),
                    "$unset" => new Operation(entry.Name, null),
                    "$max" when entry.Name == WellKnownProperties.LastHandledEventSequenceNumber => new Operation(entry.Name, entry.Value),
                    _ => throw new AppliedEventMarkerUpdateNotSupported(element.Name)
                });
            }
        }

        return [.. operations];
    }

    static void RequireSupportedPath(string path)
    {
        if (path.Split('.').Any(segment => segment.Length == 0 || segment[0] == '$'))
        {
            throw new AppliedEventMarkerUpdateNotSupported(path);
        }
    }
    sealed record Write(IReadOnlyList<string> Below, BsonValue? Value, BsonValue Predicate);

    sealed record Operation(string Path, BsonValue? Value);
}
