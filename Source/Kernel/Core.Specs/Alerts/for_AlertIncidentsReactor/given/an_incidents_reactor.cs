// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsReactor.given;

public class an_incidents_reactor : Specification
{
    protected IStorage _storage;
    protected IAlertIncidentsStorage _incidents;
    protected AlertIncidentsReactor _reactor;
    protected AlertRaised _raised;
    protected EventContext _context;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _incidents = Substitute.For<IAlertIncidentsStorage>();
        _storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).AlertIncidents.Returns(_incidents);
        _reactor = new(_storage, Microsoft.Extensions.Logging.Abstractions.NullLogger<AlertIncidentsReactor>.Instance);
        var occurred = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _raised = new(new IncidentId(Guid.NewGuid()), AlertConditionKind.PartitionFailing, AlertSeverity.Warning, new("affected", "tenant", "observer", EventSequenceId.Log, "[none]"), new(3, occurred, occurred, FailureKind.Handling, "recorded"));
        _context = EventContext.Empty with { Occurred = occurred, SequenceNumber = 10UL };
    }
}
