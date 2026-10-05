// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_paging;

namespace Cratis.Chronicle.Storage.MongoDB.Alerts.for_AlertIncidentsStorage.when_paging;

[Collection(MongoDBCollection.Name)]
public class and_limits_exceed_the_supported_range(MongoDBFixture fixture) : Contract.and_limits_exceed_the_supported_range<MongoAlertIncidentsHarness>
{
    protected override MongoAlertIncidentsHarness CreateHarness() => new() { Fixture = fixture };
}
