// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class with_a_seeded_instance_under_a_noncanonical_guid_key : given.a_collection
{
    readonly Guid _id = Guid.Parse("628c141c-f94d-4d64-854a-98006525b353");

    async Task Establish()
    {
        var key = new EventSourceId(_id.ToString("N"));
        ((ReadModelsForTesting)_store.ReadModels).RegisterInstance(key, new ObservedModule(_id, "Seeded"));
        await _store.EventLog.Append(key, new ModuleCreated("Folded"));
    }

    async Task Because()
    {
        _observation = _store.ReadModels.Observe<ObservedModule>();
        Subscribe();
        await WaitForEmission();
    }

    [Fact] void should_emit_one_instance_per_normalized_key() => _emissions.Single().Count().ShouldEqual(2);
    [Fact] void should_replace_the_seeded_instance() => _emissions.Single().Where(row => row.Id == _id).ShouldContainOnly(new ObservedModule(_id, "Folded"));
}
