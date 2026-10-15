// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Alerts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsMetricsOwner.given;

public class an_owner : Specification
{
    protected IStorage _storage;
    protected IAlertIncidentsStorage _incidents;
    protected IAlertIncidentsReadiness _readiness;
    protected IAlertIncidentsGauge _gauge;
    protected AlertIncidentsMetricsOwner _owner;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _incidents = _storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).AlertIncidents;
        _incidents.GetOpenCountsByObserver(Arg.Any<CancellationToken>()).Returns(
            [new AlertIncidentObserverCount("store", "Default", "observer", "event-log", AlertConditionKind.PartitionFailing, AlertSeverity.Warning, 2)]);
        _readiness = Substitute.For<IAlertIncidentsReadiness>();
        _readiness.Get().Returns(AlertIncidentsReadinessState.Ready);
        _gauge = Substitute.For<IAlertIncidentsGauge>();
        _owner = CreateOwner();
    }

    protected AlertIncidentsMetricsOwner CreateOwner() => new(_storage, _readiness, _gauge, Substitute.For<ILogger<AlertIncidentsMetricsOwner>>());
}
