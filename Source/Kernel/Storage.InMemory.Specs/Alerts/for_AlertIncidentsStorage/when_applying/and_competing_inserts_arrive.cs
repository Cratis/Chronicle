// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_applying;

namespace Cratis.Chronicle.Storage.InMemory.Alerts.for_AlertIncidentsStorage.when_applying;

public class and_competing_inserts_arrive : Contract.and_competing_inserts_arrive<InMemoryAlertIncidentsHarness>;
