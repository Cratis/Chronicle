// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_getting_latest_for_all_event_types_twice_without_a_change : given.an_event_types_storage
{
    EventTypeSchema[] _first;
    EventTypeSchema[] _second;

    async Task Because()
    {
        _first = [.. await _storage.GetLatestForAllEventTypes()];
        _second = [.. await _storage.GetLatestForAllEventTypes()];
    }

    [Fact] void should_return_the_same_schema_instance() => ReferenceEquals(_first[0], _second[0]).ShouldBeTrue();
    [Fact] void should_return_the_same_json_schema_instance() => ReferenceEquals(_first[0].Schema, _second[0].Schema).ShouldBeTrue();
}
