// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_there_are_no_changes : given.a_changeset_converter
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    Key _key;
    EventSequenceNumber _eventSequenceNumber;
    UpdateDefinitionAndArrayFilters _result;

    void Establish()
    {
        _key = new Key("some-key", ArrayIndexers.NoIndexers);
        _eventSequenceNumber = 42UL;

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([]);
    }

    async Task Because() => _result = await _converter.ToUpdateDefinition(_key, _changeset, _eventSequenceNumber);

    [Fact] void should_not_indicate_has_changes() => _result.hasChanges.ShouldBeFalse();
    [Fact] void should_not_convert_event_sequence_number_to_bson_value() => _mongoDBConverter.DidNotReceive().ToBsonValue(_eventSequenceNumber);
}
