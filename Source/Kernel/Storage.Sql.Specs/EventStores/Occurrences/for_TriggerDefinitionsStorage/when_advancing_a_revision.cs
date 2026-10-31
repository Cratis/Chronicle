// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.for_TriggerDefinitionsStorage;

public class when_advancing_a_revision : given.trigger_definitions
{
    bool _saved;
    TriggerDefinition? _stored;

    async Task Establish() => await _storage.TrySave(_definition, null);
    async Task Because()
    {
        _saved = await _storage.TrySave(_definition with { Revision = 2, Schema = "false" }, 1);
        _stored = await _storage.Get(_definition.Id);
    }

    [Fact] void should_save_the_next_revision() => _saved.ShouldBeTrue();
    [Fact] void should_store_the_new_contract() => _stored!.Schema.ShouldEqual("false");
    [Fact] void should_store_the_new_revision() => _stored!.Revision.ShouldEqual(2L);
}
