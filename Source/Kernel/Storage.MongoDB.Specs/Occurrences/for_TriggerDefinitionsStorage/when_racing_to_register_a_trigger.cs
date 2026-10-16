// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences.for_TriggerDefinitionsStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_racing_to_register_a_trigger(ReplicaSetMongoDBFixture fixture) : given.trigger_definitions(fixture)
{
    bool[] _saved;
    TriggerDefinition[] _stored;

    async Task Because()
    {
        var other = new TriggerDefinitionsStorage(_database);
        _saved = await StorageRace.Run(() => _storage.TrySave(_definition, null), () => other.TrySave(_definition with { Name = "Other" }, null));
        _stored = (await _storage.GetAll()).ToArray();
    }

    [Fact] void should_allow_exactly_one_writer() => _saved.Count(saved => saved).ShouldEqual(1);
    [Fact] void should_store_exactly_one_definition() => _stored.Length.ShouldEqual(1);
    [Fact] void should_preserve_the_original_identity() => _stored[0].Id.ShouldEqual(_definition.Id);
}
