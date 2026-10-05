// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;

using context = Cratis.Chronicle.Kernel.Integration.Alerts.for_AlertIncidents.when_processing_recorded_transitions.context;

namespace Cratis.Chronicle.Kernel.Integration.Alerts.for_AlertIncidents;

[Collection(ChronicleCollection.Name)]
public class when_processing_recorded_transitions(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.an_incidents_reactor(fixture)
    {
        public bool OpenCriticalIncident;
        public bool OriginalPartition;
        public bool ScopedLookup;

        async Task Because()
        {
            await Append(_raised);
            await Append(new AlertEscalated(_raised.IncidentId, AlertConditionKind.PartitionRetriesExhausted, AlertSeverity.Critical, _raised.Target with { Partition = "different" }, _raised.Evidence));
            await WaitForMaterialization();
            OpenCriticalIncident = _row is { IsOpen: true, Severity: AlertSeverity.Critical };
            OriginalPartition = _row?.Target.Partition == _raised.Target.Partition;
            ScopedLookup = await _incidents.GetOpen(new("other-store", null), _raised.IncidentId) is null;
        }
    }

    [Fact] void should_materialize_discovered_reactor_delivery() => Context.OpenCriticalIncident.ShouldBeTrue();
    [Fact] void should_preserve_the_original_target() => Context.OriginalPartition.ShouldBeTrue();
    [Fact] void should_scope_lookup_in_storage() => Context.ScopedLookup.ShouldBeTrue();
}
