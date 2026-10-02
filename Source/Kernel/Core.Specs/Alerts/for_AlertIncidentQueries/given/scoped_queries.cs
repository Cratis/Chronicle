// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentQueries.given;

public class scoped_queries : Specification
{
    protected IStorage _storage;
    protected IAlertIncidentsStorage _incidents;
    protected IAlertIncidentsReadiness _readiness;
    protected IncidentId _id;

    void Establish()
    {
        _id = new(Guid.NewGuid());
        _storage = Substitute.For<IStorage>();
        _incidents = Substitute.For<IAlertIncidentsStorage>();
        _readiness = Substitute.For<IAlertIncidentsReadiness>();
        _readiness.Get().Returns(AlertIncidentsReadinessState.Ready);
        _storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).AlertIncidents.Returns(_incidents);
        _incidents.GetOpenPage(Arg.Any<AlertIncidentFilter>(), Arg.Any<AlertIncidentCursor?>(), Arg.Any<int>()).Returns(new AlertIncidentStoragePage([], null));
        _incidents.GetOpenCounts(Arg.Any<AlertIncidentScope>()).Returns([]);
    }
}
