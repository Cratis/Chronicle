// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_observing.and_an_event_is_appended.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_observing;

[Collection(ChronicleCollection.Name)]
public class and_an_event_is_appended(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public EventSourceId WorkItemId = EventSourceId.New();
        public IEnumerable<ObservedWorkItem> Initial;
        public IEnumerable<ObservedWorkItem> Updated;
        Exception? _error;

        public override IEnumerable<Type> EventTypes => [typeof(WorkItemRecorded)];

        async Task Because()
        {
            var handler = await EventStore.Projections.Register<ObservedWorkItem>(projection => projection
                .ContainerName("observed-work-items")
                .From<WorkItemRecorded>(from => from.Set(row => row.WorkItemId).ToEventSourceId()));
            await handler.WaitTillActive();
            using var emitted = new SemaphoreSlim(0);
            var observation = EventStore.ReadModels.Observe<ObservedWorkItem>();
            using var observationLifetime = (IDisposable)observation;
            using var subscription = observation.Subscribe(
                set =>
                {
                    Updated = set;
                    emitted.Release();
                },
                error =>
                {
                    _error = error;
                    emitted.Release();
                });
            await WaitForEmission(emitted);
            Initial = Updated;

            await EventStore.EventLog.Append(WorkItemId, new WorkItemRecorded(42));
            await WaitForEmission(emitted);
        }

        async Task WaitForEmission(SemaphoreSlim emitted)
        {
            (await emitted.WaitAsync(TimeSpan.FromSeconds(30))).ShouldBeTrue();
            _error.ShouldBeNull();
        }
    }

    [Fact] void should_emit_the_initial_empty_collection() => Context.Initial.ShouldBeEmpty();
    [Fact] void should_emit_the_appended_work_item() => Context.Updated.ShouldContainOnly(new ObservedWorkItem(Guid.Parse(Context.WorkItemId.Value), 42));

    public record ObservedWorkItem([Key] Guid WorkItemId, int Number);

    /// <summary>
    /// The spec's own event type: a runtime projection over a shared event type outlives the fixture reset and can starve a later spec's projection (#4815).
    /// </summary>
    /// <param name="Number">The work item number.</param>
    [EventType("ef9847e4-02ad-4dec-b574-5c4cbf3e5f0f")]
    public record WorkItemRecorded(int Number);
}
