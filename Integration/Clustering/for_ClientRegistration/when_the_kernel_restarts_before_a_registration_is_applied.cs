// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Registrations;
using ConceptsEventStoreName = KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName;
using context = Cratis.Chronicle.Integration.Clustering.for_ClientRegistration.when_the_kernel_restarts_before_a_registration_is_applied.context;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientRegistration;

/// <summary>
/// A registration the kernel accepted survives the kernel stopping before it was applied: after the restart the intent
/// is still there and gets applied.
/// </summary>
/// <param name="_context">The <see cref="context"/> for the specification.</param>
[Collection(BusyKernelRestartCollection.Name)]
public class when_the_kernel_restarts_before_a_registration_is_applied(context _context) : IClassFixture<context>
{
    const string Registrant = "restart-specs";

    public class context(BusyKernelClusteringFixture fixture) : IAsyncLifetime
    {
        public RegistrationIntentState StateBeforeRestart { get; private set; }
        public RegistrationIntentState StateAfterRestart { get; private set; }

        public async ValueTask InitializeAsync()
        {
            var eventStore = fixture.ClientEventStore;
            await eventStore.RegisterAll();
            var services = ((IChronicleServicesAccessor)eventStore.Connection).Services;
            var definitions = await services.Projections.GetAllDefinitions(new() { EventStore = eventStore.Name.Value });

            // The grain that applies it never gets to it before the kernel goes down.
            fixture.Busy.Hold = TimeSpan.FromMinutes(10);
            await services.Projections.Register(new RegisterRequest
            {
                EventStore = eventStore.Name.Value,
                Owner = ProjectionOwner.Client,
                Projections = [.. definitions],
                Registrant = Registrant,
                FullSet = false
            });
            StateBeforeRestart = (await Intents()).Single(_ => _.Registrant == Registrant).State;

            await fixture.CrashAndRestart(() => fixture.Busy.Hold = TimeSpan.Zero);

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(90);
            while (DateTime.UtcNow < deadline)
            {
                StateAfterRestart = (await Intents()).Single(_ => _.Registrant == Registrant).State;
                if (StateAfterRestart == RegistrationIntentState.Applied)
                {
                    break;
                }

                // Task.Delay: poll interval while waiting for the restarted kernel to apply the registration, bounded by the deadline.
                await Task.Delay(200);
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        async Task<IReadOnlyList<RegistrationIntent>> Intents() =>
            await fixture.SiloServices.GetRequiredService<IStorage>().GetEventStore((ConceptsEventStoreName)Constants.EventStore).RegistrationIntents.GetAll();
    }

    [Fact] void should_have_kept_the_registration_pending_before_the_restart() => _context.StateBeforeRestart.ShouldEqual(RegistrationIntentState.Pending);
    [Fact] void should_apply_the_registration_after_the_restart() => _context.StateAfterRestart.ShouldEqual(RegistrationIntentState.Applied);
}
