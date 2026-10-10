// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Registrations;

using context = Cratis.Chronicle.Integration.for_ReadModels.when_waiting_for_registration.and_the_event_store_is_fresh.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_waiting_for_registration;

[Collection(ChronicleCollection.Name)]
public class and_the_event_store_is_fresh(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        readonly EventSourceId _eventSourceId = Guid.NewGuid();
        IEventStore _freshStore;

        public RegistrationOutcome Registration;
        public Contracts.Observation.ObserverInformation ObserverAfterRegistration;
        public AppendResultWaitForCompletionResult Completion;
        public IEnumerable<SomeReadModel> Instances;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent), typeof(AnotherEvent)];
        public override IEnumerable<Type> Projections => [typeof(SomeProjection)];

        async Task Establish()
        {
            // Keep the unique store name short enough for PostgreSQL's database identifier limit.
            _freshStore = await ChronicleClient.GetEventStore($"reg{Guid.NewGuid():N}"[..12]);
            await _freshStore.DiscoverAll();
            await _freshStore.RegisterAll();
        }

        async Task Because()
        {
            Registration = await _freshStore.WaitForRegistration(TimeSpan.FromSeconds(30));
            ObserverAfterRegistration = await ((IChronicleServicesAccessor)_freshStore.Connection).Services.Observers.GetObserverInformation(new()
            {
                EventStore = _freshStore.Name,
                Namespace = _freshStore.Namespace,
                ObserverId = _freshStore.Projections.GetProjectionIdFor<SomeProjection>(),
                EventSequenceId = EventSequences.EventSequenceId.Log
            });

            var appendResult = await _freshStore.EventLog.Append(_eventSourceId, new SomeEvent(42));
            appendResult.IsSuccess.ShouldBeTrue();
            Completion = await appendResult.WaitForCompletion(TimeSpan.FromSeconds(30));
            Instances = await _freshStore.ReadModels.GetInstances<SomeReadModel>();
        }
    }

    [Fact] void should_register_the_projection() => Context.Registration.IsSuccess.ShouldBeTrue();
    [Fact] void should_already_have_subscribed_the_observer() => Context.ObserverAfterRegistration.IsSubscribed.ShouldBeTrue();
    [Fact] void should_complete_observation_of_the_first_append() => Context.Completion.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_the_materialized_instance_after_completion() => Context.Instances.Single().Number.ShouldEqual(42);
}
