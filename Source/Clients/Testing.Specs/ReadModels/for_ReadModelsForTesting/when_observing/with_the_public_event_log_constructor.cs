// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class with_the_public_event_log_constructor : given.a_collection
{
    async Task Because()
    {
        var readModels = new ReadModelsForTesting(_store.ReadModels, _store.EventLog);
        _observation = readModels.Observe<ObservedModule>();
        Subscribe();
        await WaitForEmission();
    }

    [Fact] void should_rebuild_from_the_provided_log() => _emissions.Single().Select(row => row.Name).ShouldContainOnly("First");
}
