// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverDefinitionsStorage.when_observing_all;

public class and_one_subscription_is_disposed : given.an_observed_storage
{
    int _disposedEmissions;

    async Task Because()
    {
        var subscription = _storage.ObserveAll().Subscribe(_ => _disposedEmissions++);
        subscription.Dispose();
        await _storage.Save(Definition());
    }

    [Fact] void should_stop_emitting_to_the_disposed_subscriber() => _disposedEmissions.ShouldEqual(1);
    [Fact] void should_keep_emitting_to_the_other_subscriber() => _emissions.Count.ShouldEqual(2);
    [Fact] void should_deliver_the_definition_to_the_other_subscriber() => _emissions[^1].Single().ShouldEqual(Definition());
}
