// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreNamespaceStorage;

public class when_an_auxiliary_named_sequence_has_events : given.a_namespace_database
{
    const string Sequence = "custom+closed_streams";
    bool _hasEvents;

    void Establish()
    {
        _names.Add(Sequence);
        _nonEmpty.Add(Sequence);
    }

    async Task Because() => _hasEvents = await _database.HasEvents();

    [Fact] void should_not_exclude_unknown_names_by_pattern() => _hasEvents.ShouldBeTrue();
}
