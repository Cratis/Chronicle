// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_querying;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts.for_AlertIncidentsStorage.when_querying;

public class and_all_optional_filters_apply : Contract.and_all_optional_filters_apply<SqlAlertIncidentsHarness>;
