// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreNamespaceStorage;

public class when_checking_after_an_observer_was_saved : given.a_namespace_storage
{
    bool _hasData;

    async Task Because()
    {
        await _storage.Observers.Save(ObserverState.Empty with { Identifier = new ObserverId("some-observer") });
        _hasData = await _storage.HasData();
    }

    [Fact] void should_report_having_data() => _hasData.ShouldBeTrue();
}
