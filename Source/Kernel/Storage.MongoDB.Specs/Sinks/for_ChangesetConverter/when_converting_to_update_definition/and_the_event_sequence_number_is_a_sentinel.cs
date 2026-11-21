// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_the_event_sequence_number_is_a_sentinel : given.a_converter_rendering_updates
{
    Task Because() => Convert(
        new Key("key", ArrayIndexers.NoIndexers),
        EventSequenceNumber.Unavailable,
        new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", "Before", "After")]));

    [Fact] void should_indicate_has_changes() => _result.hasChanges.ShouldBeTrue();
    [Fact] void should_set_the_property() => _update["$set"]["name"].ShouldEqual(new BsonString("After"));
    [Fact] void should_not_write_the_watermark() => _update.Contains("$max").ShouldBeFalse();
}
