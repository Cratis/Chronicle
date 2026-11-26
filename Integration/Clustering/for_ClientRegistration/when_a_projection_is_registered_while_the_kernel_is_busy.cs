// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Storage;
using ConceptsEventStoreName = KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName;
using context = Cratis.Chronicle.Integration.Clustering.for_ClientRegistration.when_a_projection_is_registered_while_the_kernel_is_busy.context;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientRegistration;

/// <summary>
/// A projection the kernel has accepted but not applied delivers nothing, and takes nothing away: once the kernel
/// applies it, it receives every event from the start, in order.
/// </summary>
/// <param name="_context">The <see cref="context"/> for the specification.</param>
[Collection(BusyKernelCollection.Name)]
public class when_a_projection_is_registered_while_the_kernel_is_busy(context _context) : IClassFixture<context>
{
    public const int Events = 6;
    const string ProjectionIdentifier = "held-projection";

    public class context(BusyKernelClusteringFixture fixture) : IAsyncLifetime
    {
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(90);

        public bool WasDefinedWhileUnapplied { get; private set; }
        public bool RegistrationWasStillWaiting { get; private set; }
        public int NumberAfterwards { get; private set; }
        public Exception? LastReadFailure { get; private set; }

        public async ValueTask InitializeAsync()
        {
            var eventStore = fixture.ClientEventStore;
            await eventStore.RegisterAll();
            var store = fixture.SiloServices.GetRequiredService<IStorage>().GetEventStore((ConceptsEventStoreName)Constants.EventStore);

            fixture.Busy.Hold = TimeSpan.FromSeconds(6);
            var registering = eventStore.Projections.Register<HeldProjectionReadModel>(
                builder => builder.From<HeldProjectionEvent>(events => events.Set(model => model.Number).To(@event => @event.Number)),
                ProjectionIdentifier);

            // The kernel accepted the registration and is holding the grain that applies it. Events appended now are
            // for a projection that does not exist yet.
            for (var number = 1; number <= Events; number++)
            {
                await eventStore.EventLog.Append("held-projection-source", new HeldProjectionEvent(number));
            }

            WasDefinedWhileUnapplied = (await store.Projections.GetAll()).Any(_ => _.Identifier.Value == ProjectionIdentifier);
            RegistrationWasStillWaiting = !registering.IsCompleted;

            await registering.WaitAsync(_timeout);
            fixture.Busy.Hold = TimeSpan.Zero;

            var deadline = DateTime.UtcNow + _timeout;
            while (DateTime.UtcNow < deadline && NumberAfterwards != Events)
            {
                // Task.Delay: poll interval while waiting for the projected count, bounded by the deadline.
                await Task.Delay(100);
                try
                {
                    NumberAfterwards = (await eventStore.ReadModels.GetInstanceById<HeldProjectionReadModel>("held-projection-source")).Number;
                }
                catch (Exception exception)
                {
                    // The read model has no instance until the first event is projected.
                    LastReadFailure = exception;
                }
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_not_have_applied_the_projection_when_the_events_were_appended() => _context.WasDefinedWhileUnapplied.ShouldBeFalse();
    [Fact] void should_still_be_waiting_for_the_kernel_to_apply_it() => _context.RegistrationWasStillWaiting.ShouldBeTrue();
    [Fact] void should_project_every_event_in_order_once_applied() => _context.NumberAfterwards.ShouldEqual(Events);
}
