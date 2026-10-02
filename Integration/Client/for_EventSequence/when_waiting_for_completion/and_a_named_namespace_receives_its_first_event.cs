// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Registrations;

using context = Cratis.Chronicle.Integration.for_EventSequence.when_waiting_for_completion.and_a_named_namespace_receives_its_first_event.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_waiting_for_completion;

[Collection(ChronicleCollection.Name)]
public class and_a_named_namespace_receives_its_first_event(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        /// <summary>
        /// The fixture's PostgreSQL database prefix already contains a GUID; leave room for
        /// the store and namespace suffixes within PostgreSQL's 63-byte identifier limit.
        /// </summary>
        readonly string _storeName = $"c{Guid.NewGuid():N}"[..8];
        readonly EventSourceId _customerId = EventSourceId.New();
        IEventStore _defaultStore;
        IEventStore _tenantStore;

        public AppendResultWaitForCompletionResult DefaultCompletion { get; private set; }
        public AppendResultWaitForCompletionResult TenantCompletion { get; private set; }
        public CustomerSnapshot DefaultModel { get; private set; }
        public CustomerSnapshot TenantModel { get; private set; }
        public Contracts.Observation.ObserverInformation PatternObserver { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(CustomerNamed)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(CustomerSnapshot)];

        async Task Establish()
        {
            _defaultStore = await ChronicleClient.GetEventStore(_storeName);
            await _defaultStore.DiscoverAll();
            await _defaultStore.RegisterAll();
            (await _defaultStore.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();

            var append = await _defaultStore.EventLog.Append(_customerId, new CustomerNamed("Default customer"));
            append.IsSuccess.ShouldBeTrue();
            DefaultCompletion = await append.WaitForCompletion(TimeSpan.FromSeconds(30));
            DefaultModel = await _defaultStore.ReadModels.GetInstanceById<CustomerSnapshot>(_customerId);

            // These are the same artifacts already registered for the store: no event-type mutation
            // or restart should be needed to subscribe kernel observers in this new namespace.
            _tenantStore = await ChronicleClient.GetEventStore(_storeName, "acme");
            await _tenantStore.DiscoverAll();
            await _tenantStore.RegisterAll();
            (await _tenantStore.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
        }

        async Task Because()
        {
            var append = await _tenantStore.EventLog.Append(_customerId, new CustomerNamed("Tenant customer"));
            append.IsSuccess.ShouldBeTrue();
            TenantCompletion = await append.WaitForCompletion(TimeSpan.FromSeconds(30));
            TenantModel = await _tenantStore.ReadModels.GetInstanceById<CustomerSnapshot>(_customerId);
            var services = ((IChronicleServicesAccessor)_tenantStore.Connection).Services;
            PatternObserver = await services.Observers.GetObserverInformation(new()
            {
                EventStore = _storeName,
                Namespace = _tenantStore.Namespace,
                EventSequenceId = EventSequences.EventSequenceId.Log,
                ObserverId = PatternCapture.ObserverIdentifier
            });
        }
    }

    [Fact] void should_complete_in_the_default_namespace() => Context.DefaultCompletion.IsSuccess.ShouldBeTrue();
    [Fact] void should_complete_in_the_new_namespace() => Context.TenantCompletion.IsSuccess.ShouldBeTrue();
    [Fact] void should_materialize_the_default_model() => Context.DefaultModel.CustomerName.ShouldEqual("Default customer");
    [Fact] void should_materialize_the_tenant_model() => Context.TenantModel.CustomerName.ShouldEqual("Tenant customer");
    [Fact] void should_subscribe_pattern_capture_in_the_new_namespace() => Context.PatternObserver.IsSubscribed.ShouldBeTrue();
    [Fact] void should_capture_the_first_event() => Context.PatternObserver.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.First.Value);
}
