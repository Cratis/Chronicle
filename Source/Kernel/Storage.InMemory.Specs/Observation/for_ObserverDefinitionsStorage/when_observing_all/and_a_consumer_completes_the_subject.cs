// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverDefinitionsStorage.when_observing_all;

public class and_a_consumer_completes_the_subject : given.an_observed_storage
{
    async Task Because()
    {
        _storage.ObserveAll().OnCompleted();
        await _storage.Save(Definition());
    }

    [Fact] void should_keep_the_shared_producer_alive() => _emissions.Count.ShouldEqual(2);
    [Fact] void should_deliver_the_saved_definition() => _emissions[^1].Single().ShouldEqual(Definition());
}
