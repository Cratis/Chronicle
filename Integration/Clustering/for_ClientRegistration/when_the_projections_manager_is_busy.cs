// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using context = Cratis.Chronicle.Integration.Clustering.for_ClientRegistration.when_the_projections_manager_is_busy.context;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientRegistration;

/// <summary>
/// Pins what a client experiences when it registers projections while the grain that owns them is saturated: the
/// registration call is held for as long as the grain is, and still succeeds once the grain gets to it.
/// </summary>
/// <param name="_context">The <see cref="context"/> for the specification.</param>
[Collection(BusyKernelCollection.Name)]
public class when_the_projections_manager_is_busy(context _context) : IClassFixture<context>
{
    public static readonly TimeSpan HoldTime = TimeSpan.FromSeconds(3);

    public class context(BusyKernelClusteringFixture fixture) : IAsyncLifetime
    {
        public TimeSpan Elapsed { get; private set; }
        public Exception? Failure { get; private set; }
        public int Reached { get; private set; }

        public async ValueTask InitializeAsync()
        {
            var eventStore = fixture.ClientEventStore;
            await eventStore.RegisterAll();
            var services = ((IChronicleServicesAccessor)eventStore.Connection).Services;
            var definitions = await services.Projections.GetAllDefinitions(new() { EventStore = eventStore.Name.Value });
            var request = new RegisterRequest
            {
                EventStore = eventStore.Name.Value,
                Owner = ProjectionOwner.Client,
                Projections = [.. definitions],
                Registrant = "busy-kernel-specs",
                FullSet = false
            };

            fixture.Busy.Hold = HoldTime;
            var reachedBefore = fixture.Busy.Reached;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await services.Projections.Register(request);
            }
            catch (Exception exception)
            {
                Failure = exception;
            }
            finally
            {
                Elapsed = stopwatch.Elapsed;
                fixture.Busy.Hold = TimeSpan.Zero;
            }

            Reached = fixture.Busy.Reached - reachedBefore;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_succeed() => _context.Failure.ShouldBeNull();
    [Fact] void should_have_reached_the_projections_manager() => _context.Reached.ShouldBeGreaterThan(0);
    [Fact] void should_have_been_held_for_as_long_as_the_grain_was_busy() => _context.Elapsed.ShouldBeGreaterThanOrEqual(HoldTime - TimeSpan.FromMilliseconds(250));
}
