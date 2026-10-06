// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_querying;

namespace Cratis.Chronicle.Storage.InMemory.Alerts.for_AlertIncidentsStorage.when_querying;

public class and_count_buckets_differ_by_trailing_spaces : Contract.and_count_buckets_differ_by_trailing_spaces<InMemoryAlertIncidentsHarness>;
