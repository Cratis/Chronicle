// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Clustering.for_RolePlacement;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Projections;
using context = Cratis.Chronicle.Integration.Clustering.for_JobsRolePlacement.when_replaying_a_projection_with_a_dedicated_jobs_silo.context;

namespace Cratis.Chronicle.Integration.Clustering.for_JobsRolePlacement;

[Collection(JobsRoleCollection.Name)]
public class when_replaying_a_projection_with_a_dedicated_jobs_silo(context _context) : IClassFixture<context>
{
    public class context(JobsRoleFixture fixture) : IAsyncLifetime
    {
        const int Partitions = 8;
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(120);

        public SiloAddress JobsSilo => fixture.EventSequencesSiloAddress;
        public JobStepPlacementSignal Placement => fixture.Placement;
        public IReadOnlyList<JobsRoleReadModel> Replayed { get; private set; } = [];

        public async Task InitializeAsync()
        {
            // Wait for role metadata on both callers, not just membership convergence.
            await PlacementScenario.ActivateFromBothSilos(fixture);
            var store = fixture.ClientEventStore;
            var handler = store.Projections.GetHandlerFor<JobsRoleProjection>();
            await handler.WaitTillActive(_timeout);
            var sources = Enumerable.Range(0, Partitions).Select(_ => Guid.NewGuid().ToString()).ToArray();
            foreach (var source in sources)
            {
                var append = await store.EventLog.Append(source, new JobsRoleEvent(42));
                await handler.WaitTillReachesEventSequenceNumber(append.SequenceNumber, _timeout);
            }

            Placement.Steps.Clear();
            Placement.Capturing = true;
            try
            {
                var replay = await store.Projections.Replay<JobsRoleProjection>();
                await store.Jobs.WaitTillJobCompletesOrIsDeleted(replay, _timeout);
                Replayed = await Task.WhenAll(sources.Select(source => store.ReadModels.GetInstanceById<JobsRoleReadModel>(source)));
            }
            finally
            {
                Placement.Capturing = false;
            }
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    [Fact] void should_run_steps() => _context.Placement.Steps.Count.ShouldBeGreaterThan(0);
    [Fact] void should_run_every_step_on_the_jobs_silo() => _context.Placement.Steps.Values.ShouldEachConformTo(address => address == _context.JobsSilo);
    [Fact] void should_complete_the_replay() => _context.Replayed.ShouldEachConformTo(model => model.Number == 42);
}
