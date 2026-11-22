// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using context = Cratis.Chronicle.Integration.Clustering.for_ClientRegistration.when_the_projections_manager_is_busy.context;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientRegistration;

/// <summary>
/// A client that registers projections while the grain that owns them is saturated is answered at once; the kernel
/// applies the registration afterwards, when the grain gets to it.
/// </summary>
/// <param name="_context">The <see cref="context"/> for the specification.</param>
[Collection(BusyKernelCollection.Name)]
public class when_the_projections_manager_is_busy(context _context) : IClassFixture<context>
{
    public static readonly TimeSpan HoldTime = TimeSpan.FromSeconds(5);

    public class context(BusyKernelClusteringFixture fixture) : IAsyncLifetime
    {
        public TimeSpan Elapsed { get; private set; }
        public Exception? Failure { get; private set; }
        public RegistrationState StateWhenAnswered { get; private set; }
        public RegistrationState StateAfterwards { get; private set; }
        public int ReachedAfterwards { get; private set; }

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
            }

            StateWhenAnswered = (await services.Projections.GetRegistrationStatus(new() { EventStore = eventStore.Name.Value, Registrant = "busy-kernel-specs" })).State;

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
            while (DateTime.UtcNow < deadline)
            {
                StateAfterwards = (await services.Projections.GetRegistrationStatus(new() { EventStore = eventStore.Name.Value, Registrant = "busy-kernel-specs" })).State;
                if (StateAfterwards != RegistrationState.Pending)
                {
                    break;
                }

                await Task.Delay(100);
            }

            fixture.Busy.Hold = TimeSpan.Zero;
            ReachedAfterwards = fixture.Busy.Reached - reachedBefore;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_succeed() => _context.Failure.ShouldBeNull();
    [Fact] void should_answer_in_under_a_second() => _context.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    [Fact] void should_report_pending_when_it_answers() => _context.StateWhenAnswered.ShouldEqual(RegistrationState.Pending);
    [Fact] void should_apply_it_afterwards() => _context.StateAfterwards.ShouldEqual(RegistrationState.Applied);
    [Fact] void should_have_asked_the_projections_manager_after_it_answered() => _context.ReachedAfterwards.ShouldBeGreaterThan(0);
}
