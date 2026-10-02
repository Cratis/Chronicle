// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentQueries;

public class when_binding_an_omitted_page_limit : given.scoped_queries
{
    Contracts.Queries.QueryResult<Contracts.Alerts.AlertIncidentPageResponse> _result;

    async Task Because()
    {
        var service = new Services.Alerts.Alerts(_storage, _readiness, NullLogger<Services.Alerts.Alerts>.Instance);
        var request = Serializer.DeepClone(new Contracts.Alerts.GetOpenIncidentsRequest { EventStore = "affected" });
        _result = await service.GetOpenIncidents(request);
    }

    [Fact] void should_bind_the_query_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_use_the_default_page_size() => await _incidents.Received(1).GetOpenPage(new(new("affected", null), null, null, null), null, 100);
}
