// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using context = Cratis.Chronicle.Integration.Clustering.for_ClientRegistration.when_a_registration_cannot_be_applied.context;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientRegistration;

/// <summary>
/// A registration the kernel accepted and then cannot apply is recorded as failed, and reported by the status call,
/// without holding back the registration of anyone else.
/// </summary>
/// <param name="_context">The <see cref="context"/> for the specification.</param>
[Collection(BusyKernelCollection.Name)]
public class when_a_registration_cannot_be_applied(context _context) : IClassFixture<context>
{
    public class context(BusyKernelClusteringFixture fixture) : IAsyncLifetime
    {
        public Exception? AcceptFailure { get; private set; }
        public RegistrationStatus Failing { get; private set; } = new();
        public RegistrationStatus Healthy { get; private set; } = new();

        public async ValueTask InitializeAsync()
        {
            var eventStore = fixture.ClientEventStore;
            await eventStore.RegisterAll();
            var services = ((IChronicleServicesAccessor)eventStore.Connection).Services;
            var existing = await services.Projections.GetAllDefinitions(new() { EventStore = eventStore.Name.Value });

            try
            {
                await services.Projections.Register(new RegisterRequest
                {
                    EventStore = eventStore.Name.Value,
                    Owner = ProjectionOwner.Client,
                    Registrant = "failing-specs",
                    Projections =
                    [
                        new ProjectionDefinition
                        {
                            EventSequenceId = "event-log",
                            Identifier = "projection-without-a-read-model",
                            ReadModel = "this-read-model-was-never-registered",
                            InitialModelState = "{}",
                            All = new FromEveryDefinition()
                        }
                    ]
                });
                await services.Projections.Register(new RegisterRequest
                {
                    EventStore = eventStore.Name.Value,
                    Owner = ProjectionOwner.Client,
                    Registrant = "healthy-specs",
                    Projections = [.. existing]
                });
            }
            catch (Exception exception)
            {
                AcceptFailure = exception;
                return;
            }

            Failing = await WaitUntilSettled(services, eventStore.Name.Value, "failing-specs");
            Healthy = await WaitUntilSettled(services, eventStore.Name.Value, "healthy-specs");
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        static async Task<RegistrationStatus> WaitUntilSettled(IServices services, string eventStore, string registrant)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(90);
            var status = new RegistrationStatus();
            while (DateTime.UtcNow < deadline)
            {
                status = await services.Projections.GetRegistrationStatus(new() { EventStore = eventStore, Registrant = registrant });
                if (status.State != RegistrationState.Pending)
                {
                    break;
                }

                // Task.Delay: poll interval while waiting for the registration outcome, bounded by the deadline.
                await Task.Delay(200);
            }

            return status;
        }
    }

    [Fact] void should_accept_both_registrations() => _context.AcceptFailure.ShouldBeNull();
    [Fact] void should_record_the_failed_registration_as_failed() => _context.Failing.State.ShouldEqual(RegistrationState.Failed);
    [Fact] void should_say_why_it_failed() => string.IsNullOrEmpty(_context.Failing.Failure).ShouldBeFalse();
    [Fact] void should_apply_the_other_registration() => _context.Healthy.State.ShouldEqual(RegistrationState.Applied);
}
