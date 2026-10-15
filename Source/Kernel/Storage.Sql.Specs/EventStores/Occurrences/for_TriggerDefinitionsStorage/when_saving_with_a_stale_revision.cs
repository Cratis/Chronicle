// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.for_TriggerDefinitionsStorage;

public class when_saving_with_a_stale_revision : given.trigger_definitions
{
    bool _saved;
    TriggerDefinition? _stored;

    async Task Establish() => await _storage.TrySave(_definition, null);
    async Task Because()
    {
        _saved = await _storage.TrySave(_definition with { Revision = 3, Schema = "false" }, 2);
        _stored = await _storage.Get(_definition.Id);
    }

    [Fact] void should_refuse_the_stale_writer() => _saved.ShouldBeFalse();
    [Fact] void should_preserve_the_winner() => _stored.ShouldEqual(_definition);
}
