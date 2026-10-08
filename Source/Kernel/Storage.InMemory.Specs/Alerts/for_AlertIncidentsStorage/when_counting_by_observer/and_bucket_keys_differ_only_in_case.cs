// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_counting_by_observer;

namespace Cratis.Chronicle.Storage.InMemory.Alerts.for_AlertIncidentsStorage.when_counting_by_observer;

public class and_bucket_keys_differ_only_in_case : Contract.and_bucket_keys_differ_only_in_case<InMemoryAlertIncidentsHarness>;
