// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsMetricsOwnerKeeper.given;

public class a_keeper : Specification
{
    protected IGrainFactory _grainFactory;
    protected IAlertIncidentsMetricsOwner _owner;
    protected AlertIncidentsMetricsOwnerKeeper _keeper;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _owner = Substitute.For<IAlertIncidentsMetricsOwner>();
        _grainFactory.GetGrain<IAlertIncidentsMetricsOwner>(AlertIncidentsMetricsOwner.Key, null).Returns(_owner);
        _keeper = new(_grainFactory, Substitute.For<ILogger<AlertIncidentsMetricsOwnerKeeper>>());
    }
}
