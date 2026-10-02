// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Registrations;

using context = Cratis.Chronicle.Integration.for_EventSequence.when_waiting_for_completion.and_the_same_event_types_are_used_in_a_second_namespace.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_waiting_for_completion;

[Collection(ChronicleCollection.Name)]
public class and_the_same_event_types_are_used_in_a_second_namespace(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        IEventStore _defaultStore;
        readonly Guid _customerId = Guid.NewGuid();

        public AppendResultWaitForCompletionResult DefaultCompletion;
        public AppendResultWaitForCompletionResult TenantCompletion;
        public CustomerSnapshot DefaultModel;
        public CustomerSnapshot TenantModel;
        public Contracts.Observation.ObserverInformation PatternCapture;

        public override IEnumerable<Type> EventTypes => [typeof(CustomerNamed)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(CustomerSnapshot)];

        async Task Establish()
        {
            // The fixture already isolates storage per run. Keep the store name short so the PostgreSQL
            // database prefix plus store and namespace stays within its 63-byte identifier limit.
            _defaultStore = await ChronicleClient.GetEventStore("wait4492");
            await _defaultStore.DiscoverAll();
            await _defaultStore.RegisterAll();
            (await _defaultStore.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();

            var first = await _defaultStore.EventLog.Append(_customerId, new CustomerNamed("Default customer"));
            first.IsSuccess.ShouldBeTrue();
            DefaultCompletion = await first.WaitForCompletion(TimeSpan.FromSeconds(30));
            DefaultModel = await _defaultStore.ReadModels.GetInstanceById<CustomerSnapshot>(_customerId);
        }

        async Task Because()
        {
            var tenantStore = await ChronicleClient.GetEventStore(_defaultStore.Name, "acme");
            await tenantStore.DiscoverAll();
            await tenantStore.RegisterAll();
            (await tenantStore.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();

            var second = await tenantStore.EventLog.Append(_customerId, new CustomerNamed("Tenant customer"));
            second.IsSuccess.ShouldBeTrue();
            TenantCompletion = await second.WaitForCompletion(TimeSpan.FromSeconds(30));
            TenantModel = await tenantStore.ReadModels.GetInstanceById<CustomerSnapshot>(_customerId);
            PatternCapture = await ((IChronicleServicesAccessor)tenantStore.Connection).Services.Observers.GetObserverInformation(new()
            {
                EventStore = tenantStore.Name,
                Namespace = tenantStore.Namespace,
                EventSequenceId = EventSequences.EventSequenceId.Log,
                ObserverId = Patterns.PatternCapture.ObserverIdentifier
            });
        }
    }

    [Fact] void should_complete_in_the_default_namespace() => Context.DefaultCompletion.IsSuccess.ShouldBeTrue();
    [Fact] void should_complete_in_the_second_namespace() => Context.TenantCompletion.IsSuccess.ShouldBeTrue();
    [Fact] void should_materialize_the_default_read_model() => Context.DefaultModel.CustomerName.ShouldEqual("Default customer");
    [Fact] void should_materialize_the_tenant_read_model() => Context.TenantModel.CustomerName.ShouldEqual("Tenant customer");
    [Fact] void should_subscribe_pattern_capture_in_the_second_namespace() => Context.PatternCapture.IsSubscribed.ShouldBeTrue();
    [Fact] void should_capture_the_first_tenant_event() => Context.PatternCapture.LastHandledEventSequenceNumber.ShouldEqual(0UL);
}
