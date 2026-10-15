// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.for_TriggerDefinitionsStorage;

public class when_racing_to_register_a_trigger : given.trigger_definitions
{
    bool[] _saved;
    TriggerDefinition? _stored;

    async Task Because()
    {
        var other = Recreate();
        _saved = await StorageRace.Run(() => _storage.TrySave(_definition, null), () => other.TrySave(_definition with { Name = "Other" }, null));
        _stored = await _storage.Get(_definition.Id);
    }

    [Fact] void should_allow_exactly_one_writer() => _saved.Count(saved => saved).ShouldEqual(1);
    [Fact] void should_store_the_first_revision() => _stored!.Revision.ShouldEqual(1L);
    [Fact] void should_preserve_the_original_identity() => _stored!.Id.ShouldEqual(_definition.Id);
}
