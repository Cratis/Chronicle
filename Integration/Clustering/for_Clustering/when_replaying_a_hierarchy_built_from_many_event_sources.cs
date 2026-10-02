// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Clustering.for_Clustering.Hierarchy;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Projections;
using context = Cratis.Chronicle.Integration.Clustering.for_Clustering.when_replaying_a_hierarchy_built_from_many_event_sources.context;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

/// <summary>
/// A read model whose children arrive on other event sources - a module, its features, their slices and the
/// slices' events each on their own stream - is rebuilt by a replay that writes child changes into arrays of a
/// document an earlier event created. Every document has to come out of the replay whole, across a cluster.
/// </summary>
/// <param name="_context">The <see cref="context"/> for the specification.</param>
[Collection(ChronicleCollection.Name)]
public class when_replaying_a_hierarchy_built_from_many_event_sources(context _context)
    : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(180);

        public IReadOnlyList<HierarchyModule?> AfterCatchUp { get; private set; } = [];
        public IReadOnlyList<HierarchyModule?> AfterReplay { get; private set; } = [];

        public async Task InitializeAsync()
        {
            var eventStore = fixture.ClientEventStore;
            var projectionId = eventStore.Projections.GetProjectionIdForModel<HierarchyModule>();
            var handler = eventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillActive(_timeout);

            var (modules, last) = await HierarchyEvents.Append(eventStore);

            await handler.WaitTillReachesEventSequenceNumber(last, _timeout);
            AfterCatchUp = await ReadAll(eventStore, modules);

            var replayJobId = await eventStore.Projections.Replay(projectionId);
            await eventStore.Jobs.WaitTillJobCompletesOrIsDeleted(replayJobId, _timeout);
            await handler.WaitTillReachesEventSequenceNumber(last, _timeout);
            AfterReplay = await ReadAll(eventStore, modules);
        }

        public Task DisposeAsync() => Task.CompletedTask;

        static async Task<IReadOnlyList<HierarchyModule?>> ReadAll(IEventStore eventStore, IEnumerable<Guid> modules)
        {
            var result = new List<HierarchyModule?>();
            foreach (var module in modules)
            {
                result.Add(await eventStore.ReadModels.GetInstanceById<HierarchyModule>(module.ToString()));
            }

            return result;
        }
    }

    static int SliceCount(IEnumerable<HierarchyModule?> modules) =>
        modules.Sum(module => module?.Features.Sum(feature => feature.Slices.Count()) ?? 0);

    static int EventCount(IEnumerable<HierarchyModule?> modules) =>
        modules.Sum(module => module?.Features.Sum(feature => feature.Slices.Sum(slice => slice.Events.Count())) ?? 0);

    [Fact] void should_have_every_module_after_catching_up() => _context.AfterCatchUp.Count(_ => _ is not null).ShouldEqual(HierarchyEvents.Modules);
    [Fact] void should_have_every_slice_after_catching_up() => SliceCount(_context.AfterCatchUp).ShouldEqual(HierarchyEvents.Slices);
    [Fact] void should_have_every_module_after_replaying() => _context.AfterReplay.Count(_ => _ is not null).ShouldEqual(HierarchyEvents.Modules);
    [Fact] void should_have_every_feature_after_replaying() => _context.AfterReplay.Sum(_ => _?.Features.Count() ?? 0).ShouldEqual(HierarchyEvents.Features);
    [Fact] void should_have_every_slice_after_replaying() => SliceCount(_context.AfterReplay).ShouldEqual(HierarchyEvents.Slices);
    [Fact] void should_have_every_slice_event_after_replaying() => EventCount(_context.AfterReplay).ShouldEqual(HierarchyEvents.SliceEvents);
}
