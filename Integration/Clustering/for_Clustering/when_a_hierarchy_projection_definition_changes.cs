// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Clustering.for_Clustering.Hierarchy;
using Cratis.Chronicle.Projections;
using context = Cratis.Chronicle.Integration.Clustering.for_Clustering.when_a_hierarchy_projection_definition_changes.context;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

/// <summary>
/// A new build of an application that changes a projection's definition makes Chronicle replay it on its own.
/// A deployment brings up several instances of that build, each registering the new definition. The replay
/// they cause must rebuild a read model whose children arrive on other event sources completely.
/// </summary>
/// <param name="_context">The <see cref="context"/> for the specification.</param>
[Collection(ChronicleCollection.Name)]
public class when_a_hierarchy_projection_definition_changes(context _context)
    : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(180);

        public IReadOnlyList<EvolvingModule?> AfterCatchUp { get; private set; } = [];
        public IReadOnlyList<EvolvingModule?> AfterReplay { get; private set; } = [];

        public async Task InitializeAsync()
        {
            var eventStore = fixture.ClientEventStore;
            var handler = eventStore.Projections.GetHandlerFor<EvolvingHierarchyProjection>();
            await handler.WaitTillActive(_timeout);

            var (modules, last) = await HierarchyEvents.Append(eventStore);
            await handler.WaitTillReachesEventSequenceNumber(last, _timeout);
            AfterCatchUp = await ReadAll(eventStore, modules);

            EvolvingHierarchyProjection.Evolved = true;
            await Task.WhenAll(Reregister(fixture.ClientEventStore), Reregister(fixture.SecondClientEventStore));

            AfterReplay = await WaitForReplay(eventStore, modules);
            await handler.WaitTillReachesEventSequenceNumber(last, _timeout);
            AfterReplay = await ReadAll(eventStore, modules);
        }

        public Task DisposeAsync()
        {
            EvolvingHierarchyProjection.Evolved = false;
            return Task.CompletedTask;
        }

        static async Task Reregister(IEventStore eventStore)
        {
            await eventStore.Projections.Discover();
            await eventStore.Projections.Register();
        }

        static async Task<IReadOnlyList<EvolvingModule?>> ReadAll(IEventStore eventStore, IEnumerable<Guid> modules)
        {
            var result = new List<EvolvingModule?>();
            foreach (var module in modules)
            {
                result.Add(await eventStore.ReadModels.GetInstanceById<EvolvingModule>(module.ToString()));
            }

            return result;
        }

        async Task<IReadOnlyList<EvolvingModule?>> WaitForReplay(IEventStore eventStore, Guid[] modules)
        {
            // Only the new definition sets the label, so every module carrying one has been rebuilt by the replay.
            using var cancellationTokenSource = new CancellationTokenSource(_timeout);
            IReadOnlyList<EvolvingModule?> result = [];
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                result = await ReadAll(eventStore, modules);
                if (result.All(module => !string.IsNullOrEmpty(module?.Label)))
                {
                    return result;
                }

                await Task.Delay(250, cancellationTokenSource.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }

            return result;
        }
    }

    static int FeatureCount(IEnumerable<EvolvingModule?> modules) =>
        modules.Sum(module => module?.Features.Count() ?? 0);

    static int SliceCount(IEnumerable<EvolvingModule?> modules) =>
        modules.Sum(module => module?.Features.Sum(feature => feature.Slices.Count()) ?? 0);

    static int EventCount(IEnumerable<EvolvingModule?> modules) =>
        modules.Sum(module => module?.Features.Sum(feature => feature.Slices.Sum(slice => slice.Events.Count())) ?? 0);

    [Fact] void should_have_every_slice_before_the_definition_changes() => SliceCount(_context.AfterCatchUp).ShouldEqual(HierarchyEvents.Slices);
    [Fact] void should_rebuild_every_module_with_the_new_definition() => _context.AfterReplay.Count(_ => !string.IsNullOrEmpty(_?.Label)).ShouldEqual(HierarchyEvents.Modules);
    [Fact] void should_have_every_feature_after_replaying() => FeatureCount(_context.AfterReplay).ShouldEqual(HierarchyEvents.Features);
    [Fact] void should_have_every_slice_after_replaying() => SliceCount(_context.AfterReplay).ShouldEqual(HierarchyEvents.Slices);
    [Fact] void should_have_every_slice_event_after_replaying() => EventCount(_context.AfterReplay).ShouldEqual(HierarchyEvents.SliceEvents);
}
