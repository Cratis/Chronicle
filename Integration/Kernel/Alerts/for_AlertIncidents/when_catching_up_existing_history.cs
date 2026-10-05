// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reactors.Kernel;

using context = Cratis.Chronicle.Kernel.Integration.Alerts.for_AlertIncidents.when_catching_up_existing_history.context;

namespace Cratis.Chronicle.Kernel.Integration.Alerts.for_AlertIncidents;

[Collection(ChronicleCollection.Name)]
public class when_catching_up_existing_history(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.an_incidents_reactor(fixture)
    {
        public bool HistoryMaterialized;
        public bool WasNotReady;

        async Task Establish()
        {
            await _factory.GetGrain<IObserver>(AlertIncidentsReactor.ObserverKey).Unsubscribe();
            await Append(_raised);
            WasNotReady = await Services.GetRequiredService<IAlertIncidentsReadiness>().Get() == AlertIncidentsReadinessState.CatchingUp;
        }

        async Task Because()
        {
            await Services.GetRequiredService<IReactors>().DiscoverAndRegister(Concepts.EventStoreName.System, Concepts.EventStoreNamespaceName.Default);
            await WaitForMaterialization();
            HistoryMaterialized = _row?.Id == _raised.IncidentId;
        }
    }

    [Fact] void should_catch_up_recorded_history() => Context.HistoryMaterialized.ShouldBeTrue();
    [Fact] void should_not_report_ready_before_registration() => Context.WasNotReady.ShouldBeTrue();
}
