// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_paging;

namespace Cratis.Chronicle.Storage.InMemory.Alerts.for_AlertIncidentsStorage.when_paging;

public class and_no_incidents_exist : Contract.and_no_incidents_exist<InMemoryAlertIncidentsHarness>;
