// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_a_primitive_child_is_added : given.a_converter_rendering_updates
{
    Task Because() => Convert(new Key("key", ArrayIndexers.NoIndexers), 42UL, new ChildAdded("tag-1", new PropertyPath("Tags"), new PropertyPath("Id"), "tag-1", ArrayIndexers.NoIndexers));

    [Fact] void should_push_the_value() => _update["$push"]["Tags"].ShouldEqual(new BsonString("tag-1"));
    [Fact] void should_not_convert_the_state_as_a_document() => _expandoObjectConverter.DidNotReceive().ToBsonDocument(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>());
}
