// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Orleans.Jobs;

using KernelEventSequenceId = Cratis.Chronicle.Concepts.EventSequences.EventSequenceId;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_revising.given;

public class a_constrained_event_log(ChronicleFixture fixture) : Specification(fixture)
{
    IEventStoreStorage Store => Services.GetRequiredService<IStorage>().GetEventStore(EventStore.Name.Value);
    IEventStoreNamespaceStorage Namespace => Store.GetNamespace(EventStore.Namespace.Value);

    public async Task<TResult> AppendWhenIndexIsCurrent<TResult>(Func<Task<TResult>> append)
        where TResult : IAppendResult
    {
        TResult result = default!;
        await RetryWaits.Until(async () =>
        {
            result = await append();
            return !result.Errors.Any(error => error.Value == "ConstraintIndexUnavailable");
        });

        return result;
    }

    public async Task<string[]> Claims(string constraintName, params EventSourceId[] sources)
    {
        var definition = (await Store.Constraints.GetDefinitions())
            .OfType<Concepts.Events.Constraints.UniqueConstraintDefinition>()
            .Single(candidate => candidate.Name.Value == constraintName);
        var storage = Namespace.GetUniqueConstraintsStorage(KernelEventSequenceId.Log);
        var claims = new List<string>();
        foreach (var source in sources)
        {
            foreach (var value in await storage.GetFor(source.Value, definition, string.Empty))
            {
                var (_, position) = await storage.IsAllowed(source.Value, definition, value, string.Empty);
                claims.Add($"{source.Value}:{value.Value}@{position.Value}");
            }
        }

        return claims.Order(StringComparer.Ordinal).ToArray();
    }

    public async Task<string[]> Closures(string owner)
    {
        var closures = await Namespace.GetClosedStreamsConstraints(KernelEventSequenceId.Log).GetForOwner(new(owner));

        return closures.Select(closure => $"source={closure.Scope.EventSourceId?.Value};sourceType={closure.Scope.EventSourceType?.Value};streamType={closure.Scope.EventStreamType?.Value};esid={closure.Scope.EventStreamId?.Value}@{closure.SequenceNumber.Value}")
            .Order(StringComparer.Ordinal).ToArray();
    }

    public async Task Reindex(string constraintName)
    {
        var jobs = Services.GetRequiredService<IGrainFactory>().GetJobsManager(EventStore.Name.Value, EventStore.Namespace.Value);
        var started = await jobs.Start<IReindexConstraints, ReindexConstraintsRequest>(new(KernelEventSequenceId.Log, [new(constraintName, true, [])]));
        started.IsSuccess.ShouldBeTrue();
        await EventStore.Jobs.WaitForThereToBeNoJobs(TimeSpan.FromSeconds(30), Jobs.JobStatus.CompletedSuccessfully);
    }
}
