// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <content>
/// Applies mixed changesets without moving direct writes across their multi-document operations.
/// </content>
public partial class Sink
{
    static bool CanContinueOrderedWrite(UpdateResult? result, bool guarded) =>
        !guarded || (result?.IsAcknowledged == true && (result.MatchedCount > 0 || result.UpsertedId is not null));

    async Task<IEnumerable<FailedPartition>> WriteInOrder(
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        IChangesetConverterWithDirectWrites orderedConverter,
        OrderedWrite destination)
    {
        var plan = destination.Plan;
        var guarded = IsGuardedWrite(destination.Options.Mode, plan, destination.EventSequenceNumber);

        // A guarded run must return its own outcome before any later join or direct run can proceed.
        // Immediate options also keep its null-parent repairs out of the bulk queue.
        var options = guarded
            ? destination.Options with { DrainedBatches = destination.Options.DrainedBatches ?? [] }
            : destination.Options;
        var context = new ChangesetWriteContext(destination.Key, destination.EventSequenceNumber, DirectWrites, RemoveChildFromAll);
        var currentPlan = plan;
        var failures = new List<FailedPartition>();
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

            // Retain the shared path's bulk cache and watermark bookkeeping for buffered and immediate runs.
            var outcome = await WriteUpdate(write, changeset, options);
            failures.AddRange(outcome.Failures);
            if (!CanContinueOrderedWrite(outcome.Result, guarded))
            {
                // Resuming the converter would execute the next join or removal before yielding again.
                break;
            }

            // The first direct write advances the watermark. Later runs belong to that same accepted event.
            currentPlan = PlanWrite(destination.Key, changeset) with { IsUpsert = false };
        }
        return failures;
    }

    sealed record OrderedWrite(Key Key, EventSequenceNumber EventSequenceNumber, WritePlan Plan, SinkWriteOptions Options);
}
