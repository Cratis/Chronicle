// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_counting_by_observer;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts.for_AlertIncidentsStorage.when_counting_by_observer;

public class and_partitions_share_an_observer : Contract.and_partitions_share_an_observer<SqlAlertIncidentsHarness>;
