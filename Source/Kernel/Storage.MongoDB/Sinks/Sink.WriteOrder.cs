// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Applies mixed changesets without moving direct writes across their multi-document operations.
/// </summary>
public partial class Sink
{
    async Task<IEnumerable<FailedPartition>> WriteInOrder(
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        IChangesetConverterWithDirectWrites orderedConverter,
        OrderedWrite destination)
    {
        var plan = destination.Plan;
        var options = destination.Options;
        var context = new ChangesetWriteContext(destination.Key, destination.EventSequenceNumber, DirectWrites, RemoveChildFromAll);
        var currentPlan = plan;
        var acceptDirectWrites = !plan.ShouldSuppressRootUpdateAfterRootLevelJoin;
        await foreach (var converted in orderedConverter.ToUpdatesInOrder(changeset, context).WithCancellation(CancellationToken.None))
        {
            if (!converted.hasChanges || !acceptDirectWrites)
            {
                continue;
            }

            var write = ScopeToExistingArrays(new UpdateWrite(destination.Key, destination.EventSequenceNumber, converted, currentPlan.Filter, currentPlan.IsUpsert));
            if (!currentPlan.UsesJoinTargetsOnlyFilter)
            {
                await UnsetNullParents(write, options);
            }

            // Never buffer a run: the converter resumes with a join or removal only after this write completes.
            var result = await WriteOrderedUpdate(write);
            CacheImmediateWrite(write, changeset, options);
            acceptDirectWrites = !IsGuardedWrite(options.Mode, plan, destination.EventSequenceNumber) ||
                !result.IsAcknowledged || result.MatchedCount > 0 || result.UpsertedId is not null;

            // The first direct write advances the watermark. Later runs belong to that same accepted event.
            currentPlan = PlanWrite(destination.Key, changeset) with { IsUpsert = false };
        }
        return [];
    }

    Task<UpdateResult> WriteOrderedUpdate(UpdateWrite write) => writes.UpdateOne(
        Collection,
        write.Filter,
        write.Converted.UpdateDefinition,
        new UpdateOptions { IsUpsert = write.IsUpsert, ArrayFilters = write.Converted.ArrayFilters });

    sealed record OrderedWrite(Key Key, EventSequenceNumber EventSequenceNumber, WritePlan Plan, SinkWriteOptions Options);
}
